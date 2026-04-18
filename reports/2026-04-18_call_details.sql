SELECT
    c.call_id,
    s.phone_number AS source_phone,
    c.dest_phone,
    city.name AS destination_city,
    to_timestamp(c.start_unix_time) AT TIME ZONE 'UTC' AS started_at_utc,
    c.duration_minutes,
    c.time_of_day,
    CASE
        WHEN c.time_of_day = 'Day' THEN city.day_tariff
        ELSE city.night_tariff
    END AS applied_tariff,
    COALESCE(d.discount_percent, 0) AS discount_percent,
    ROUND(
        c.duration_minutes *
        CASE
            WHEN c.time_of_day = 'Day' THEN city.day_tariff
            ELSE city.night_tariff
        END,
        2
    ) AS base_cost,
    ROUND(
        c.duration_minutes *
        CASE
            WHEN c.time_of_day = 'Day' THEN city.day_tariff
            ELSE city.night_tariff
        END *
        (1 - COALESCE(d.discount_percent, 0) / 100.0),
        2
    ) AS final_cost
FROM calls c
JOIN subscribers s ON s.subscriber_id = c.subscriber_id
JOIN cities city ON city.city_id = c.city_id
LEFT JOIN LATERAL (
    SELECT discount_percent
    FROM city_discounts
    WHERE city_id = c.city_id
      AND c.duration_minutes >= min_minutes
      AND (max_minutes IS NULL OR c.duration_minutes < max_minutes)
    ORDER BY min_minutes DESC
    LIMIT 1
) d ON true
WHERE c.duration_minutes IS NOT NULL
  AND to_timestamp(c.start_unix_time) >= @date_from
  AND to_timestamp(c.start_unix_time) < @date_to
ORDER BY c.start_unix_time DESC;
