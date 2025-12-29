using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WasThere.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFlyerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlyerId",
                table: "ClubNights",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Flyers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    EarliestClubNightDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flyers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flyers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flyers_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubNights_FlyerId",
                table: "ClubNights",
                column: "FlyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Flyers_EventId",
                table: "Flyers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Flyers_VenueId",
                table: "Flyers",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubNights_Flyers_FlyerId",
                table: "ClubNights",
                column: "FlyerId",
                principalTable: "Flyers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubNights_Flyers_FlyerId",
                table: "ClubNights");

            migrationBuilder.DropTable(
                name: "Flyers");

            migrationBuilder.DropIndex(
                name: "IX_ClubNights_FlyerId",
                table: "ClubNights");

            migrationBuilder.DropColumn(
                name: "FlyerId",
                table: "ClubNights");
        }
    }
}
