using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyDisk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Folders_FolderId",
                table: "ShareLinks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Folders_FolderId",
                table: "ShareLinks",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Folders_FolderId",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "BannedAt",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Folders_FolderId",
                table: "ShareLinks",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id");
        }
    }
}
