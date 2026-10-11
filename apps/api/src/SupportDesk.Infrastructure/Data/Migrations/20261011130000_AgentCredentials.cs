using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure.Data;
namespace SupportDesk.Infrastructure.Data.Migrations;
[DbContext(typeof(SupportDbContext))]
[Migration("20261011130000_AgentCredentials")]
public sealed class AgentCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("PasswordHash", "Agents", type: "nvarchar(512)", maxLength: 512, nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("PasswordHash", "Agents");
}
