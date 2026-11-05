using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseSplitterApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedPlaceholderToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_settlements_Groups_GroupId",
                table: "settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_settlements_Users_FromUserId",
                table: "settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_settlements_Users_ToUserId",
                table: "settlements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_settlements",
                table: "settlements");

            migrationBuilder.RenameTable(
                name: "settlements",
                newName: "Settlements");

            migrationBuilder.RenameIndex(
                name: "IX_settlements_ToUserId",
                table: "Settlements",
                newName: "IX_Settlements_ToUserId");

            migrationBuilder.RenameIndex(
                name: "IX_settlements_GroupId",
                table: "Settlements",
                newName: "IX_Settlements_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_settlements_FromUserId",
                table: "Settlements",
                newName: "IX_Settlements_FromUserId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedPlaceholder",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settlements",
                table: "Settlements",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_Groups_GroupId",
                table: "Settlements",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_Users_FromUserId",
                table: "Settlements",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_Users_ToUserId",
                table: "Settlements",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_Groups_GroupId",
                table: "Settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_Users_FromUserId",
                table: "Settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_Users_ToUserId",
                table: "Settlements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Settlements",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "IsDeletedPlaceholder",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Settlements",
                newName: "settlements");

            migrationBuilder.RenameIndex(
                name: "IX_Settlements_ToUserId",
                table: "settlements",
                newName: "IX_settlements_ToUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Settlements_GroupId",
                table: "settlements",
                newName: "IX_settlements_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Settlements_FromUserId",
                table: "settlements",
                newName: "IX_settlements_FromUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_settlements",
                table: "settlements",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_settlements_Groups_GroupId",
                table: "settlements",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_settlements_Users_FromUserId",
                table: "settlements",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_settlements_Users_ToUserId",
                table: "settlements",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
