using Microsoft.EntityFrameworkCore;
using TelephoneCallRecording.Models.Authorization;

namespace TelephoneCallRecording.Services.DataBase.Authorization
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            /*
             Table: Users
                id SERIAL PRIMARY KEY,
                email CITEXT NOT NULL UNIQUE,
                username CITEXT NOT NULL UNIQUE,
                password_hash VARCHAR(44) NOT NULL,
                password_salt VARCHAR(24) NOT NULL,
                is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
                email_confirmation_code_hash VARCHAR(44),
                email_confirmation_expires TIMESTAMP,
                failed_login_attempts INT NOT NULL DEFAULT 0,
                failed_email_confirm_attempts INT NOT NULL DEFAULT 0,
                lockout_end TIMESTAMP
             */

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
            });
        }
    }
}
