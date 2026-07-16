using CareerOS.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Data;

public class CareerDbContext(DbContextOptions<CareerDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<CandidateProfile>();
        profile.ToTable("candidate_profiles");
        profile.HasKey(x => x.Id);
        profile.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        profile.Property(x => x.ProfessionalTitle).HasMaxLength(160).IsRequired();
        profile.Property(x => x.Email).HasMaxLength(320).IsRequired();
        profile.Property(x => x.ProfessionalSummary).HasMaxLength(4000);
        profile.HasIndex(x => x.Email);
    }
}
