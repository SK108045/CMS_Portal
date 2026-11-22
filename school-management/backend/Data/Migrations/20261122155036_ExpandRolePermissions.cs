using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve legacy accounts and IDs. Quarantine malformed unassigned legacy accounts for Admin review.
            migrationBuilder.Sql("""
                INSERT INTO "Departments" ("Name","Head","Active","CreatedAt","CreatedBy")
                SELECT 'Unassigned (migration)','Admin review required',false,CURRENT_TIMESTAMP,NULL
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "Role"='USER' AND "DepartmentId" IS NULL)
                ON CONFLICT ("Name") DO NOTHING;
                UPDATE "Users" SET "DepartmentId"=(SELECT "Id" FROM "Departments" WHERE "Name"='Unassigned (migration)'),"Active"=false
                WHERE "Role"='USER' AND "DepartmentId" IS NULL;
                INSERT INTO "Audit" ("Actor","Action","Entity","EntityId","DepartmentId","Description","OldValues","NewValues","CreatedAt","CreatedBy")
                SELECT 'Migration','Role migrated','users',"Id","DepartmentId",'Legacy USER safely mapped to STAFF','USER','STAFF',CURRENT_TIMESTAMP,NULL FROM "Users" WHERE "Role"='USER';
                UPDATE "Users" SET "Role"='STAFF',"SessionVersion"="SessionVersion"+1 WHERE "Role"='USER';
                """);
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Department",
                table: "Users",
                sql: "\"Role\" NOT IN ('STAFF','DEPARTMENT_HEAD') OR \"DepartmentId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "\"Role\" IN ('ADMIN','FINANCE','STOREKEEPER','DEPARTMENT_HEAD','STAFF','AUDITOR')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Department",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\"='USER',\"SessionVersion\"=\"SessionVersion\"+1 WHERE \"Role\"<>'ADMIN'");
        }
    }
}
