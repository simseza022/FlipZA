using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlipZA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Marketplace = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VariantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    Brand = table.Column<string>(type: "TEXT", nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ModelNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    CategoryPath = table.Column<string>(type: "TEXT", nullable: true),
                    LeafCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Dimensions = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<string>(type: "TEXT", nullable: true),
                    Warranty = table.Column<string>(type: "TEXT", nullable: true),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastRawPayload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: true),
                    ListPrice = table.Column<double>(type: "REAL", nullable: true),
                    IsInStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLeadTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    StockStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    DistributionCentres = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsMarketplaceSeller = table.Column<bool>(type: "INTEGER", nullable: false),
                    BuyBoxSellerId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    BuyBoxSellerName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    BuyBoxSellerVatRegistered = table.Column<bool>(type: "INTEGER", nullable: true),
                    BuyBoxSellerRating = table.Column<double>(type: "REAL", nullable: true),
                    StarRating = table.Column<double>(type: "REAL", nullable: true),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OtherOffersCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LowestOtherOfferPrice = table.Column<double>(type: "REAL", nullable: true),
                    PromotionName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingSnapshots_MarketplaceProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "MarketplaceProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetingOffers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SnapshotId = table.Column<long>(type: "INTEGER", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SellerId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SellerName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsMarketplaceOperator = table.Column<bool>(type: "INTEGER", nullable: false),
                    SellerVatRegistered = table.Column<bool>(type: "INTEGER", nullable: true),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    IsInStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    StockStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetingOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetingOffers_ListingSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ListingSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetingOffers_SnapshotId",
                table: "CompetingOffers",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingSnapshots_ProductId_CapturedAtUtc",
                table: "ListingSnapshots",
                columns: new[] { "ProductId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProducts_Barcode",
                table: "MarketplaceProducts",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProducts_Marketplace_ExternalId",
                table: "MarketplaceProducts",
                columns: new[] { "Marketplace", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetingOffers");

            migrationBuilder.DropTable(
                name: "ListingSnapshots");

            migrationBuilder.DropTable(
                name: "MarketplaceProducts");
        }
    }
}
