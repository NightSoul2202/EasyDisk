using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyDisk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTagEntityFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ColorHex",
                table: "Tags",
                newName: "Color");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tags");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "Tags",
                newName: "ColorHex");
        }
    }
}
