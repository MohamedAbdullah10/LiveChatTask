using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveChatTask.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSessionDurationMinutes",
                table: "ChatSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxDurationMinutes",
                table: "ChatSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ChatSessions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSessionDurationMinutes",
                table: "ChatSettings");

            migrationBuilder.DropColumn(
                name: "MaxDurationMinutes",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ChatSessions");
        }
    }
}
