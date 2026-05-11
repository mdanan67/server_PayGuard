using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class FundRequestConfiguration : IEntityTypeConfiguration<FundRequest>
    {
        public void Configure(EntityTypeBuilder<FundRequest> builder)
        {
            builder.HasKey(request => request.Id);

            builder.HasOne(request => request.Child)
                .WithMany(user => user.ChildFundRequests)
                .HasForeignKey(request => request.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(request => request.Parent)
                .WithMany(user => user.ParentFundRequests)
                .HasForeignKey(request => request.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(request => request.Amount)
                .HasColumnType("numeric(18,2)");

            builder.Property(request => request.Reason)
                .HasMaxLength(500);

            builder.HasIndex(request => request.ChildId);
            builder.HasIndex(request => request.ParentId);
            builder.HasIndex(request => request.Status);
            builder.HasIndex(request => request.CreatedAt);
        }
    }
}
