using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasThere.Api.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteForFlyers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flyers_Events_EventId",
                table: "Flyers");

            migrationBuilder.DropForeignKey(
                name: "FK_Flyers_Venues_VenueId",
                table: "Flyers");

            migrationBuilder.AddForeignKey(
                name: "FK_Flyers_Events_EventId",
                table: "Flyers",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Flyers_Venues_VenueId",
                table: "Flyers",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flyers_Events_EventId",
                table: "Flyers");

            migrationBuilder.DropForeignKey(
                name: "FK_Flyers_Venues_VenueId",
                table: "Flyers");

            migrationBuilder.AddForeignKey(
                name: "FK_Flyers_Events_EventId",
                table: "Flyers",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Flyers_Venues_VenueId",
                table: "Flyers",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
