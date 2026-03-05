using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentApp.Postgres.Migrations
{
    /// <summary>
    /// Migration to standardize specialty names across the system
    /// Ensures consistent naming convention using -ologist/-ician suffixes
    /// </summary>
    public partial class StandardizeSpecializationNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Cardiology -> Cardiologist
            migrationBuilder.Sql(
                @"UPDATE ""Professionals""
                  SET ""Specialization"" = 'Cardiologist'
                  WHERE ""Specialization"" = 'Cardiology';");

            // Update Dermatology -> Dermatologist
            migrationBuilder.Sql(
                @"UPDATE ""Professionals""
                  SET ""Specialization"" = 'Dermatologist'
                  WHERE ""Specialization"" = 'Dermatology';");

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
                @"UPDATE ""Professionals""
                  SET ""Specialization"" = 'Cardiology'
                  WHERE ""Specialization"" = 'Cardiologist';");

            // Revert Dermatologist -> Dermatology
            migrationBuilder.Sql(
                @"UPDATE ""Professionals""
                  SET ""Specialization"" = 'Dermatology'
                  WHERE ""Specialization"" = 'Dermatologist';");
        }
    }
}