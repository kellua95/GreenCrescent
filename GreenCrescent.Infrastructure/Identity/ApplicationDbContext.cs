using GreenCrescent.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Identity
{
    public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options),
      IDataProtectionKeyContext
    {
        public DbSet<DataProtectionKey> DataProtectionKeys =>
            Set<DataProtectionKey>();
        public DbSet<Sponsor> Sponsors => Set<Sponsor>();

        public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();

        public DbSet<Sponsorship> Sponsorships => Set<Sponsorship>();

        public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();

        public DbSet<PaymentAllocation> PaymentAllocations =>
            Set<PaymentAllocation>();

        public DbSet<SponsorshipChange> SponsorshipChanges =>
            Set<SponsorshipChange>();

        public DbSet<ResponsibleSheikh> ResponsibleSheikhs => Set<ResponsibleSheikh>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);

            builder.Entity<ResponsibleSheikh>(entity =>
            {
                entity.ToTable("ResponsibleSheikhs");

                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            builder.Entity<Sponsorship>()
                .HasOne(x => x.ResponsibleSheikh)
                .WithMany(x => x.Sponsorships)
                .HasForeignKey(x => x.ResponsibleSheikhId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
