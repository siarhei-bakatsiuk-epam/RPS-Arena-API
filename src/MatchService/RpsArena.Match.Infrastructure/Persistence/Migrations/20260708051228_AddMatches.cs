using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RpsArena.Match.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_one_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_two_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_one_score = table.Column<int>(type: "integer", nullable: false),
                    player_two_score = table.Column<int>(type: "integer", nullable: false),
                    played_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                    table.CheckConstraint("ck_matches_no_self_play", "player_one_id <> player_two_id");
                    table.CheckConstraint("ck_matches_scores_non_negative", "player_one_score >= 0 AND player_two_score >= 0");
                    table.ForeignKey(
                        name: "fk_matches_players_player_one_id",
                        column: x => x.player_one_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_matches_players_player_two_id",
                        column: x => x.player_two_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_matches_idempotency_key",
                table: "matches",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_matches_played_at",
                table: "matches",
                column: "played_at");

            migrationBuilder.CreateIndex(
                name: "ix_matches_player_one_id",
                table: "matches",
                column: "player_one_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_player_two_id",
                table: "matches",
                column: "player_two_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matches");
        }
    }
}
