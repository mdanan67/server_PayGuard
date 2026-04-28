using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()"); // ✅ fixed

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.FirstName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasMaxLength(100);

            builder.Property(u => u.Gender)
                .HasMaxLength(20);

            builder.Property(u => u.Phone)
                .HasMaxLength(20);

            builder.Property(u => u.Profile_image)
                .HasMaxLength(500);

            builder.Property(u => u.Role)
                .HasMaxLength(50)
                .HasDefaultValue("parent");

            builder.Property(u => u.RefreshToken)
                .HasMaxLength(500);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("NOW()"); // ✅ fixed

            builder.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("NOW()"); // ✅ already correct

            builder.HasOne(u => u.ParentBalance)
                .WithOne(pb => pb.User)
                .HasForeignKey<ParentBalance>(pb => pb.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}