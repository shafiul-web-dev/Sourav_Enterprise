using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sourav_Enterprise.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOrdersRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserID1",
                table: "Orders",
                column: "UserID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserID1",
                table: "Orders",
                column: "UserID1",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserID1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserID1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserID1",
                table: "Orders");
        }
    }
}
