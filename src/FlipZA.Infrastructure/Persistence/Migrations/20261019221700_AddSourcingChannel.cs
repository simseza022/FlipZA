using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlipZA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourcingChannel",
                table: "MarketplaceProducts",
                type: "TEXT",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourcingChannel",
                table: "MarketplaceProducts");
        }
    }
}
