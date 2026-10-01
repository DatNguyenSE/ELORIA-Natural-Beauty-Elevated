using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDynamicSeedAndWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "STATIC-GUID-CON-67890", "AQAAAAIAAYagAAAAELuWf8X8+7J8J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+A==", "STATIC-GUID-SEC-12345" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e9b846ed-6e4b-4895-a805-d6e0c3cb436b", "AQAAAAIAAYagAAAAEKHuMlEcEes96yDcglMSYFJNkcK9Sl4beTY+HErhVdfUu3LyWcBsvKEWNb8dou4MvA==", "56299696-6088-43da-827d-092592078696" });
        }
    }
}
