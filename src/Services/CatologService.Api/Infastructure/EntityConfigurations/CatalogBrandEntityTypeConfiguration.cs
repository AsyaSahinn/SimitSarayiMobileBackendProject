using CatologService.Api.Core.Domain;
using CatologService.Api.Infastructure.Context;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CatologService.Api.Infastructure.EntityConfigurations
{
    class CatalogBrandEntityTypeConfiguration : IEntityTypeConfiguration<CatalogBrand>
    {
        public void Configure(EntityTypeBuilder<CatalogBrand> builder)
        {
            builder.ToTable("CatalogBrand", CatalogContext.DEFAULT_SCHEMA);

            builder.HasKey(ci => ci.Id);

            builder.Property(ci => ci.Id)
               .UseHiLo("catalog_brand_hilo")
               .IsRequired();
            //High-Low algoritması, bir veritabanı alanını birincil anahtar olarak kullanırken otomatik artan bir değer oluşturmak için bir tekniktir.
            builder.Property(cb => cb.Brand)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
