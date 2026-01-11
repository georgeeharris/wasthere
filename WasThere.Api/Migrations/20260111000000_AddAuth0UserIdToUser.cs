using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasThere.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuth0UserIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Auth0UserId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Auth0UserId",
                table: "Users",
                column: "Auth0UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Auth0UserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Auth0UserId",
                table: "Users");
        }
    }
}
