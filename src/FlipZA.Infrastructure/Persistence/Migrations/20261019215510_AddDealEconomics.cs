using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlipZA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDealEconomics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EstimatedNetProceeds",
                table: "ListingSnapshots",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxBuyPrice",
                table: "ListingSnapshots",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedNetProceeds",
                table: "ListingSnapshots");

            migrationBuilder.DropColumn(
                name: "MaxBuyPrice",
                table: "ListingSnapshots");
        }
    }
}
