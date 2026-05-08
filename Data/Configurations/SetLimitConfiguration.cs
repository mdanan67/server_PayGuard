using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class SetLimitConfiguration : IEntityTypeConfiguration<SpendingLimit>
    {
        public void Configure(EntityTypeBuilder<SpendingLimit> builder)
        {
            builder.HasKey(fk => fk.Id);
            builder.HasOne(id => id.User)
            .WithOne(id => id.SpendingLimit)
            .HasForeignKey<SpendingLimit>(id => id.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(id => id.UserId).IsUnique();

        }
    }
}