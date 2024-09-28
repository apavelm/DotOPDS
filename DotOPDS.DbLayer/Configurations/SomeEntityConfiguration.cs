using DotOPDS.DbLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotOPDS.DbLayer.Configurations;

public class SomeEntityConfiguration : IEntityTypeConfiguration<SomeEntity>

{
    public void Configure(EntityTypeBuilder<SomeEntity> builder)
    {
        //builder.ToTable("SomeEntity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SomeField).IsRequired().IsUnicode().HasMaxLength(256);

    }
}
