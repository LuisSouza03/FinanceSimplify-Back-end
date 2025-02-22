using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSimplify.Migrations
{
    /// <inheritdoc />
    public partial class addBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "Card",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Card");
        }
    }
}
