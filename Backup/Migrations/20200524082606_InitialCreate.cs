using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backup.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupCollections",
                columns: table => new
                {
                    BackupCollectionId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(nullable: true),
                    SourceDirectory = table.Column<string>(nullable: true),
                    Started = table.Column<DateTime>(nullable: true),
                    Finished = table.Column<DateTime>(nullable: true),
                    BaseDirectory = table.Column<string>(nullable: true),
                    IncludePattern = table.Column<string>(nullable: true),
                    ExcludePattern = table.Column<string>(nullable: true),
                    AutomaticBackup = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupCollections", x => x.BackupCollectionId);
                });

            migrationBuilder.CreateTable(
                name: "DestinationDirectories",
                columns: table => new
                {
                    DestinationDirectoryId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackupCollectionId = table.Column<long>(nullable: false),
                    PathName = table.Column<string>(nullable: true),
                    Copied = table.Column<int>(nullable: false),
                    Started = table.Column<DateTime>(nullable: true),
                    Finished = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationDirectories", x => x.DestinationDirectoryId);
                    table.ForeignKey(
                        name: "FK_DestinationDirectories_BackupCollections_BackupCollectionId",
                        column: x => x.BackupCollectionId,
                        principalTable: "BackupCollections",
                        principalColumn: "BackupCollectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceFiles",
                columns: table => new
                {
                    SourceFileId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackupCollectionId = table.Column<long>(nullable: false),
                    PathName = table.Column<string>(nullable: true),
                    ContentHash = table.Column<string>(nullable: true),
                    LastWrittenTimeUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceFiles", x => x.SourceFileId);
                    table.ForeignKey(
                        name: "FK_SourceFiles_BackupCollections_BackupCollectionId",
                        column: x => x.BackupCollectionId,
                        principalTable: "BackupCollections",
                        principalColumn: "BackupCollectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CopyFailures",
                columns: table => new
                {
                    CopyFailureId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceFileId = table.Column<long>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true),
                    DestinationDirectoryId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopyFailures", x => x.CopyFailureId);
                    table.ForeignKey(
                        name: "FK_CopyFailures_DestinationDirectories_DestinationDirectoryId",
                        column: x => x.DestinationDirectoryId,
                        principalTable: "DestinationDirectories",
                        principalColumn: "DestinationDirectoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestinationFiles",
                columns: table => new
                {
                    DestinationFileId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceFileId = table.Column<long>(nullable: false),
                    PathName = table.Column<string>(nullable: true),
                    ContentHash = table.Column<string>(nullable: true),
                    DestinationDirectoryId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationFiles", x => x.DestinationFileId);
                    table.ForeignKey(
                        name: "FK_DestinationFiles_DestinationDirectories_DestinationDirectoryId",
                        column: x => x.DestinationDirectoryId,
                        principalTable: "DestinationDirectories",
                        principalColumn: "DestinationDirectoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupCollections_Title",
                table: "BackupCollections",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopyFailures_DestinationDirectoryId",
                table: "CopyFailures",
                column: "DestinationDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DestinationDirectories_BackupCollectionId_PathName",
                table: "DestinationDirectories",
                columns: new[] { "BackupCollectionId", "PathName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DestinationFiles_DestinationDirectoryId",
                table: "DestinationFiles",
                column: "DestinationDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceFiles_BackupCollectionId_PathName",
                table: "SourceFiles",
                columns: new[] { "BackupCollectionId", "PathName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopyFailures");

            migrationBuilder.DropTable(
                name: "DestinationFiles");

            migrationBuilder.DropTable(
                name: "SourceFiles");

            migrationBuilder.DropTable(
                name: "DestinationDirectories");

            migrationBuilder.DropTable(
                name: "BackupCollections");
        }
    }
}
