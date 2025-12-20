using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVisualMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductVisualMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CategoryAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubCategoryAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubCategoryEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorsAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorsEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StyleAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StyleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatternAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatternEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaptionAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeywordsAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeywordsEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVisualMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVisualMetadata_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVisualMetadata_ProductId",
                table: "ProductVisualMetadata",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVisualMetadata");
        }
    }
}
