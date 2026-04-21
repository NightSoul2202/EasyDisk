using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyDisk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShareEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "ShareLinks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "ShareLinks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_FolderId",
                table: "ShareLinks",
                column: "FolderId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Folders_FolderId",
                table: "ShareLinks");

            migrationBuilder.DropIndex(
                name: "IX_ShareLinks_FolderId",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "ShareLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "ShareLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Files_FileId",
                table: "ShareLinks",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
