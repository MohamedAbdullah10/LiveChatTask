using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveChatTask.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessionSessionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionKey",
                table: "ChatSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SessionKey",
                table: "ChatSessions",
                column: "SessionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_SessionKey",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SessionKey",
                table: "ChatSessions");
        }
    }
}
