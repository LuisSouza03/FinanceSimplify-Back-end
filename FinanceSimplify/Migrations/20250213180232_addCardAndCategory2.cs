using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSimplify.Migrations
{
    /// <inheritdoc />
    public partial class addCardAndCategory2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_CardModel_CardId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_CategoryModel_CategoryId",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryModel",
                table: "CategoryModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CardModel",
                table: "CardModel");

            migrationBuilder.RenameTable(
                name: "CategoryModel",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "CardModel",
                newName: "Card");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Card",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Card",
                table: "Card",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Card_CardId",
                table: "Transaction",
                column: "CardId",
                principalTable: "Card",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Categories_CategoryId",
                table: "Transaction",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Card_CardId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Categories_CategoryId",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Card",
                table: "Card");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "CategoryModel");

            migrationBuilder.RenameTable(
                name: "Card",
                newName: "CardModel");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "CardModel",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryModel",
                table: "CategoryModel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CardModel",
                table: "CardModel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_CardModel_CardId",
                table: "Transaction",
                column: "CardId",
                principalTable: "CardModel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_CategoryModel_CategoryId",
                table: "Transaction",
                column: "CategoryId",
                principalTable: "CategoryModel",
                principalColumn: "Id");
        }
    }
}
