using HireFlow.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireFlow.Identity.Infrastructure.Persistence.Configurations;

public class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("otp_challenges");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(o => o.Purpose)
            .HasColumnName("purpose")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.CodeHash)
            .HasColumnName("code_hash")
            .IsRequired();

        builder.Property(o => o.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(o => o.VerifiedAtUtc)
            .HasColumnName("verified_at_utc");

        builder.Property(o => o.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0);

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(o => new { o.UserId, o.Purpose });

        builder.HasOne(o => o.User)
            .WithMany(u => u.OtpChallenges)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
