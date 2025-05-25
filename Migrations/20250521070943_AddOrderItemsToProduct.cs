using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sourav_Enterprise.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductID1",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductID1",
                table: "OrderItems",
                column: "ProductID1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductID1",
                table: "OrderItems",
                column: "ProductID1",
                principalTable: "Products",
                principalColumn: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductID1",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductID1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductID1",
                table: "OrderItems");
        }
    }
}
