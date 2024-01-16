using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalkWaveApi.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Channels");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
