using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_stats",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    total_matches = table.Column<int>(type: "integer", nullable: false),
                    match_points = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_stats", x => x.player_id);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_messages", x => x.message_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_stats_match_points_total_score",
                table: "player_stats",
                columns: new[] { "match_points", "total_score" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_stats");

            migrationBuilder.DropTable(
                name: "processed_messages");
        }
    }
}
