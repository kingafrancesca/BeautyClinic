using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyClinic.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdToBookingsAndUpdateRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EmployeeId",
                table: "Bookings",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_EmployeeId",
                table: "Bookings",
                column: "EmployeeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_EmployeeId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_EmployeeId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Bookings");
        }
    }
}
