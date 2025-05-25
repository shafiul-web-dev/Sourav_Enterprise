using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sourav_Enterprise.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemsNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderID1",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderID1",
                table: "OrderItems",
                column: "OrderID1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderID1",
                table: "OrderItems",
                column: "OrderID1",
                principalTable: "Orders",
                principalColumn: "OrderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderID1",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderID1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderID1",
                table: "OrderItems");
        }
    }
}
