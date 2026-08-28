using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candidate_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreferredName = table.Column<string>(type: "text", nullable: true),
                    ProfessionalTitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProfessionalSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    OpenToRemoteWork = table.Column<bool>(type: "boolean", nullable: false),
                    OpenToRelocation = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TargetCountry = table.Column<string>(type: "text", nullable: false),
                    ShowPhone = table.Column<bool>(type: "boolean", nullable: false),
                    ShowEmail = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLocation = table.Column<bool>(type: "boolean", nullable: false),
                    CustomizedTitle = table.Column<string>(type: "text", nullable: false),
                    CustomizedSummary = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    CustomizedExperiencesJson = table.Column<string>(type: "text", nullable: true),
                    CustomizedEducationsJson = table.Column<string>(type: "text", nullable: true),
                    CustomizedCertificationsJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: true),
                    IssuedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CredentialUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certifications_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "education_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Course = table.Column<string>(type: "text", nullable: false),
                    Degree = table.Column<string>(type: "text", nullable: true),
                    CompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_education_history_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_experiences_candidate_profiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "candidate_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_Email",
                table: "candidate_profiles",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_certifications_CandidateProfileId",
                table: "certifications",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_education_history_CandidateProfileId",
                table: "education_history",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_work_experiences_CandidateProfileId",
                table: "work_experiences",
                column: "CandidateProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certifications");

            migrationBuilder.DropTable(
                name: "education_history");

            migrationBuilder.DropTable(
                name: "resumes");

            migrationBuilder.DropTable(
                name: "work_experiences");

            migrationBuilder.DropTable(
                name: "candidate_profiles");
        }
    }
}
