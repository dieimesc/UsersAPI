using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.Domain.Models;

namespace UsersAPI.Infra.Data.Mappings
{
    public class UserMapping : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.HasKey(c => c.Id).HasName("Id");

            builder.Property(c => c.Id).IsRequired();
            builder.Property(c => c.Login).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Password).IsRequired().HasMaxLength(30);
          
        }
    }
}
