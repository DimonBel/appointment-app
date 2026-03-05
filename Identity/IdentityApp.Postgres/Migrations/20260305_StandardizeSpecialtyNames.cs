using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityApp.Postgres.Migrations
{
    /// <summary>
    /// Migration to standardize specialty names across the system
    /// Ensures consistent naming convention using -ologist/-ician suffixes
    /// </summary>
    public partial class StandardizeSpecialtyNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Cardiology -> Cardiologist
            migrationBuilder.Sql(
                @"UPDATE ""DoctorProfiles""
                  SET ""Specialty"" = 'Cardiologist'
                  WHERE ""Specialty"" = 'Cardiology';");

            // Update Dermatology -> Dermatologist
            migrationBuilder.Sql(
                @"UPDATE ""DoctorProfiles""
                  SET ""Specialty"" = 'Dermatologist'
                  WHERE ""Specialty"" = 'Dermatology';");

            // Note: Other specialties are already correctly named:
            // - General Practitioner
            // - Pediatrician
            // - Orthopedic Surgeon
            // - Psychiatrist
            // - Gynecologist
            // - Neurologist
            // - Oncologist
            // - Ophthalmologist
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Cardiologist -> Cardiology
            migrationBuilder.Sql(
                @"UPDATE ""DoctorProfiles""
                  SET ""Specialty"" = 'Cardiology'
                  WHERE ""Specialty"" = 'Cardiologist';");

            // Revert Dermatologist -> Dermatology
            migrationBuilder.Sql(
                @"UPDATE ""DoctorProfiles""
                  SET ""Specialty"" = 'Dermatology'
                  WHERE ""Specialty"" = 'Dermatologist';");
        }
    }
}