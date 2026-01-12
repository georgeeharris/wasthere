using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WasThere.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClubNightPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubNightPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubNightId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    QuotedPostId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubNightPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubNightPosts_ClubNights_ClubNightId",
                        column: x => x.ClubNightId,
                        principalTable: "ClubNights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubNightPosts_ClubNightPosts_QuotedPostId",
                        column: x => x.QuotedPostId,
                        principalTable: "ClubNightPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubNightPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubNightPosts_ClubNightId",
                table: "ClubNightPosts",
                column: "ClubNightId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubNightPosts_QuotedPostId",
                table: "ClubNightPosts",
                column: "QuotedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubNightPosts_UserId",
                table: "ClubNightPosts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubNightPosts");
        }
    }
}
