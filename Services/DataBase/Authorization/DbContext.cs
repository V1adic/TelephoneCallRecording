using Microsoft.EntityFrameworkCore;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Models.Calls;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<Subscriber> Subscribers => Set<Subscriber>();
        public DbSet<CityDiscount> CityDiscounts => Set<CityDiscount>();
        public DbSet<CallRecord> Calls => Set<CallRecord>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("id");

                entity.Property(x => x.Email)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasColumnType("citext");

                entity.HasIndex(x => x.Email)
                    .IsUnique();

                entity.Property(x => x.Username)
                    .HasColumnName("username")
                    .IsRequired()
                    .HasMaxLength(15)
                    .HasColumnType("citext");

                entity.HasIndex(x => x.Username)
                    .IsUnique();

                entity.Property(x => x.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired()
                    .HasMaxLength(44);

                entity.Property(x => x.PasswordSalt)
                    .HasColumnName("password_salt")
                    .IsRequired()
                    .HasMaxLength(24);

                entity.Property(x => x.IsEmailConfirmed)
                    .HasColumnName("is_email_confirmed")
                    .HasDefaultValue(false);

                entity.Property(x => x.EmailConfirmationCodeHash)
                    .HasColumnName("email_confirmation_code_hash")
                    .HasMaxLength(44);

                entity.Property(x => x.EmailConfirmationExpires)
                    .HasColumnName("email_confirmation_expires");

                entity.Property(x => x.FailedLoginAttempts)
                    .HasColumnName("failed_login_attempts")
                    .HasDefaultValue(0);

                entity.Property(x => x.FailedEmailConfirmAttempts)
                    .HasColumnName("failed_email_confirm_attempts")
                    .HasDefaultValue(0);

                entity.Property(x => x.LockoutEnd)
                    .HasColumnName("lockout_end");

                entity.Property(x => x.SubscriberId)
                    .HasColumnName("subscriber_id");

                entity.HasIndex(x => x.SubscriberId)
                    .IsUnique()
                    .HasFilter("\"subscriber_id\" IS NOT NULL");

                entity.HasOne(x => x.Subscriber)
                    .WithOne(x => x.User)
                    .HasForeignKey<User>(x => x.SubscriberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Role)
                    .HasColumnName("role")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Client");
            });

            builder.Entity<City>(entity =>
            {
                entity.ToTable("cities");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("city_id");

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.Name)
                    .IsUnique();

                entity.Property(x => x.DayTariff)
                    .HasColumnName("day_tariff")
                    .HasPrecision(10, 2);

                entity.Property(x => x.NightTariff)
                    .HasColumnName("night_tariff")
                    .HasPrecision(10, 2);
            });

            builder.Entity<Subscriber>(entity =>
            {
                entity.ToTable("subscribers");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("subscriber_id");

                entity.Property(x => x.PhoneNumber)
                    .HasColumnName("phone_number")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(x => x.PhoneNumber)
                    .IsUnique();

                entity.Property(x => x.Inn)
                    .HasColumnName("inn")
                    .HasMaxLength(12)
                    .IsRequired();

                entity.Property(x => x.Address)
                    .HasColumnName("address")
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(x => x.CityId)
                    .HasColumnName("city_id");

                entity.HasOne(x => x.City)
                    .WithMany(x => x.Subscribers)
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CityDiscount>(entity =>
            {
                entity.ToTable("city_discounts");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("city_discount_id");

                entity.Property(x => x.CityId)
                    .HasColumnName("city_id");

                entity.Property(x => x.MinMinutes)
                    .HasColumnName("min_minutes");

                entity.Property(x => x.MaxMinutes)
                    .HasColumnName("max_minutes");

                entity.Property(x => x.DiscountPercent)
                    .HasColumnName("discount_percent")
                    .HasPrecision(5, 2);

                entity.HasIndex(x => new { x.CityId, x.MinMinutes })
                    .IsUnique();

                entity.HasOne(x => x.City)
                    .WithMany(x => x.Discounts)
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CallRecord>(entity =>
            {
                entity.ToTable("calls");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasColumnName("call_id");

                entity.Property(x => x.SubscriberId)
                    .HasColumnName("subscriber_id");

                entity.Property(x => x.CityId)
                    .HasColumnName("city_id");

                entity.Property(x => x.DestPhone)
                    .HasColumnName("dest_phone")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.StartUnixTime)
                    .HasColumnName("start_unix_time");

                entity.Property(x => x.DurationMinutes)
                    .HasColumnName("duration_minutes");

                entity.Property(x => x.TimeOfDay)
                    .HasColumnName("time_of_day")
                    .HasConversion<string>()
                    .HasMaxLength(10);

                entity.HasIndex(x => new { x.SubscriberId, x.DestPhone });

                entity.HasIndex(x => new { x.SubscriberId, x.DestPhone, x.DurationMinutes })
                    .IsUnique()
                    .HasFilter("\"duration_minutes\" IS NULL");

                entity.HasOne(x => x.Subscriber)
                    .WithMany(x => x.Calls)
                    .HasForeignKey(x => x.SubscriberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.City)
                    .WithMany(x => x.Calls)
                    .HasForeignKey(x => x.CityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
