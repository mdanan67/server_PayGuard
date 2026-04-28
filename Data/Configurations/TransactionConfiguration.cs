using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.Id);


            builder.HasOne(t => t.SenderWallet)
                .WithMany(w => w.SentTransactions)
                .HasForeignKey(t => t.SenderWalletId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasOne(t => t.ReceiverWallet)
                .WithMany(w => w.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverWalletId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Property(t => t.Amount)
                .HasColumnType("numeric(18,2)");


            builder.Property(t => t.StripePaymentIntentId)
                .HasMaxLength(255);

            builder.Property(t => t.StripeChargeId)
                .HasMaxLength(255);

            builder.Property(t => t.FailureReason)
                .HasMaxLength(500);


            builder.HasIndex(t => t.SenderWalletId);
            builder.HasIndex(t => t.ReceiverWalletId);
            builder.HasIndex(t => t.CreatedAt);
        }
    }
}