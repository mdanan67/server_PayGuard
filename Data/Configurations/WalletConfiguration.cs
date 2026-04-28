using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.HasKey(f => f.Id);
            builder.HasOne(w => w.User)
             .WithOne(u => u.Wallet)
             .HasForeignKey<Wallet>(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(w => w.UserId)
                .IsUnique();

            builder.Property(w => w.Balance)
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(0m);

            builder.Property(w => w.TotalSpend)
                .HasColumnType("numeric(18,2)")
                .HasDefaultValue(0m);

        }
    }
}