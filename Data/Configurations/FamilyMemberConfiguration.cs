using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
    {
        public void Configure(EntityTypeBuilder<FamilyMember> builder)
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.LinkedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(f => f.Parent).WithMany(f => f.ParentLinks).HasForeignKey(f => f.ParentId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Child).WithMany(f => f.ChildLinks).HasForeignKey(f => f.ChildId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => f.ChildId).IsUnique();

            builder.HasIndex(f => new { f.ChildId, f.ParentId }).IsUnique();
        }
    }


}