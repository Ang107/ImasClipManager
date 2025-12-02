using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImasClipManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisplayStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Context = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyKey = table.Column<string>(type: "TEXT", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Performers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Yomi = table.Column<string>(type: "TEXT", nullable: false),
                    LiveType = table.Column<int>(type: "INTEGER", nullable: false),
                    Brand = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Spaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SpaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    StartTimeMs = table.Column<long>(type: "INTEGER", nullable: true),
                    EndTimeMs = table.Column<long>(type: "INTEGER", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ConcertName = table.Column<string>(type: "TEXT", nullable: false),
                    Brands = table.Column<int>(type: "INTEGER", nullable: false),
                    LiveType = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcertDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SongTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsAutoThumbnail = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Lyrics = table.Column<string>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clips_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SpaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SortIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlists_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClipPerformer",
                columns: table => new
                {
                    ClipsId = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClipPerformer", x => new { x.ClipsId, x.PerformersId });
                    table.ForeignKey(
                        name: "FK_ClipPerformer_Clips_ClipsId",
                        column: x => x.ClipsId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClipPerformer_Performers_PerformersId",
                        column: x => x.PerformersId,
                        principalTable: "Performers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClipPlaylist",
                columns: table => new
                {
                    ClipsId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaylistsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClipPlaylist", x => new { x.ClipsId, x.PlaylistsId });
                    table.ForeignKey(
                        name: "FK_ClipPlaylist_Clips_ClipsId",
                        column: x => x.ClipsId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClipPlaylist_Playlists_PlaylistsId",
                        column: x => x.PlaylistsId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClipPerformer_PerformersId",
                table: "ClipPerformer",
                column: "PerformersId");

            migrationBuilder.CreateIndex(
                name: "IX_ClipPlaylist_PlaylistsId",
                table: "ClipPlaylist",
                column: "PlaylistsId");

            migrationBuilder.CreateIndex(
                name: "IX_Clips_SpaceId",
                table: "Clips",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_SpaceId",
                table: "Playlists",
                column: "SpaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClipPerformer");

            migrationBuilder.DropTable(
                name: "ClipPlaylist");

            migrationBuilder.DropTable(
                name: "DisplayStates");

            migrationBuilder.DropTable(
                name: "Performers");

            migrationBuilder.DropTable(
                name: "Clips");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "Spaces");
        }
    }
}
