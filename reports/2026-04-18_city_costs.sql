SELECT
    city.name,
    COUNT(*) AS total_calls,
    SUM(c.duration_minutes) AS total_minutes,
    ROUND(SUM(
        c.duration_minutes *
        CASE
            WHEN c.time_of_day = 'Day' THEN city.day_tariff
            ELSE city.night_tariff
        END *
        (1 - COALESCE(d.discount_percent, 0) / 100.0)
    ), 2) AS total_cost
FROM calls c
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
GROUP BY city.name
ORDER BY total_cost DESC, city.name;
