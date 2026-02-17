using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityApp.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Specialty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Qualifications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    Services = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConsultationFee = table.Column<decimal>(type: "numeric", nullable: true),
                    Languages = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAvailableForAppointments = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "2dc0e479-2751-4b5e-8cd4-fc2113d61301", new DateTime(2026, 2, 16, 13, 30, 3, 587, DateTimeKind.Utc).AddTicks(9711) });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "d3e80a47-5dd0-4a50-baf8-118fd8ee44f7", new DateTime(2026, 2, 16, 13, 30, 3, 588, DateTimeKind.Utc).AddTicks(9166) });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "c2963b40-13ba-4d2c-9140-61b4ee0a2b66", new DateTime(2026, 2, 16, 13, 30, 3, 588, DateTimeKind.Utc).AddTicks(9310) });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_City",
                table: "DoctorProfiles",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_Specialty",
                table: "DoctorProfiles",
                column: "Specialty");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_UserId",
                table: "DoctorProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProfiles");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "4f696a87-2fb7-44de-a420-2efaa38ce44c", new DateTime(2026, 2, 14, 14, 58, 12, 367, DateTimeKind.Utc).AddTicks(6881) });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "c78d1277-9c95-4da0-a518-1d494204223f", new DateTime(2026, 2, 14, 14, 58, 12, 368, DateTimeKind.Utc).AddTicks(5729) });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ConcurrencyStamp", "CreatedAt" },
                values: new object[] { "4687feb8-2733-4550-82ec-e2345ee77fc6", new DateTime(2026, 2, 14, 14, 58, 12, 368, DateTimeKind.Utc).AddTicks(5762) });
        }
    }
}
