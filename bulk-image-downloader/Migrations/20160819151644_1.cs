using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BulkMediaDownloader.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Downloadables",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DownloadDir = table.Column<string>(nullable: true),
                    Length = table.Column<long>(nullable: false),
                    RefererURLString = table.Column<string>(nullable: true),
                    SimpleHeaders = table.Column<bool>(nullable: false),
                    SiteString = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false),
                    URLString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Downloadables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DownloadableSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DownloadDir = table.Column<string>(nullable: true),
                    MediaSourceName = table.Column<string>(nullable: true),
                    RefererURLString = table.Column<string>(nullable: true),
                    SourceStage = table.Column<string>(nullable: true),
                    SourceURLString = table.Column<string>(nullable: true),
                    State = table.Column<int>(nullable: false),
                    URLString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadableSources", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Downloadables");

            migrationBuilder.DropTable(
                name: "DownloadableSources");
        }
    }
}
