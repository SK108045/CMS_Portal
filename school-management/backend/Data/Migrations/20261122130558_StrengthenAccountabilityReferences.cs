using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenAccountabilityReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "PettyTransactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer"
            );

            migrationBuilder.Sql(
                "UPDATE \"PettyTransactions\" SET \"ApprovedBy\" = NULL WHERE \"ApprovedBy\" = 0"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedAt",
                table: "Suppliers",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedBy",
                table: "Suppliers",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CreatedAt",
                table: "Requests",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CreatedBy",
                table: "Requests",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestedBy",
                table: "Requests",
                column: "RequestedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_CreatedAt",
                table: "RequestItems",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RequestItems_CreatedBy",
                table: "RequestItems",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyTransactions_ApprovedBy",
                table: "PettyTransactions",
                column: "ApprovedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyTransactions_CreatedAt",
                table: "PettyTransactions",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyTransactions_CreatedBy",
                table: "PettyTransactions",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyAccounts_CreatedAt",
                table: "PettyAccounts",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyAccounts_CreatedBy",
                table: "PettyAccounts",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ApprovedBy",
                table: "Payments",
                column: "ApprovedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedBy",
                table: "Payments",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CreatedAt",
                table: "Movements",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CreatedBy",
                table: "Movements",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CreatedAt",
                table: "Locations",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CreatedBy",
                table: "Locations",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedAt",
                table: "Invoices",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedBy",
                table: "Invoices",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_CreatedAt",
                table: "Inventory",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_CreatedBy",
                table: "Inventory",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Income_CreatedAt",
                table: "Income",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Income_CreatedBy",
                table: "Income",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ApprovedBy",
                table: "Expenses",
                column: "ApprovedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatedAt",
                table: "Expenses",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatedBy",
                table: "Expenses",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedBy",
                table: "Documents",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedAt",
                table: "Departments",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedBy",
                table: "Departments",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedAt",
                table: "Categories",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedBy",
                table: "Categories",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CreatedAt",
                table: "Budgets",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CreatedBy",
                table: "Budgets",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Audit_CreatedAt",
                table: "Audit",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Audit_CreatedBy",
                table: "Audit",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CreatedAt",
                table: "Assets",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CreatedBy",
                table: "Assets",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_CreatedAt",
                table: "Approvals",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_CreatedBy",
                table: "Approvals",
                column: "CreatedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_CreatedAt",
                table: "Adjustments",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_CreatedBy",
                table: "Adjustments",
                column: "CreatedBy"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Adjustments_Users_CreatedBy",
                table: "Adjustments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Users_CreatedBy",
                table: "Approvals",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Users_CreatedBy",
                table: "Assets",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Audit_Users_CreatedBy",
                table: "Audit",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Users_CreatedBy",
                table: "Budgets",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_CreatedBy",
                table: "Categories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_CreatedBy",
                table: "Documents",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_ApprovedBy",
                table: "Expenses",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_CreatedBy",
                table: "Expenses",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Income_Users_CreatedBy",
                table: "Income",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Users_CreatedBy",
                table: "Inventory",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedBy",
                table: "Invoices",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Users_CreatedBy",
                table: "Locations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Movements_Users_CreatedBy",
                table: "Movements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ApprovedBy",
                table: "Payments",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_CreatedBy",
                table: "Payments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PettyAccounts_Users_CreatedBy",
                table: "PettyAccounts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PettyTransactions_Users_ApprovedBy",
                table: "PettyTransactions",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PettyTransactions_Users_CreatedBy",
                table: "PettyTransactions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_RequestItems_Users_CreatedBy",
                table: "RequestItems",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_CreatedBy",
                table: "Requests",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestedBy",
                table: "Requests",
                column: "RequestedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Users_CreatedBy",
                table: "Suppliers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adjustments_Users_CreatedBy",
                table: "Adjustments"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Users_CreatedBy",
                table: "Approvals"
            );

            migrationBuilder.DropForeignKey(name: "FK_Assets_Users_CreatedBy", table: "Assets");

            migrationBuilder.DropForeignKey(name: "FK_Audit_Users_CreatedBy", table: "Audit");

            migrationBuilder.DropForeignKey(name: "FK_Budgets_Users_CreatedBy", table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Users_CreatedBy",
                table: "Categories"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_CreatedBy",
                table: "Documents"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_ApprovedBy",
                table: "Expenses"
            );

            migrationBuilder.DropForeignKey(name: "FK_Expenses_Users_CreatedBy", table: "Expenses");

            migrationBuilder.DropForeignKey(name: "FK_Income_Users_CreatedBy", table: "Income");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Users_CreatedBy",
                table: "Inventory"
            );

            migrationBuilder.DropForeignKey(name: "FK_Invoices_Users_CreatedBy", table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Users_CreatedBy",
                table: "Locations"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Movements_Users_CreatedBy",
                table: "Movements"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ApprovedBy",
                table: "Payments"
            );

            migrationBuilder.DropForeignKey(name: "FK_Payments_Users_CreatedBy", table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PettyAccounts_Users_CreatedBy",
                table: "PettyAccounts"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_PettyTransactions_Users_ApprovedBy",
                table: "PettyTransactions"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_PettyTransactions_Users_CreatedBy",
                table: "PettyTransactions"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_RequestItems_Users_CreatedBy",
                table: "RequestItems"
            );

            migrationBuilder.DropForeignKey(name: "FK_Requests_Users_CreatedBy", table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestedBy",
                table: "Requests"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Users_CreatedBy",
                table: "Suppliers"
            );

            migrationBuilder.DropForeignKey(name: "FK_Users_Users_CreatedBy", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Users_CreatedAt", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Users_CreatedBy", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Suppliers_CreatedAt", table: "Suppliers");

            migrationBuilder.DropIndex(name: "IX_Suppliers_CreatedBy", table: "Suppliers");

            migrationBuilder.DropIndex(name: "IX_Requests_CreatedAt", table: "Requests");

            migrationBuilder.DropIndex(name: "IX_Requests_CreatedBy", table: "Requests");

            migrationBuilder.DropIndex(name: "IX_Requests_RequestedBy", table: "Requests");

            migrationBuilder.DropIndex(name: "IX_RequestItems_CreatedAt", table: "RequestItems");

            migrationBuilder.DropIndex(name: "IX_RequestItems_CreatedBy", table: "RequestItems");

            migrationBuilder.DropIndex(
                name: "IX_PettyTransactions_ApprovedBy",
                table: "PettyTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_PettyTransactions_CreatedAt",
                table: "PettyTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_PettyTransactions_CreatedBy",
                table: "PettyTransactions"
            );

            migrationBuilder.DropIndex(name: "IX_PettyAccounts_CreatedAt", table: "PettyAccounts");

            migrationBuilder.DropIndex(name: "IX_PettyAccounts_CreatedBy", table: "PettyAccounts");

            migrationBuilder.DropIndex(name: "IX_Payments_ApprovedBy", table: "Payments");

            migrationBuilder.DropIndex(name: "IX_Payments_CreatedAt", table: "Payments");

            migrationBuilder.DropIndex(name: "IX_Payments_CreatedBy", table: "Payments");

            migrationBuilder.DropIndex(name: "IX_Movements_CreatedAt", table: "Movements");

            migrationBuilder.DropIndex(name: "IX_Movements_CreatedBy", table: "Movements");

            migrationBuilder.DropIndex(name: "IX_Locations_CreatedAt", table: "Locations");

            migrationBuilder.DropIndex(name: "IX_Locations_CreatedBy", table: "Locations");

            migrationBuilder.DropIndex(name: "IX_Invoices_CreatedAt", table: "Invoices");

            migrationBuilder.DropIndex(name: "IX_Invoices_CreatedBy", table: "Invoices");

            migrationBuilder.DropIndex(name: "IX_Inventory_CreatedAt", table: "Inventory");

            migrationBuilder.DropIndex(name: "IX_Inventory_CreatedBy", table: "Inventory");

            migrationBuilder.DropIndex(name: "IX_Income_CreatedAt", table: "Income");

            migrationBuilder.DropIndex(name: "IX_Income_CreatedBy", table: "Income");

            migrationBuilder.DropIndex(name: "IX_Expenses_ApprovedBy", table: "Expenses");

            migrationBuilder.DropIndex(name: "IX_Expenses_CreatedAt", table: "Expenses");

            migrationBuilder.DropIndex(name: "IX_Expenses_CreatedBy", table: "Expenses");

            migrationBuilder.DropIndex(name: "IX_Documents_CreatedAt", table: "Documents");

            migrationBuilder.DropIndex(name: "IX_Documents_CreatedBy", table: "Documents");

            migrationBuilder.DropIndex(name: "IX_Departments_CreatedAt", table: "Departments");

            migrationBuilder.DropIndex(name: "IX_Departments_CreatedBy", table: "Departments");

            migrationBuilder.DropIndex(name: "IX_Categories_CreatedAt", table: "Categories");

            migrationBuilder.DropIndex(name: "IX_Categories_CreatedBy", table: "Categories");

            migrationBuilder.DropIndex(name: "IX_Budgets_CreatedAt", table: "Budgets");

            migrationBuilder.DropIndex(name: "IX_Budgets_CreatedBy", table: "Budgets");

            migrationBuilder.DropIndex(name: "IX_Audit_CreatedAt", table: "Audit");

            migrationBuilder.DropIndex(name: "IX_Audit_CreatedBy", table: "Audit");

            migrationBuilder.DropIndex(name: "IX_Assets_CreatedAt", table: "Assets");

            migrationBuilder.DropIndex(name: "IX_Assets_CreatedBy", table: "Assets");

            migrationBuilder.DropIndex(name: "IX_Approvals_CreatedAt", table: "Approvals");

            migrationBuilder.DropIndex(name: "IX_Approvals_CreatedBy", table: "Approvals");

            migrationBuilder.DropIndex(name: "IX_Adjustments_CreatedAt", table: "Adjustments");

            migrationBuilder.DropIndex(name: "IX_Adjustments_CreatedBy", table: "Adjustments");

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "PettyTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );
        }
    }
}
