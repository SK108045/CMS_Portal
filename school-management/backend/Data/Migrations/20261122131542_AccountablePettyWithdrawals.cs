using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountablePettyWithdrawals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WithdrawalId",
                table: "PettyTransactions",
                type: "integer",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyTransactions_WithdrawalId",
                table: "PettyTransactions",
                column: "WithdrawalId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PettyTransactions_PettyTransactions_WithdrawalId",
                table: "PettyTransactions",
                column: "WithdrawalId",
                principalTable: "PettyTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PettyTransactions_PettyTransactions_WithdrawalId",
                table: "PettyTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_PettyTransactions_WithdrawalId",
                table: "PettyTransactions"
            );

            migrationBuilder.DropColumn(name: "WithdrawalId", table: "PettyTransactions");
        }
    }
}
