using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class AddHardMute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHardMute",
                table: "UnmuteTimer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHardMute",
                table: "MutedUserId",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ulong>(
                name: "ReactionRemovedId",
                table: "LogSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyMuteRebornTicketCost",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EachTicketDecreaseMuteTime",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EachTicketIncreaseMuteTime",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableMuteReborn",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxIncreaseMuteTime",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRegex",
                table: "Expressions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OwnerOnly",
                table: "Expressions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ulong>(
                name: "UseCount",
                table: "Expressions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateTable(
                name: "MuteRebornTicket",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RebornTicketNum = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuteRebornTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuteRebornTicket_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuteRebornTicket_GuildConfigId",
                table: "MuteRebornTicket",
                column: "GuildConfigId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuteRebornTicket");

            migrationBuilder.DropColumn(
                name: "IsHardMute",
                table: "UnmuteTimer");

            migrationBuilder.DropColumn(
                name: "IsHardMute",
                table: "MutedUserId");

            migrationBuilder.DropColumn(
                name: "ReactionRemovedId",
                table: "LogSettings");

            migrationBuilder.DropColumn(
                name: "BuyMuteRebornTicketCost",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "EachTicketDecreaseMuteTime",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "EachTicketIncreaseMuteTime",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "EnableMuteReborn",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "MaxIncreaseMuteTime",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "IsRegex",
                table: "Expressions");

            migrationBuilder.DropColumn(
                name: "OwnerOnly",
                table: "Expressions");

            migrationBuilder.DropColumn(
                name: "UseCount",
                table: "Expressions");
        }
    }
}
