using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HerbalWellnessCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Bookings",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Bookings");
        }
    }
}
