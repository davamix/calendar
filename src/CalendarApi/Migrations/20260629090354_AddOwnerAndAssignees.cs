using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerAndAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "tasks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "tasks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "projects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "projects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_assignees",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_assignees", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_project_assignees_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_assignees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_assignees",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assignees", x => new { x.TaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_task_assignees_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_assignees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_OwnerId",
                table: "tasks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_OwnerId",
                table: "projects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_project_assignees_UserId",
                table: "project_assignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignees_UserId",
                table: "task_assignees",
                column: "UserId");

            // Backfill: any rows that predate the owner column (OwnerId defaulted to '') get a
            // placeholder owner + a matching assignee row, so the owner FK below is satisfied and
            // the rows stay visible to that user. No-op on a fresh database.
            migrationBuilder.Sql(
                """
                INSERT INTO users ("Id", "DisplayName") VALUES ('legacy-owner', 'Legacy Owner')
                    ON CONFLICT ("Id") DO NOTHING;
                UPDATE projects SET "OwnerId" = 'legacy-owner', "CreatedBy" = 'legacy-owner', "CreatedAt" = now() WHERE "OwnerId" = '';
                UPDATE tasks     SET "OwnerId" = 'legacy-owner', "CreatedBy" = 'legacy-owner', "CreatedAt" = now() WHERE "OwnerId" = '';
                INSERT INTO project_assignees ("ProjectId", "UserId")
                    SELECT "Id", 'legacy-owner' FROM projects WHERE "OwnerId" = 'legacy-owner'
                    ON CONFLICT DO NOTHING;
                INSERT INTO task_assignees ("TaskId", "UserId")
                    SELECT "Id", 'legacy-owner' FROM tasks WHERE "OwnerId" = 'legacy-owner'
                    ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_users_OwnerId",
                table: "projects",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_users_OwnerId",
                table: "tasks",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_users_OwnerId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_OwnerId",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "project_assignees");

            migrationBuilder.DropTable(
                name: "task_assignees");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_tasks_OwnerId",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_projects_OwnerId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "projects");
        }
    }
}
