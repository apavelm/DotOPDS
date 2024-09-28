using DotOPDS.DbLayer.Configurations;
using DotOPDS.DbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotOPDS.DbLayer
{
    public class DotOPDSDbContext : DbContext
    {
        public DbSet<SomeEntity> SomeEntities { get; set; }

        public DotOPDSDbContext(DbContextOptions<DotOPDSDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.ApplyConfiguration<SomeEntity>(new SomeEntityConfiguration());
        }
    }
}
