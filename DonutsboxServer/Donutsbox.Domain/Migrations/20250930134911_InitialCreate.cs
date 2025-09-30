using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donutsbox.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserTypes_TypeId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UsersAuth_AuthId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Refresh_token_expiry_time",
                table: "UsersAuth",
                newName: "refresh_token_expiry_time");

            migrationBuilder.RenameColumn(
                name: "Refresh_token",
                table: "UsersAuth",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "UsersAuth",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UsersAuth",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LastAuth",
                table: "UsersAuth",
                newName: "last_auth");

            migrationBuilder.RenameColumn(
                name: "AuthEmail",
                table: "UsersAuth",
                newName: "auth_email");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Users",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "TypeId",
                table: "Users",
                newName: "type_id");

            migrationBuilder.RenameColumn(
                name: "AuthId",
                table: "Users",
                newName: "auth_id");

            migrationBuilder.RenameColumn(
                name: "GUID",
                table: "Users",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Users_TypeId",
                table: "Users",
                newName: "IX_Users_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_Users_AuthId",
                table: "Users",
                newName: "IX_Users_auth_id");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ContentPost",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "CommentsCount",
                table: "ContentPost",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "ContentPost",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserTypes_type_id",
                table: "Users",
                column: "type_id",
                principalTable: "UserTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UsersAuth_auth_id",
                table: "Users",
                column: "auth_id",
                principalTable: "UsersAuth",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersAuth_Users_id",
                table: "UsersAuth",
                column: "id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserTypes_type_id",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UsersAuth_auth_id",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersAuth_Users_id",
                table: "UsersAuth");

            migrationBuilder.DropColumn(
                name: "CommentsCount",
                table: "ContentPost");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "ContentPost");

            migrationBuilder.RenameColumn(
                name: "refresh_token_expiry_time",
                table: "UsersAuth",
                newName: "Refresh_token_expiry_time");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "UsersAuth",
                newName: "Refresh_token");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "UsersAuth",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UsersAuth",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_auth",
                table: "UsersAuth",
                newName: "LastAuth");

            migrationBuilder.RenameColumn(
                name: "auth_email",
                table: "UsersAuth",
                newName: "AuthEmail");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "Users",
                newName: "TypeId");

            migrationBuilder.RenameColumn(
                name: "auth_id",
                table: "Users",
                newName: "AuthId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "GUID");

            migrationBuilder.RenameIndex(
                name: "IX_Users_type_id",
                table: "Users",
                newName: "IX_Users_TypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_auth_id",
                table: "Users",
                newName: "IX_Users_AuthId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ContentPost",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserTypes_TypeId",
                table: "Users",
                column: "TypeId",
                principalTable: "UserTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UsersAuth_AuthId",
                table: "Users",
                column: "AuthId",
                principalTable: "UsersAuth",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
