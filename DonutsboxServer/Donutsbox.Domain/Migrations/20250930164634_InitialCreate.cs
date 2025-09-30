using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Donutsbox.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_post",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    likes_count = table.Column<int>(type: "integer", nullable: false),
                    dislikes_count = table.Column<int>(type: "integer", nullable: false),
                    comments_count = table.Column<int>(type: "integer", nullable: false),
                    audio_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    video_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    picture_urls = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_post", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "creator_page_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    page_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: false),
                    banner_url = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    subscribers_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creator_page_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_comment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_reaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaction_type_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_reaction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reaction_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reaction_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    picture_url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    last_auth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    begin_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscription", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UserTypeId = table.Column<int>(type: "integer", nullable: false),
                    UserAuthId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_user_auth_UserAuthId",
                        column: x => x.UserAuthId,
                        principalTable: "user_auth",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_user_type_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "user_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    notification_email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    payment_info = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_data", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_data_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "user_type",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Creator" },
                    { 3, "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_UserAuthId",
                table: "user",
                column: "UserAuthId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_UserTypeId",
                table: "user",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_user_data_user_id",
                table: "user_data",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_post");

            migrationBuilder.DropTable(
                name: "creator_page_data");

            migrationBuilder.DropTable(
                name: "post_comment");

            migrationBuilder.DropTable(
                name: "post_reaction");

            migrationBuilder.DropTable(
                name: "reaction_type");

            migrationBuilder.DropTable(
                name: "subscription");

            migrationBuilder.DropTable(
                name: "user_data");

            migrationBuilder.DropTable(
                name: "user_subscription");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "user_auth");

            migrationBuilder.DropTable(
                name: "user_type");
        }
    }
}
