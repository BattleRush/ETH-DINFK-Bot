using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ETHBot.DataLayer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSetting",
                columns: table => new
                {
                    BotSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SpaceXSubredditCheckCronJob = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    LastSpaceXRedditPost = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    PlaceLocked = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSetting", x => x.BotSettingId);
                });

            migrationBuilder.CreateTable(
                name: "BotStartUpTimes",
                columns: table => new
                {
                    BotStartUpTimeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StartUpTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotStartUpTimes", x => x.BotStartUpTimeId);
                });

            migrationBuilder.CreateTable(
                name: "CommandTypes",
                columns: table => new
                {
                    CommandTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTypes", x => x.CommandTypeId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordEmotes",
                columns: table => new
                {
                    DiscordEmoteId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EmoteName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    Animated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Url = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    LocalPath = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    Blocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmotes", x => x.DiscordEmoteId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    DiscordServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ServerName = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.DiscordServerId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordUsers",
                columns: table => new
                {
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscriminatorValue = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    IsBot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsWebhook = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Username = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    AvatarUrl = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Nickname = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    FirstDailyPostCount = table.Column<int>(type: "int", nullable: false),
                    AllowedPlaceMultipixel = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUsers", x => x.DiscordUserId);
                });

            migrationBuilder.CreateTable(
                name: "PlaceBoardPixels",
                columns: table => new
                {
                    XPos = table.Column<short>(type: "smallint", nullable: false),
                    YPos = table.Column<short>(type: "smallint", nullable: false),
                    R = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    G = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    B = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceBoardPixels", x => new { x.XPos, x.YPos });
                });

            migrationBuilder.CreateTable(
                name: "PlacePerformanceInfos",
                columns: table => new
                {
                    PlacePerformanceHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    AvgTimeInMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacePerformanceInfos", x => x.PlacePerformanceHistoryId);
                });

            migrationBuilder.CreateTable(
                name: "RantTypes",
                columns: table => new
                {
                    RantTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RantTypes", x => x.RantTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SubredditInfos",
                columns: table => new
                {
                    SubredditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubredditName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    SubredditDescription = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    IsManuallyBanned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsNSFW = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NewestPost = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    NewestPostDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OldestPost = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    OldestPostDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsScraping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReachedOldest = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubredditInfos", x => x.SubredditId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordEmoteStatistics",
                columns: table => new
                {
                    DiscordEmoteId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    UsedAsReaction = table.Column<int>(type: "int", nullable: false),
                    UsedInText = table.Column<int>(type: "int", nullable: false),
                    UsedInTextOnce = table.Column<int>(type: "int", nullable: false),
                    UsedByBots = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmoteStatistics", x => x.DiscordEmoteId);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteStatistics_DiscordEmotes_DiscordEmoteId",
                        column: x => x.DiscordEmoteId,
                        principalTable: "DiscordEmotes",
                        principalColumn: "DiscordEmoteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordChannels",
                columns: table => new
                {
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ChannelName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    DiscordServerId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
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
                name: "DiscordRoles",
                columns: table => new
                {
                    DiscordRoleId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ColorHex = table.Column<string>(type: "varchar(10) CHARACTER SET utf8mb4", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsHoisted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsManaged = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsMentionable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    DiscordServerId = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordRoles", x => x.DiscordRoleId);
                    table.ForeignKey(
                        name: "FK_DiscordRoles_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "DiscordServerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BannedLinks",
                columns: table => new
                {
                    BannedLinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Link = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    ReportTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    AddedByDiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedLinks", x => x.BannedLinkId);
                    table.ForeignKey(
                        name: "FK_BannedLinks_DiscordUsers_AddedByDiscordUserId",
                        column: x => x.AddedByDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandStatistics",
                columns: table => new
                {
                    CommandStatisticId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommandTypeId = table.Column<int>(type: "int", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
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
                    PingInfoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PingCount = table.Column<int>(type: "int", nullable: false),
                    PingCountOnce = table.Column<int>(type: "int", nullable: false),
                    PingCountBot = table.Column<int>(type: "int", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
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
                name: "PlaceDiscordUsers",
                columns: table => new
                {
                    PlaceDiscordUserId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TotalPixelsPlaced = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceDiscordUsers", x => x.PlaceDiscordUserId);
                    table.ForeignKey(
                        name: "FK_PlaceDiscordUsers_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedditPosts",
                columns: table => new
                {
                    RedditPostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubredditInfoId = table.Column<int>(type: "int", nullable: false),
                    PostTitle = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    PostId = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    IsNSFW = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Author = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    UpvoteCount = table.Column<int>(type: "int", nullable: false),
                    DownvoteCount = table.Column<int>(type: "int", nullable: false),
                    Permalink = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    Url = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    IsText = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Content = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true)
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
                    BotChannelSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChannelPermissionFlags = table.Column<int>(type: "int", nullable: false),
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    OldestPostTimePreloaded = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    NewestPostTimePreloaded = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ReachedOldestPreload = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Content = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true),
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ReplyMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Preloaded = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordMessages", x => x.DiscordMessageId);
                    table.ForeignKey(
                        name: "FK_DiscordMessages_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordMessages_DiscordMessages_ReplyMessageId",
                        column: x => x.ReplyMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscordMessages_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceBoardHistory",
                columns: table => new
                {
                    PlaceBoardHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    XPos = table.Column<short>(type: "smallint", nullable: false),
                    YPos = table.Column<short>(type: "smallint", nullable: false),
                    R = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    G = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    B = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    PlaceDiscordUserId = table.Column<short>(type: "smallint", nullable: false),
                    PlacedDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Removed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceBoardHistory", x => x.PlaceBoardHistoryId);
                    table.ForeignKey(
                        name: "FK_PlaceBoardHistory_PlaceBoardPixels_XPos_YPos",
                        columns: x => new { x.XPos, x.YPos },
                        principalTable: "PlaceBoardPixels",
                        principalColumns: new[] { "XPos", "YPos" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaceBoardHistory_PlaceDiscordUsers_PlaceDiscordUserId",
                        column: x => x.PlaceDiscordUserId,
                        principalTable: "PlaceDiscordUsers",
                        principalColumn: "PlaceDiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedditImages",
                columns: table => new
                {
                    RedditImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RedditPostId = table.Column<int>(type: "int", nullable: false),
                    Link = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    LocalPath = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    Downloaded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsNSFW = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBlockedManually = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                name: "DiscordEmoteHistory",
                columns: table => new
                {
                    DiscordEmoteHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsReaction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    DateTimePosted = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DiscordEmoteId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordEmoteHistory", x => x.DiscordEmoteHistoryId);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordEmotes_DiscordEmoteId",
                        column: x => x.DiscordEmoteId,
                        principalTable: "DiscordEmotes",
                        principalColumn: "DiscordEmoteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscordEmoteHistory_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PingHistory",
                columns: table => new
                {
                    PingHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordRoleId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    FromDiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingHistory", x => x.PingHistoryId);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordRoles_DiscordRoleId",
                        column: x => x.DiscordRoleId,
                        principalTable: "DiscordRoles",
                        principalColumn: "DiscordRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PingHistory_DiscordUsers_FromDiscordUserId",
                        column: x => x.FromDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RantMessages",
                columns: table => new
                {
                    RantMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RantTypeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true),
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RantMessages", x => x.RantMessageId);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "DiscordChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RantMessages_RantTypes_RantTypeId",
                        column: x => x.RantTypeId,
                        principalTable: "RantTypes",
                        principalColumn: "RantTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedMessages",
                columns: table => new
                {
                    SavedMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordMessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DirectLink = table.Column<string>(type: "varchar(128) CHARACTER SET utf8mb4", maxLength: 128, nullable: true),
                    Content = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true),
                    SendInDM = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SavedByDiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ByDiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMessages", x => x.SavedMessageId);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordMessages_DiscordMessageId",
                        column: x => x.DiscordMessageId,
                        principalTable: "DiscordMessages",
                        principalColumn: "DiscordMessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordUsers_ByDiscordUserId",
                        column: x => x.ByDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedMessages_DiscordUsers_SavedByDiscordUserId",
                        column: x => x.SavedByDiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "DiscordUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannedLinks_AddedByDiscordUserId",
                table: "BannedLinks",
                column: "AddedByDiscordUserId");

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
                name: "IX_DiscordEmoteHistory_DiscordEmoteId",
                table: "DiscordEmoteHistory",
                column: "DiscordEmoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmoteHistory_DiscordMessageId",
                table: "DiscordEmoteHistory",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordEmoteHistory_DiscordUserId",
                table: "DiscordEmoteHistory",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_DiscordChannelId",
                table: "DiscordMessages",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_DiscordUserId",
                table: "DiscordMessages",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordMessages_ReplyMessageId",
                table: "DiscordMessages",
                column: "ReplyMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordRoles_DiscordServerId",
                table: "DiscordRoles",
                column: "DiscordServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_DiscordMessageId",
                table: "PingHistory",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_DiscordRoleId",
                table: "PingHistory",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_DiscordUserId",
                table: "PingHistory",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PingHistory_FromDiscordUserId",
                table: "PingHistory",
                column: "FromDiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PingStatistics_DiscordUserId",
                table: "PingStatistics",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceBoardHistory_PlaceDiscordUserId",
                table: "PlaceBoardHistory",
                column: "PlaceDiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceBoardHistory_XPos_YPos",
                table: "PlaceBoardHistory",
                columns: new[] { "XPos", "YPos" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceDiscordUsers_DiscordUserId",
                table: "PlaceDiscordUsers",
                column: "DiscordUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordChannelId",
                table: "RantMessages",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordMessageId",
                table: "RantMessages",
                column: "DiscordMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_DiscordUserId",
                table: "RantMessages",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RantMessages_RantTypeId",
                table: "RantMessages",
                column: "RantTypeId");

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
                name: "IX_SavedMessages_DiscordMessageId",
                table: "SavedMessages",
                column: "DiscordMessageId");

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
                name: "BotSetting");

            migrationBuilder.DropTable(
                name: "BotStartUpTimes");

            migrationBuilder.DropTable(
                name: "CommandStatistics");

            migrationBuilder.DropTable(
                name: "DiscordEmoteHistory");

            migrationBuilder.DropTable(
                name: "DiscordEmoteStatistics");

            migrationBuilder.DropTable(
                name: "PingHistory");

            migrationBuilder.DropTable(
                name: "PingStatistics");

            migrationBuilder.DropTable(
                name: "PlaceBoardHistory");

            migrationBuilder.DropTable(
                name: "PlacePerformanceInfos");

            migrationBuilder.DropTable(
                name: "RantMessages");

            migrationBuilder.DropTable(
                name: "RedditImages");

            migrationBuilder.DropTable(
                name: "SavedMessages");

            migrationBuilder.DropTable(
                name: "CommandTypes");

            migrationBuilder.DropTable(
                name: "DiscordEmotes");

            migrationBuilder.DropTable(
                name: "DiscordRoles");

            migrationBuilder.DropTable(
                name: "PlaceBoardPixels");

            migrationBuilder.DropTable(
                name: "PlaceDiscordUsers");

            migrationBuilder.DropTable(
                name: "RantTypes");

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
