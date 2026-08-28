using CareerOS.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Data;

public class CareerDbContext(DbContextOptions<CareerDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> EducationHistory => Set<Education>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Resume> Resumes => Set<Resume>();

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

        modelBuilder.Entity<WorkExperience>().ToTable("work_experiences").Property(x => x.Company).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Education>().ToTable("education_history").Property(x => x.Institution).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Certification>().ToTable("certifications").Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Resume>().ToTable("resumes").Property(x => x.Language).HasMaxLength(10).IsRequired();
        modelBuilder.Entity<CandidateProfile>().HasMany(x => x.WorkExperiences).WithOne().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CandidateProfile>().HasMany(x => x.EducationHistory).WithOne().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CandidateProfile>().HasMany(x => x.Certifications).WithOne().HasForeignKey(x => x.CandidateProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
