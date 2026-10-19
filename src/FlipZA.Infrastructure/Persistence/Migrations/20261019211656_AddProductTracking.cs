using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlipZA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "MarketplaceProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProducts_Marketplace_IsTracked_LastSeenAtUtc",
                table: "MarketplaceProducts",
                columns: new[] { "Marketplace", "IsTracked", "LastSeenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketplaceProducts_Marketplace_IsTracked_LastSeenAtUtc",
                table: "MarketplaceProducts");

            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "MarketplaceProducts");
        }
    }
}
