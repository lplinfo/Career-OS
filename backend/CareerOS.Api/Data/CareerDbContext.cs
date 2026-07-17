using CareerOS.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Data;

public class CareerDbContext(DbContextOptions<CareerDbContext> options) : DbContext(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> Educations => Set<Education>();
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

        // One-to-many relationships
        profile.HasMany(x => x.Experiences)
               .WithOne()
               .HasForeignKey(x => x.CandidateProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        profile.HasMany(x => x.Educations)
               .WithOne()
               .HasForeignKey(x => x.CandidateProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        profile.HasMany(x => x.Certifications)
               .WithOne()
               .HasForeignKey(x => x.CandidateProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        profile.HasMany(x => x.Resumes)
               .WithOne()
               .HasForeignKey(x => x.CandidateProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        var experience = modelBuilder.Entity<WorkExperience>();
        experience.ToTable("work_experiences");
        experience.HasKey(x => x.Id);
        experience.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        experience.Property(x => x.JobTitle).HasMaxLength(160).IsRequired();
        experience.Property(x => x.Description).HasMaxLength(4000);

        var education = modelBuilder.Entity<Education>();
        education.ToTable("educations");
        education.HasKey(x => x.Id);
        education.Property(x => x.Institution).HasMaxLength(200).IsRequired();
        education.Property(x => x.Degree).HasMaxLength(100).IsRequired();
        education.Property(x => x.FieldOfStudy).HasMaxLength(100).IsRequired();

        var certification = modelBuilder.Entity<Certification>();
        certification.ToTable("certifications");
        certification.HasKey(x => x.Id);
        certification.Property(x => x.Name).HasMaxLength(200).IsRequired();
        certification.Property(x => x.IssuingOrganization).HasMaxLength(200).IsRequired();

        var resume = modelBuilder.Entity<Resume>();
        resume.ToTable("resumes");
        resume.HasKey(x => x.Id);
        resume.Property(x => x.Language).HasMaxLength(10).IsRequired();
        resume.Property(x => x.TargetCountry).HasMaxLength(10).IsRequired();
        resume.Property(x => x.CustomizedTitle).HasMaxLength(200);
        resume.Property(x => x.CustomizedSummary).HasMaxLength(4000);
    }
}
