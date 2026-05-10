using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class MonthlyExpenseConfiguration : IEntityTypeConfiguration<MonthlyExpense>
    {
        public void Configure(EntityTypeBuilder<MonthlyExpense> builder)
        {
            builder.HasKey(e => e.Id);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Wallet)
                .WithMany()
                .HasForeignKey(e => e.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Transaction)
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(e => e.Category)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Amount)
                .HasColumnType("numeric(18,2)");

            builder.HasIndex(e => new { e.UserId, e.Category, e.Year, e.Month });
            builder.HasIndex(e => e.TransactionId);
        }
    }
}
