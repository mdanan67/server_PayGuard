using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.model;

namespace server.Data.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Type)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Name)
                .HasMaxLength(100);

            builder.HasOne(c => c.CreatedByUser)
                .WithMany(u => u.CreatedConversations)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
