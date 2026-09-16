using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MindCare.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceResourceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Resources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "Resources",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Resources",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Resources",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByAdminId",
                table: "Resources",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Resources",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Historic resources were already public.  Preserve that behaviour while
            // the application seed process links their legacy category text to a row.
            migrationBuilder.Sql("UPDATE [Resources] SET [Status] = 2, [CreatedByRole] = 'System', [UpdatedAt] = [CreatedAt], [PublishedAt] = [CreatedAt]");

            migrationBuilder.CreateTable(
                name: "ResourceBookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceBookmarks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceReviewLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceReviewLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceReviewLogs_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CategoryId",
                table: "Resources",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CreatedByUserId",
                table: "Resources",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ReviewedByAdminId",
                table: "Resources",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Status_CategoryId",
                table: "Resources",
                columns: new[] { "Status", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_ResourceId",
                table: "ResourceBookmarks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_UserId_ResourceId",
                table: "ResourceBookmarks",
                columns: new[] { "UserId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_Name",
                table: "ResourceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReviewLogs_ResourceId",
                table: "ResourceReviewLogs",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_AspNetUsers_CreatedByUserId",
                table: "Resources",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_AspNetUsers_ReviewedByAdminId",
                table: "Resources",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_ResourceCategories_CategoryId",
                table: "Resources",
                column: "CategoryId",
                principalTable: "ResourceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_AspNetUsers_CreatedByUserId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_AspNetUsers_ReviewedByAdminId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_ResourceCategories_CategoryId",
                table: "Resources");

            migrationBuilder.DropTable(
                name: "ResourceBookmarks");

            migrationBuilder.DropTable(
                name: "ResourceCategories");

            migrationBuilder.DropTable(
                name: "ResourceReviewLogs");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CategoryId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CreatedByUserId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ReviewedByAdminId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Status_CategoryId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Resources");
        }
    }
}
