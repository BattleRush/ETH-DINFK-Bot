using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandTypes",
                columns: table => new
                {
                    CommandTypeId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTypes", x => x.CommandTypeId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    DiscordServerId = table.Column<ulong>(nullable: false),
                    ServerName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.DiscordServerId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordUsers",
                columns: table => new
                {
                    DiscordUserId = table.Column<ulong>(nullable: false),
                    DiscriminatorValue = table.Column<ushort>(nullable: false),
                    IsBot = table.Column<bool>(nullable: false),
                    IsWebhook = table.Column<bool>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    AvatarUrl = table.Column<string>(nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(nullable: true),
                    Nickname = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUsers", x => x.DiscordUserId);
                });

            migrationBuilder.CreateTable(
                name: "EmojiStatistics",
                columns: table => new
                {
                    EmojiInfoId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmojiName = table.Column<string>(nullable: true),
                    EmojiId = table.Column<ulong>(nullable: false),
                    FallbackEmojiId = table.Column<ulong>(nullable: false),
                    Animated = table.Column<bool>(nullable: false),
                    UsedAsReaction = table.Column<int>(nullable: false),
                    UsedInText = table.Column<int>(nullable: false),
                    UsedInTextOnce = table.Column<int>(nullable: false),
                    UsedByBots = table.Column<int>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmojiStatistics", x => x.EmojiInfoId);
                });

            migrationBuilder.CreateTable(
                name: "SubredditInfos",
                columns: table => new
                {
                    SubredditId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubredditName = table.Column<string>(nullable: true),
                    SubredditDescription = table.Column<string>(nullable: true),
                    IsManuallyBanned = table.Column<bool>(nullable: false),
                    IsNSFW = table.Column<bool>(nullable: false),
                    NewestPost = table.Column<string>(nullable: true),
                    NewestPostDate = table.Column<DateTime>(nullable: false),
                    OldestPost = table.Column<string>(nullable: true),
                    OldestPostDate = table.Column<DateTime>(nullable: false),
                    IsScraping = table.Column<bool>(nullable: false),
                    ReachedOldest = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubredditInfos", x => x.SubredditId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordChannels",
                columns: table => new
                {
                    DiscordChannelId = table.Column<ulong>(nullable: false),
                    ChannelName = table.Column<string>(nullable: true),
                    DiscordServerId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordChannels", x => x.DiscordChannelId);
                    table.ForeignKey(
                        name: "FK_DiscordChannels_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "DiscordServerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BannedLinks",
                columns: table => new
                {
                    BannedLinkId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Link = table.Column<string>(nullable: true),
                    ReportTime = table.Column<DateTimeOffset>(nullable: false),
                    ByUserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedLinks", x => x.BannedLinkId);
                    table.ForeignKey(
                        name: "FK_BannedLinks_DiscordUsers_ByUserId",
                        column: x => x.ByUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandStatistics",
                columns: table => new
                {
                    CommandStatisticId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommandTypeId = table.Column<int>(nullable: false),
                    DiscordUserId = table.Column<ulong>(nullable: false),
                    Count = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandStatistics", x => x.CommandStatisticId);
                    table.ForeignKey(
                        name: "FK_CommandStatistics_CommandTypes_CommandTypeId",
                        column: x => x.CommandTypeId,
                        principalTable: "CommandTypes",
                        principalColumn: "CommandTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommandStatistics_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PingStatistics",
                columns: table => new
                {
                    PingInfoId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PingCount = table.Column<int>(nullable: false),
                    PingCountOnce = table.Column<int>(nullable: false),
                    PingCountBot = table.Column<int>(nullable: false),
                    DiscordUserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingStatistics", x => x.PingInfoId);
                    table.ForeignKey(
                        name: "FK_PingStatistics_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmojiHistory",
                columns: table => new
                {
                    EmojiHistoryId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsReaction = table.Column<bool>(nullable: false),
                    IsBot = table.Column<bool>(nullable: false),
                    Count = table.Column<int>(nullable: false),
                    DateTimePosted = table.Column<DateTime>(nullable: false),
                    EmojiStatisticId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmojiHistory", x => x.EmojiHistoryId);
                    table.ForeignKey(
                        name: "FK_EmojiHistory_EmojiStatistics_EmojiStatisticId",
                        column: x => x.EmojiStatisticId,
                        principalTable: "EmojiStatistics",
                        principalColumn: "EmojiInfoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedditPosts",
                columns: table => new
                {
                    RedditPostId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PostTitle = table.Column<string>(nullable: true),
                    PostId = table.Column<string>(nullable: true),
                    IsNSFW = table.Column<bool>(nullable: false),
                    PostedAt = table.Column<DateTime>(nullable: false),
                    Author = table.Column<string>(nullable: true),
                    UpvoteCount = table.Column<int>(nullable: false),
                    DownvoteCount = table.Column<int>(nullable: false),
                    Permalink = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    SubredditInfoId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedditPosts", x => x.RedditPostId);
                    table.ForeignKey(
                        name: "FK_RedditPosts_SubredditInfos_SubredditInfoId",
                        column: x => x.SubredditInfoId,
                        principalTable: "SubredditInfos",
                        principalColumn: "SubredditId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotChannelSettings",
                columns: table => new
                {
                    BotChannelSettingId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelPermissionFlags = table.Column<int>(nullable: false),
                    DiscordChannelId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotChannelSettings", x => x.BotChannelSettingId);
                    table.ForeignKey(
                        name: "FK_BotChannelSettings_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordMessages",
                columns: table => new
                {
                    MessageId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(nullable: true),
                    DiscordChannelId = table.Column<ulong>(nullable: false),
                    DiscordUserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_DiscordMessages_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordMessages_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedditImages",
                columns: table => new
                {
                    RedditImageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Link = table.Column<string>(nullable: true),
                    LocalPath = table.Column<string>(nullable: true),
                    Downloaded = table.Column<bool>(nullable: false),
                    IsNSFW = table.Column<bool>(nullable: false),
                    IsBlockedManually = table.Column<bool>(nullable: false),
                    RedditPostId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedditImages", x => x.RedditImageId);
                    table.ForeignKey(
                        name: "FK_RedditImages_RedditPosts_RedditPostId",
                        column: x => x.RedditPostId,
                        principalTable: "RedditPosts",
                        principalColumn: "RedditPostId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedMessages",
                columns: table => new
                {
                    SavedMessageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<ulong>(nullable: false),
                    DirectLink = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true),
                    SendInDM = table.Column<bool>(nullable: false),
                    SavedByDiscordUserId = table.Column<ulong>(nullable: false),
                    ByDiscordUserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMessages", x => x.SavedMessageId);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordUsers_ByDiscordUserId",
                        column: x => x.ByDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordUsers_SavedByDiscordUserId",
                        column: x => x.SavedByDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannedLinks_ByUserId",
                table: "BannedLinks",
                column: "ByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BotChannelSettings_DiscordChannelId",
                table: "BotChannelSettings",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandStatistics_CommandTypeId",
                table: "CommandStatistics",
                column: "CommandTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandStatistics_DiscordUserId",
                table: "CommandStatistics",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannels_DiscordServerId",
                table: "DiscordChannels",
                column: "DiscordServerId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_DiscordChannelId",
                table: "DiscordMessages",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_DiscordUserId",
                table: "DiscordMessages",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmojiHistory_EmojiStatisticId",
                table: "EmojiHistory",
                column: "EmojiStatisticId");

            migrationBuilder.CreateIndex(
                name: "IX_PingStatistics_DiscordUserId",
                table: "PingStatistics",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RedditImages_RedditPostId",
                table: "RedditImages",
                column: "RedditPostId");

            migrationBuilder.CreateIndex(
                name: "IX_RedditPosts_SubredditInfoId",
                table: "RedditPosts",
                column: "SubredditInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMessages_ByDiscordUserId",
                table: "SavedMessages",
                column: "ByDiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMessages_MessageId",
                table: "SavedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMessages_SavedByDiscordUserId",
                table: "SavedMessages",
                column: "SavedByDiscordUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedLinks");

            migrationBuilder.DropTable(
                name: "BotChannelSettings");

            migrationBuilder.DropTable(
                name: "CommandStatistics");

            migrationBuilder.DropTable(
                name: "EmojiHistory");

            migrationBuilder.DropTable(
                name: "PingStatistics");

            migrationBuilder.DropTable(
                name: "RedditImages");

            migrationBuilder.DropTable(
                name: "SavedMessages");

            migrationBuilder.DropTable(
                name: "CommandTypes");

            migrationBuilder.DropTable(
                name: "EmojiStatistics");

            migrationBuilder.DropTable(
                name: "RedditPosts");

            migrationBuilder.DropTable(
                name: "DiscordMessages");

            migrationBuilder.DropTable(
                name: "SubredditInfos");

            migrationBuilder.DropTable(
                name: "DiscordChannels");

            migrationBuilder.DropTable(
                name: "DiscordUsers");

            migrationBuilder.DropTable(
                name: "DiscordServers");
        }
    }
}
