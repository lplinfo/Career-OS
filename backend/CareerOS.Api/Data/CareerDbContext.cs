using CareerOS.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Data;

public class CareerDbContext(DbContextOptions<CareerDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<Education> EducationHistory => Set<Education>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Resume> Resumes => Set<Resume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.Email).HasMaxLength(320);
            b.Property(u => u.UserName).HasMaxLength(320);
            b.Property(u => u.NormalizedEmail).HasMaxLength(320);
            b.Property(u => u.NormalizedUserName).HasMaxLength(320);
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
            b.HasOne<CandidateProfile>()
             .WithMany()
             .HasForeignKey(u => u.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("user_claims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("user_logins");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("user_tokens");
        });

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
