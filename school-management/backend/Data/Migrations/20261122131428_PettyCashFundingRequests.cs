using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace School.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PettyCashFundingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PettyRequests",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DecisionNotes = table.Column<string>(type: "text", nullable: false),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PettyRequests_PettyAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "PettyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_PettyRequests_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_PettyRequests_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyRequests_AccountId",
                table: "PettyRequests",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyRequests_ApprovedBy",
                table: "PettyRequests",
                column: "ApprovedBy"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyRequests_CreatedAt",
                table: "PettyRequests",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PettyRequests_CreatedBy",
                table: "PettyRequests",
                column: "CreatedBy"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PettyRequests");
        }
    }
}
