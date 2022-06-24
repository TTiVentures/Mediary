using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mediary.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissedMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Topic = table.Column<string>(type: "TEXT", nullable: true),
                    Payload = table.Column<byte[]>(type: "BLOB", nullable: true),
                    QualityOfServiceLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Retain = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dup = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissedMessages", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissedMessages");
        }
    }
}
