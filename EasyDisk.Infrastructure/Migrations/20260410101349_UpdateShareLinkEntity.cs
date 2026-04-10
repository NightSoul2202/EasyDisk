using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyDisk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShareLinkEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "ShareLinks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ClickCount",
                table: "ShareLinks",
                newName: "DownloadCount");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "ShareLinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ShareLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ShareLinks");

            migrationBuilder.RenameColumn(
                name: "DownloadCount",
                table: "ShareLinks",
                newName: "ClickCount");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ShareLinks",
                newName: "ExpiryDate");
        }
    }
}
