using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCandidateProfile : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_Email",
                table: "candidate_profiles",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_profiles");
        }
    }
}
