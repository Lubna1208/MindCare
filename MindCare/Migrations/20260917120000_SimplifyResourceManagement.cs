using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MindCare.Data;

#nullable disable

namespace MindCare.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260917120000_SimplifyResourceManagement")]
public partial class SimplifyResourceManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ResourceReviewLogs");
        migrationBuilder.DropColumn(name: "ViewCount", table: "Resources");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ViewCount", table: "Resources", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.CreateTable(
            name: "ResourceReviewLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ResourceId = table.Column<int>(type: "int", nullable: false),
                AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResourceReviewLogs", x => x.Id);
                table.ForeignKey(name: "FK_ResourceReviewLogs_Resources_ResourceId", column: x => x.ResourceId, principalTable: "Resources", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_ResourceReviewLogs_ResourceId", table: "ResourceReviewLogs", column: "ResourceId");
    }
}
