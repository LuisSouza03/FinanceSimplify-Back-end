using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSimplify.Migrations
{
    /// <inheritdoc />
    public partial class removeCardAndCategoryFromTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Card_CardId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Categories_CategoryId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_CardId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_CategoryId",
                table: "Transaction");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryModelId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_CategoryModelId",
                table: "Transaction",
                column: "CategoryModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Categories_CategoryModelId",
                table: "Transaction",
                column: "CategoryModelId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Categories_CategoryModelId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_CategoryModelId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "CategoryModelId",
                table: "Transaction");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_CardId",
                table: "Transaction",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_CategoryId",
                table: "Transaction",
                column: "CategoryId");

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
    }
}
