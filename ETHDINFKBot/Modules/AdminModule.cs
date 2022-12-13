using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Classes;
using ETHDINFKBot.CronJobs;
using ETHDINFKBot.CronJobs.Jobs;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
//using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using ETHDINFKBot.Log;
using ETHDINFKBot.Struct;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Web;
using ETHBot.DataLayer.Data.Discord;
using MySqlConnector;
using ETHBot.DataLayer.Data.ETH.Food;

namespace ETHDINFKBot.Modules
{

    public class Class1
    {
        public ulong id { get; set; }
        public string nick { get; set; }
        public string top_role_name { get; set; }
        public ulong top_role_id { get; set; }
    }


    public class EmoteInfo
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public bool Animated { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Url { get; set; }
        public string Folder { get; set; }

    }

    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("image")]
        public async Task IMgaeTest()
        {
            try
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                GoogleEngine google = new GoogleEngine();
                var results = await google.GetSearchResultBySelenium("ETH Zürich", 0, "de");
                if (results != null && results.Count > 0)
                    await Context.Channel.SendMessageAsync(string.Join(Environment.NewLine, results));
                else
                    await Context.Channel.SendMessageAsync("No results found", false);

            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        [Command("renameback")]
        public async Task Test()
        {
            return; // disable again
            //var author = Context.Message.Author;
            //if (author.Id != Program.ApplicationSetting.Owner)
            //{
            //    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
            //    return;
            //}
            //try
            //{
            //    var allUsers = await Context.Guild.GetUsersAsync().FlattenAsync();
            //    Context.Channel.SendMessageAsync("users " + allUsers.Count().ToString(), false);

            //    Random r = new Random();

            //    var jsonString = File.ReadAllText("");

            //    var jsonUsers = JsonConvert.DeserializeObject<Class1[]>(jsonString).ToList();

            //    Context.Channel.SendMessageAsync("json " + jsonUsers.Count.ToString(), false);


            //    foreach (SocketGuildUser user in allUsers)
            //    {
            //        var targetUser = jsonUsers.SingleOrDefault(i => i.id == user.Id);

            //        if (targetUser == null || targetUser.nick == user.Nickname)
            //            continue;

            //        try
            //        {
            //            await user.ModifyAsync(i =>
            //            {
            //                i.Nickname = targetUser.nick;
            //            });

            //            await Context.Channel.SendMessageAsync("Fixing " + user.Username, false);

            //        }
            //        catch (Exception ex)
            //        {
            //            await Context.Channel.SendMessageAsync(ex.Message + " on " + user.Username, false);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    await Context.Channel.SendMessageAsync(ex.Message, false);
            //}
        }

        //[RequireOwner]
        [Command("help")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AdminHelp()
        {
            //var author = Context.Message.Author;
            //if (author.Id != Program.ApplicationSetting.Owner)
            //{
            //    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
            //    return;
            //}

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Admin Help (Admin only)");

            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

            builder.WithCurrentTimestamp();
            builder.AddField("admin help", "This message :)");
            builder.AddField("admin channel help", "Help for channel command");
            builder.AddField("admin reddit help", "Help for reddit command");
            builder.AddField("admin rant help", "Help for rant command");
            builder.AddField("admin place help", "Help for place command");
            builder.AddField("admin keyval help", "Help for KeyValue DB Management");
            builder.AddField("admin kill", "Do I really need to explain this one");
            builder.AddField("admin cronjob <name>", "Manually start a CronJob");
            builder.AddField("admin blockemote <id> <block>", "Block an emote from being selectable");
            builder.AddField("admin events", "Sync VIS Events");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("kill")]
        public async Task AdminKill()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            await Context.Channel.SendMessageAsync("I'll be back!", false);
            Process.GetCurrentProcess().Kill();
        }


        [Command("reboot")]
        public async Task AdminReboot()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            await Context.Channel.SendMessageAsync("Rebooting...", false);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "sudo shutdown -r now", };
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message, false);
            }

        }

        private Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return
              assembly.GetTypes()
                      .Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                      .ToArray();
        }

        [Command("events")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SyncVisEvents()
        {
            // TODO Move constants to config
            await DiscordHelper.SyncVisEvents(
                (Context.Channel as SocketGuildChannel).Guild.Id, 
                747768907992924192, 
                819864331192631346);
        }

        [Command("cronjob")]
        public async Task ManualCronJob(string cronJobName)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            // TODO find a way to call them dynamically

            /*
            string baseNamespaceCronJobs = "ETHDINFKBot.CronJobs.Jobs";
            Type[] typelist = GetTypesInNamespace(Assembly.GetExecutingAssembly(), baseNamespaceCronJobs);

            Type type = Type.GetType(baseNamespaceCronJobs + "." + "DailyCleanup"); //target type

            MethodInfo info = type.GetMethod("StartAsync");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            try
            {
                if (info.IsStatic)
                    info.Invoke(null, new object[] { token });
                else
                    info.Invoke(type, new object[] { token });
            }
            catch (Exception ex)
            {

            }*/
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                switch (cronJobName)
                {
                    case "DailyCleanup":
                        var config = new ScheduleConfig<DailyCleanup>();
                        config.TimeZoneInfo = TimeZoneInfo.Local;
                        config.CronExpression = "* * * * *";

                        var logger = Program.Logger.CreateLogger<DailyCleanup>();

                        var job = new DailyCleanup(config, logger);
                        await job.StartAsync(token);
                        break;

                    /* TODO Add more jobs if needed*/
                    default:
                        await Context.Channel.SendMessageAsync("Only available: DailyCleanup", false);
                        break;
                }

                cts.CancelAfter(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("Error: " + ex.ToString(), false);
            }
        }

        [Command("blockemote")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BlockEmote(ulong emoteId, bool blockStatus)
        {
            var author = Context.Message.Author;
            //if (author.Id != Program.ApplicationSetting.Owner)
            //{
            //await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
            //return;
            //}

            var emoteInfo = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);
            bool success = DatabaseManager.EmoteDatabaseManager.SetEmoteBlockStatus(emoteId, blockStatus);

            if (success)
            {
                // Also locally delete the file
                if (File.Exists(emoteInfo.LocalPath))
                    File.Delete(emoteInfo.LocalPath); // TODO Redownload if the emote is unblocked

                await Context.Channel.SendMessageAsync($"Successfully set block status of emote {emoteId} to: {blockStatus}", false);
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Failed to set block status of emote {emoteId}", false);
            }
        }

        class DiscordUserDump
        {
            public ulong DiscordUserId { get; set; }
            public string DiscordUserName { get; set; }
            public string AvatarUrl { get; set; }
            public bool IsBot { get; set; }
        }

        [Command("userupdate")]
        public async Task UserUpdate()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            var allUsers = DatabaseManager.Instance().GetDiscordUsers();

            foreach (var user in allUsers)
            {
                try
                {
                    // Only users without pfp will be force updates
                    if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                        continue;

                    var discordUser = Program.Client.GetUser(user.DiscordUserId);

                    DatabaseManager.Instance().UpdateDiscordUser(new DiscordUser()
                    {
                        DiscordUserId = user.DiscordUserId,
                        DiscriminatorValue = user.DiscriminatorValue,
                        AvatarUrl = discordUser.GetAvatarUrl() ?? discordUser.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                        IsBot = user.IsBot,
                        IsWebhook = user.IsWebhook,
                        Nickname = user.Nickname,
                        Username = user.Username,
                        JoinedAt = user.JoinedAt,
                        FirstAfternoonPostCount = user.FirstAfternoonPostCount
                    });

                    await Context.Message.Channel.SendMessageAsync($"Updated {user.Nickname ?? user.Username}");
                }
                catch (Exception ex)
                {
                    // Ignore
                }
            }

            await Context.Message.Channel.SendMessageAsync($"Done");

        }


        [Command("userdump")]
        public async Task UserDump()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }


            var allUsers = DatabaseManager.Instance().GetDiscordUsers();

            List<DiscordUserDump> discordUsersList = new List<DiscordUserDump>();

            foreach (var user in allUsers)
            {
                if (string.IsNullOrWhiteSpace(user.AvatarUrl))
                    continue;

                discordUsersList.Add(new DiscordUserDump()
                {
                    DiscordUserId = user.DiscordUserId,
                    DiscordUserName = user.Nickname ?? user.Username,
                    AvatarUrl = user.AvatarUrl,
                    IsBot = user.IsBot
                });
            }

            var json = JsonConvert.SerializeObject(discordUsersList, Formatting.Indented);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Context.Channel.SendFileAsync(stream, "DiscordUsersList.json", "DiscordUsers");
        }

        [Command("emotedump")]
        public async Task EmoteDump()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            var allEmotes = DatabaseManager.EmoteDatabaseManager.GetEmotes().OrderBy(i => i.DiscordEmoteId).ToList(); // sort it to ensure they are chronologically in there

            await Context.Channel.SendMessageAsync($"Successfully retrieved {allEmotes.Count} emotes", false);

            var emotesPath = Path.Combine(Program.ApplicationSetting.BasePath, "Emotes");
            var archivePath = Path.Combine(emotesPath, "Archive");

            try
            {

                // If the directory exists clean it up
                if (Directory.Exists(archivePath))
                    Directory.Delete(archivePath, true);

                // Create dir
                Directory.CreateDirectory(archivePath);

                List<EmoteInfo> emoteInfos = new List<EmoteInfo>();

                foreach (var emote in allEmotes)
                {
                    var folder = GetEmoteFolder(emote.LocalPath);
                    emoteInfos.Add(new EmoteInfo()
                    {
                        Id = emote.DiscordEmoteId,
                        Name = emote.EmoteName,
                        Animated = emote.Animated,
                        CreatedAt = emote.CreatedAt,
                        Url = emote.Url,
                        Folder = folder
                    });
                }

                //var emoteFolders = Directory.GetDirectories(emotesPath);

                //foreach (var emoteFolder in emoteFolders.ToList().OrderBy(i => i))
                //{
                //// Needs to contain - else its not an active folder
                //if (emoteFolder.Contains("-"))
                //{
                //string tarGZFile = $"{new DirectoryInfo(emoteFolder).Name}.tar.gz";
                //CreateTarGZ(Path.Combine(archivePath, tarGZFile), emoteFolder);
                //}
                //}

                //var archiveFiles = Directory.GetFiles(archivePath);
                //await Context.Channel.SendMessageAsync($"Created {archiveFiles.Length} archives", false);

                // Send file infos

                var json = JsonConvert.SerializeObject(emoteInfos, Formatting.Indented);
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await Context.Channel.SendFileAsync(stream, "EmoteInfo.json", "Emote Infos");

                //foreach (var archiveFile in archiveFiles.ToList().OrderBy(i => i))
                //    await Context.Channel.SendFileAsync(archiveFile, new DirectoryInfo(archiveFile).Name);

                // In the end clean up the archive folder again
                //if (Directory.Exists(archivePath))
                //    Directory.Delete(archivePath, true);

                await Context.Channel.SendMessageAsync($"Done", false);

            }
            catch (Exception ex)
            {
                string error = $"Error: {ex.ToString()}";
                await Context.Channel.SendMessageAsync(error.Substring(0, Math.Min(2000, error.Length)), false);
            }
        }

        private string GetEmoteFolder(string path)
        {
            return new DirectoryInfo(path).Parent.Name;
        }

        //  https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#user-content--create-a-tgz-targz
        private void CreateTarGZ(string tgzFilename, string sourceDirectory)
        {
            Stream outStream = File.Create(tgzFilename);
            Stream gzoStream = new GZipOutputStream(outStream);
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
            if (tarArchive.RootPath.EndsWith("/"))
                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

            AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);

            tarArchive.Close();
        }

        private void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {
            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }

        [Group("food")]
        public class FoodAdminModule : ModuleBase<SocketCommandContext>
        {
            private static FoodDBManager FoodDBManager = FoodDBManager.Instance();

            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField($"{Program.CurrentPrefix}admin food help", "This message :)");
                builder.AddField($"{Program.CurrentPrefix}admin food setup", "Sets Default values for Tables Restaurant and Allergies");
                builder.AddField($"{Program.CurrentPrefix}admin food clear <id>", "Clears today menus");
                builder.AddField($"{Program.CurrentPrefix}admin food load <id>", "Loads todays menus");
                builder.AddField($"{Program.CurrentPrefix}admin food image <restaurant|menu> <id> <full>", "Runs a websearch to replace images");
                //builder.AddField($"{Program.CurrentPrefix}admin food menuimage <menu_id>", "Returns all images found for this menu");
                builder.AddField($"{Program.CurrentPrefix}admin food status <debug>", "Returns current menus status");
                builder.AddField($"{Program.CurrentPrefix}admin food fix", "Fixes today menus");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            // TODO move this somewhere else or create insert script to check if all inserted
            [Command("setup")]
            public async Task SetupFood()
            {
                try
                {
                    var author = Context.Message.Author;
                    if (author.Id != Program.ApplicationSetting.Owner)
                    {
                        await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                        return;
                    }

                    var foodDBManager = FoodDBManager.Instance();

                    var sqlFilePath = Path.Combine(Program.ApplicationSetting.BasePath, "Data", "SQL", "RestaurantBaseSetup.sql");
                    string sqlFileContent = File.ReadAllText(sqlFilePath);

                    using (var connection = new MySqlConnection(Program.ApplicationSetting.ConnectionStringsSetting.ConnectionString_Full))
                    {
                        using (var command = new MySqlCommand(sqlFileContent, connection))
                        {
                            try
                            {
                                command.CommandTimeout = 5;

                                connection.Open();

                                int rowsAffected = command.ExecuteNonQuery();
                                await Context.Channel.SendMessageAsync($"Affected {rowsAffected} row(s)", false);
                            }
                            catch (Exception ex)
                            {
                                await Context.Message.Channel.SendMessageAsync(ex.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Context.Message.Channel.SendMessageAsync(ex.ToString());
                }
            }

            [Command("clear")]
            public async Task ClearFood(int restaurantId = -1)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                //var allRestaurants = FoodDBManager.GetAllRestaurants();

                var allMenus = FoodDBManager.GetMenusByDay(DateTime.Now, restaurantId);
                if (allMenus.Count > 0)
                    await Context.Channel.SendMessageAsync($"Deleting {allMenus.Count} menu(s)", false);

                foreach (var menu in allMenus)
                    FoodDBManager.DeleteMenu(menu);

                await Context.Channel.SendMessageAsync($"Done clear for: {restaurantId}", false);
            }

            // TODO allow load mode for all restaurants with no menus today
            [Command("fix", RunMode = RunMode.Async)]
            public async Task FixFood()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var allRestaurants = FoodDBManager.GetAllRestaurants();
                var foodHelper = new FoodHelper();

                foreach (var restaurant in allRestaurants)
                {
                    if (!restaurant.IsOpen)
                        continue;

                    var allMenus = FoodDBManager.GetMenusByDay(DateTime.Now, restaurant.RestaurantId);
                    if (allMenus.Count == 0)
                    {
                        await ClearFood(restaurant.RestaurantId); // Ensure but likely empty anyway
                        foodHelper.LoadMenus(restaurant.RestaurantId);

                        await Context.Channel.SendMessageAsync($"Done load for: {restaurant.RestaurantId}", false);
                    }
                }
            }

            // TODO allow load mode for all restaurants with no menus today
            [Command("load", RunMode = RunMode.Async)]
            public async Task LoadFood(int restaurantId = -1)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var foodHelper = new FoodHelper();
                await ClearFood(restaurantId); // Ensure deleted
                foodHelper.LoadMenus(restaurantId);

                await Context.Channel.SendMessageAsync($"Done load for: {restaurantId}", false);
            }

            [Command("image", RunMode = RunMode.Async)]
            public async Task LoadImage(string key, int id, bool full = true)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                List<Menu> menus = new List<Menu>();

                if (key == "restaurant")
                {
                    await Context.Channel.SendMessageAsync($"Loading images for RestaurantId: {id}", false);
                    menus = FoodDBManager.GetMenusByDay(DateTime.Now, id);
                }
                else if (key == "menu")
                {
                    await Context.Channel.SendMessageAsync($"Loading images for MenuId: {id}", false);
                    var menu = FoodDBManager.GetMenusById(id);
                    menus.Add(menu);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                }

                var foodHelper = new FoodHelper();
                try
                {
                    foreach (var menu in menus)
                    {
                        if (menu.MenuImageId.HasValue)
                            continue; // We dont need to research this image

                        var menuImage = foodHelper.GetImageForFood(menu, true);
                        await Context.Channel.SendMessageAsync($"Got ImageId: {menuImage?.MenuImageId ?? -1} for Menu: {menu.MenuId}", false);

                        if (menuImage != null)
                            FoodDBManager.SetImageIdForMenu(menu.MenuId, menuImage.MenuImageId);
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }

                //await ClearFood(restaurantId); // Ensure deleted
                //FoodHelper.LoadMenus(restaurantId);

                await Context.Channel.SendMessageAsync("Done load", false);
            }

            [Command("status", RunMode = RunMode.Async)]
            public async Task StatusFood(bool debug = true)
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Food status");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.WithAuthor(author);

                var allRestaurants = FoodDBManager.GetAllRestaurants();
                var allTodaysMenus = FoodDBManager.GetMenusByDay(DateTime.Now);

                builder.WithDescription(@$"Total restaurants: {allRestaurants.Count}
Total todays menus: {allTodaysMenus.Count}");

                foreach (var restaurant in allRestaurants)
                {
                    var todaysMenus = FoodDBManager.GetMenusFromRestaurant(restaurant.RestaurantId, DateTime.Now);

                    if (!debug)
                        builder.AddField(restaurant.Name, $"{todaysMenus.Count()} menu(s)" + Environment.NewLine + String.Join(", ", todaysMenus.Select(i => $"{i.Name} **{i.Amount} CHF**")));
                    else
                        builder.AddField($"{restaurant.Name} ({restaurant.RestaurantId})", $"{todaysMenus.Count()} menu(s)" + Environment.NewLine + String.Join(", ", todaysMenus.Select(i => $"{i.Name} **{i.Amount} CHF ({i.MenuId}/{i.MenuImageId ?? -1})**")));
                }

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }


        [Group("rant")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin rant help", "This message :)");
                builder.AddField("admin rant all", "List all types");
                builder.AddField("admin rant add <type>", "Add new type (open for all)");
                builder.AddField("admin rant dt <type id>", "Delete type");
                builder.AddField("admin rant dr <rant id>", "Delete rant");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("all")]
            public async Task AdminAllRantTypes()
            {
                // todo a bit of a duplicate from DiscordModule
                var typeList = DatabaseManager.Instance().GetAllRantTypes();
                string allTypes = "```" + string.Join(", ", typeList) + "```";

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("All Rant types");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("Types [Id, Name]", allTypes);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("add")]
            public async Task AddRantType(string type)
            {
                /*var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }*/

                bool success = DatabaseManager.Instance().AddRantType(type);
                await Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);
            }

            [Command("dt")]
            public async Task DeleteRantType(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantType(typeId);
                await Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }


            [Command("dr")]
            public async Task DeleteRantMessage(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantMessage(typeId);
                await Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }
        }


        [Group("channel")]
        public class ChannelAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task ChannelAdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin channel help", "This message :)");
                builder.AddField("admin channel info", "Returns info about the current channel settings and global channel order info");
                builder.AddField("admin channel lock <true|false>", "Locks the ordering of all channels and reverts any order changes when active");
                builder.AddField("admin channel lockinfo", "Returns positions for all channels (if the Position lock is active)");
                builder.AddField("admin channel preload <channelId> <amount>", "Loads old messages into the DB");
                builder.AddField("admin channel set <permission> <channelId>", "Set permissions for the current channel or specific channel");
                builder.AddField("admin channel all <permission>", "Set the MINIMUM permissions for ALL channels");
                builder.AddField("admin channel flags", "Returns help with the flag infos");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            /* public static IEnumerable<Enum> GetAllFlags(Enum e)
             {
                 return Enum.GetValues(e.GetType()).Cast<Enum>();
             }

             // TODO move to somewhere common
             static IEnumerable<Enum> GetFlags(Enum input)
             {
                 foreach (Enum value in Enum.GetValues(input.GetType()))
                     if (input.HasFlag(value))
                         yield return value;
             }*/


            [Command("lockinfo")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task GetLockInfo()
            {
                // TODO Category infos
                ulong guildId = Program.ApplicationSetting.BaseGuild;

#if DEBUG
                guildId = 774286694794919986;
#endif

                var guild = Program.Client.GetGuild(guildId);


                var channels = guild.Channels;
                var categories = guild.CategoryChannels;

                var sortedDict = from entry in Program.ChannelPositions orderby entry.Position ascending select entry;

                List<string> header = new List<string>()
                        {
                            "Discord / Cache Position",
                            "Category Name",
                            "Channel Name"
                        };


                List<List<string>> data = new List<List<string>>();

                List<TableRowInfo> tableRowInfos = new List<TableRowInfo>();

                foreach (var category in categories.OrderBy(i => i.Position))
                {
                    var categoryInfos = Program.ChannelPositions.Where(i => i.CategoryId == category.Id);

                    var categoryInfo = new List<string>() { category.Position + " / --", category.Name, "" };
                    data.Add(categoryInfo);

                    var cells = new List<TableCellInfo>() {
                        new TableCellInfo() { ColumnId = 0, FontColor = new SkiaSharp.SKColor(255, 125, 0) },
                        new TableCellInfo() { ColumnId = 1, FontColor = new SkiaSharp.SKColor(255, 125, 0) }
                    };

                    tableRowInfos.Add(new TableRowInfo()
                    {
                        RowId = data.Count - 1,
                        Cells = cells
                    });

                    // One category
                    foreach (var channelInfo in categoryInfos.OrderBy(i => i.Position))
                    {
                        var channel = channels.SingleOrDefault(i => i.Id == channelInfo.ChannelId);
                        if (channel == null)
                            continue;

                        var currentRecord = new List<string>() { channel.Position.ToString() + " / " + channelInfo.Position.ToString(), "", channelInfo.ChannelName.ToString() };
                        //currentRecord.Add(Regex.Replace(channel.Name, @"[^\u0000-\u007F]+", string.Empty)); // replace non asci cars
                        data.Add(currentRecord);
                    }
                }

                var drawTable = new DrawTable(header, data, "", tableRowInfos, 1000);

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                stream.Dispose();
            }

            [Command("lock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task LockChannelOrdering(bool lockChannels)
            {
                // allow for people that can manage channels to lock the ordering

                //var botSettings = DatabaseManager.Instance().GetBotSettings();
                //botSettings.ChannelOrderLocked = lockChannels;
                //botSettings = DatabaseManager.Instance().SetBotSettings(botSettings);

                var keyValueDBManager = DatabaseManager.KeyValueManager;

                var isLockEnabled = keyValueDBManager.Update<bool>("LockChannelPositions", lockChannels);

                await Context.Message.Channel.SendMessageAsync($"Set Global Position Lock to: {isLockEnabled}");

                if (isLockEnabled)
                {
                    // TODO Setting
                    ulong guildId = Program.ApplicationSetting.BaseGuild;

#if DEBUG
                    guildId = 774286694794919986;
#endif

                    var guild = Program.Client.GetGuild(guildId);

                    // list should always be empty
                    Program.ChannelPositions = new List<ChannelOrderInfo>();


                    // Any channels outside of categories considered?
                    foreach (var category in guild.CategoryChannels)
                        foreach (var channel in category.Channels)
                            Program.ChannelPositions.Add(new ChannelOrderInfo() { ChannelId = channel.Id, ChannelName = channel.Name, CategoryId = category.Id, CategoryName = category.Name, Position = channel.Position });


                    await Context.Message.Channel.SendMessageAsync($"Saved ordering for: {Program.ChannelPositions.Count}");
                }
                else
                {
                    // do nothing
                }
            }



            [Command("preload")]
            public async Task PreloadOldMessages(ulong channelId, int count = 1000)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                // new column preloaded
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                var dbManager = DatabaseManager.Instance();
                var channel = Program.Client.GetChannel(channelId) as ISocketMessageChannel;
                var oldestMessage = dbManager.GetOldestMessageAvailablePerChannel(channelId);

                if (oldestMessage == null)
                    return;

                //var messages = channel.GetMessagesAsync(100000).FlattenAsync(); //default is 100

                var messagesFromMsg = await channel.GetMessagesAsync(oldestMessage.Value, Direction.Before, count).FlattenAsync();

                LogManager logManager = new LogManager(dbManager);
                int success = 0;
                int tags = 0;
                int newUsers = 0;
                try
                {
                    foreach (var message in messagesFromMsg)
                    {
                        var dbUser = dbManager.GetDiscordUserById(message.Author.Id);

                        if (dbUser == null)
                        {
                            var user = message.Author;
                            var socketGuildUser = user as SocketGuildUser;


                            var dbUserNew = dbManager.CreateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                            {
                                DiscordUserId = user.Id,
                                DiscriminatorValue = user.DiscriminatorValue,
                                AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                                IsBot = user.IsBot,
                                IsWebhook = user.IsWebhook,
                                Nickname = socketGuildUser?.Nickname,
                                Username = user.Username,
                                JoinedAt = socketGuildUser?.JoinedAt
                            });

                            if (dbUserNew != null)
                                newUsers++;

                        }

                        var newMessage = dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                        {
                            //Channel = discordChannel,
                            DiscordChannelId = channelId,
                            //DiscordUser = dbAuthor,
                            DiscordUserId = message.Author.Id,
                            DiscordMessageId = message.Id,
                            Content = message.Content,
                            //ReplyMessageId = message.Reference.MessageId,
                            Preloaded = true
                        });


                        if (newMessage)
                        {
                            success++;
                        }
                        if (message.Reactions.Count > 0)
                        {
                            if (newMessage && message.Tags.Count > 0)
                            {
                                tags += message.Tags.Count;
                                await logManager.ProcessEmojisAndPings(message.Tags, message.Author.Id, message as SocketMessage, message.Author as SocketGuildUser);
                            }
                        }
                    }
                    watch.Stop();

                    await Context.Channel.SendMessageAsync($"Processed {messagesFromMsg.Count()} Added: {success} TagsCount: {tags} From: {SnowflakeUtils.FromSnowflake(messagesFromMsg.First()?.Id ?? 1)} To: {SnowflakeUtils.FromSnowflake(messagesFromMsg.Last()?.Id ?? 1)}" +
                        $" New Users: {newUsers} In: {watch.ElapsedMilliseconds}ms", false);
                }
                catch (Exception ex)
                {

                }
            }

            // TODO move to common
            private static string GetPermissionString(BotPermissionType flags)
            {
                List<string> permissionFlagNames = new List<string>();
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    var hasFlag = flags.HasFlag(flag);

                    if (hasFlag)
                        permissionFlagNames.Add($"{flag} ({(int)flag})");
                }

                return string.Join(", ", permissionFlagNames);
            }


            [Command("info")]

            public async Task GetChannelInfoAsync(bool all = false)
            {
                var guildUser = Context.Message.Author as SocketGuildUser;
                var author = Context.Message.Author;
                if (!(author.Id == Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!all)
                    {
                        var channelInfo = DatabaseManager.Instance().GetChannelSetting(guildChannel.Id);
                        //var botSettings = DatabaseManager.Instance().GetBotSettings();
                        var keyValueDBManager = DatabaseManager.KeyValueManager;

                        var isLockEnabled = keyValueDBManager.Get<bool>("LockChannelPositions");

                        if (channelInfo == null)
                        {
                            Context.Channel.SendMessageAsync("channelInfo is null bad admin", false);
                            return;
                        }

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle($"Channel Info for {guildChannel.Name}");
                        builder.WithDescription($"Global Channel position lock active: {isLockEnabled} for {(isLockEnabled ? Program.ChannelPositions.Count : -1)} channels");
                        builder.WithColor(255, 0, 0);
                        builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                        builder.WithCurrentTimestamp();

                        builder.AddField("Permission flag", channelInfo.ChannelPermissionFlags);

                        foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                        {
                            var hasFlag = ((BotPermissionType)channelInfo.ChannelPermissionFlags).HasFlag(flag);
                            builder.AddField(flag.ToString() + $" ({(int)flag})", $"```diff\r\n{(hasFlag ? "+ YES" : "- NO")}```", true);
                        }


                        await Context.Channel.SendMessageAsync("", false, builder.Build());

                    }
                    else
                    {
                        List<string> header = new List<string>()
                        {
                            "Category Name",
                            "Channel Name",
                            "Thread Name",
                            "Permission value",
                            "Permission string",
                            "Preload old",
                            "Preload new",
                            "Reached oldest"
                        };


                        List<List<string>> data = new List<List<string>>();

                        List<TableRowInfo> tableRowInfos = new List<TableRowInfo>();

                        var categories = Program.Client.GetGuild(guildChannel.Guild.Id).CategoryChannels.OrderBy(i => i.Position);


                        foreach (var category in categories)
                        {
                            var channelCategorySettingInfo = CommonHelper.GetChannelSettingByChannelId(category.Id, false);
                            var channelCategorySetting = channelCategorySettingInfo.Setting;

                            // New category
                            data.Add(new List<string>() {
                                category.Name,
                                "",
                                "",
                                channelCategorySetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                GetPermissionString((BotPermissionType)(channelCategorySetting?.ChannelPermissionFlags ?? 0)),
                                channelCategorySetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                channelCategorySetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                channelCategorySetting?.ReachedOldestPreload.ToString() ?? "N/A"
                            });

                            var cells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 0, FontColor = new SkiaSharp.SKColor(255, 100, 0) } };
                            if (channelCategorySettingInfo.Inherit || true /* For now categories cant Inherit -> but show visually*/)
                            {
                                cells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                            }

                            tableRowInfos.Add(new TableRowInfo()
                            {
                                RowId = data.Count - 1,
                                Cells = cells
                            });


                            // TODO Order
                            foreach (var channel in category.Channels)
                            {
                                var channelSettingInfo = CommonHelper.GetChannelSettingByChannelId(channel.Id, true);
                                var channelSetting = channelSettingInfo.Setting;

                                // New channel
                                data.Add(new List<string>() {
                                    "",
                                    channel.Name,
                                    "",
                                    channelSetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                    GetPermissionString((BotPermissionType)(channelSetting?.ChannelPermissionFlags ?? 0)),
                                    channelSetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                    channelSetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                    channelSetting?.ReachedOldestPreload.ToString() ?? "N/A"
                                });

                                var channelCells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 1, FontColor = new SkiaSharp.SKColor(255, 255, 0) } };
                                if (channelSettingInfo.Inherit)
                                {
                                    channelCells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                }

                                tableRowInfos.Add(new TableRowInfo()
                                {
                                    RowId = data.Count - 1,
                                    Cells = channelCells
                                });

                                if (channel is SocketTextChannel socketThextChannel)
                                {
                                    // Current channel is a thread

                                    foreach (var thread in socketThextChannel.Threads)
                                    {
                                        var threadSettingInfo = CommonHelper.GetChannelSettingByThreadId(thread.Id);
                                        var threadSetting = threadSettingInfo.Setting;

                                        // New thread
                                        data.Add(new List<string>() {
                                            "",
                                            "",
                                            "#" + thread.Name,
                                            threadSetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                            GetPermissionString((BotPermissionType)(threadSetting?.ChannelPermissionFlags ?? 0)),
                                            threadSetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.ReachedOldestPreload.ToString() ?? "N/A"
                                        });


                                        var threadCells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 2, FontColor = new SkiaSharp.SKColor(255, 255, 255) } };
                                        if (threadSettingInfo.Inherit || true /* Thread for now always inherit */)
                                        {
                                            threadCells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                        }

                                        tableRowInfos.Add(new TableRowInfo()
                                        {
                                            RowId = data.Count - 1,
                                            Cells = threadCells
                                        });
                                    }
                                }
                            }
                        }


                        var drawTable = new DrawTable(header, data, "", tableRowInfos);

                        var stream = await drawTable.GetImage();
                        if (stream == null)
                            return;// todo some message

                        await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                        stream.Dispose();
                    }
                }

            }

            [Command("set")]
            public async Task SetChannelInfoAsync(int flag, ulong? channelId = null)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!channelId.HasValue)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(guildChannel.Id, flag);
                        await Context.Channel.SendMessageAsync($"Set flag {flag} for channel {guildChannel.Name}", false);
                    }
                    else
                    {
                        var channel = guildChannel.Guild.GetChannel(channelId.Value);

                        DatabaseManager.Instance().UpdateChannelSetting(channel.Id, flag);
                        Context.Channel.SendMessageAsync($"Set flag {flag} for channel {channel.Name}", false);
                    }
                }
            }

            [Command("all")]
            public async Task SetAllChannelInfoAsync(int flag)
            {
                return; // this one is a bit too risky xD
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    var channels = DatabaseManager.Instance().GetDiscordAllChannels(guildChannel.Guild.Id);

                    foreach (var item in channels)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(item.DiscordChannelId, flag, 0, 0, true);
                        await Context.Channel.SendMessageAsync($"Set flag {flag} for channel {item.ChannelName}", false);
                    }
                }
            }


            [Command("flags")]
            public async Task GetChannelInfoFlagsAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Available flags");

                builder.WithColor(255, 0, 0);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                string inlineString = "```";
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    inlineString += $"{flag} ({(int)(flag)})\r\n";
                }

                inlineString = inlineString.Trim() + "```";
                builder.AddField("BotPermissionType", inlineString);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }

        [Group("place")]
        public class PlaceAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task PlaceAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("PLace Admin Help");

                builder.WithColor(0, 0, 255);


                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin place help", "This message :)");
                builder.AddField("admin place verify <user> <true|false>", "Used to verify user for multipixel feature");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("verify")]
            public async Task VerifyPlaceUser(SocketUser user, bool verified)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var success = DatabaseManager.Instance().VerifyDiscordUserForPlace(user.Id, verified);

                await Context.Channel.SendMessageAsync($"Set <@{user.Id}> to {verified} Success: {success}", false);
            }
        }

        [Group("reddit")]
        public class RedditAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task RedditAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin reddit help", "This message :)");
                builder.AddField("admin reddit status", "Returns if there are currently active scrapers");
                builder.AddField("admin reddit add <name>", "Add Subreddit to SubredditInfos");
                builder.AddField("admin reddit ban <name>", "Manually ban");
                builder.AddField("admin reddit start <name>", "Starts the scraper for a specific subreddit if no scraper is currently running");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("status")]
            public async Task CheckStatusAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                CheckReddit();
            }

            [Command("add")]
            public async Task AddSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("Ping the owner and he will add it for you", false);
                    return;
                }

                AddSubreddit(subredditName);
            }

            [Command("ban")]
            public async Task BanSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                DatabaseManager.Instance().BanSubreddit(subredditName);
                await Context.Channel.SendMessageAsync("Banned " + subredditName, false);

            }

            [Command("start")]
            public async Task StartScraperAsync(string subredditName)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();
                if (subreddits.Count > 0)
                {
                    await Context.Channel.SendMessageAsync($"{subreddits.First().SubredditName} is currently running. Try again later", false);
                    return;
                }
                // TODO check if no scraper is active
                await Context.Channel.SendMessageAsync($"Started {subredditName} please wait :)", false);

                if (subredditName.ToLower() == "all")
                {
                    var allSubreddits = DatabaseManager.Instance().GetSubredditsByStatus(false);

                    var allNames = allSubreddits.Select(i => i.SubredditName).ToList();

                    await Context.Channel.SendMessageAsync($"Starting", false);

                    for (int i = 0; i < allNames.Count; i += 100)
                    {
                        var items = allNames.Skip(i).Take(100);
                        await Context.Channel.SendMessageAsync($"{string.Join(", ", items)}", false);
                        // Do something with 100 or remaining items
                    }

                    await Context.Channel.SendMessageAsync($"Please wait :)", false);


                    await Task.Factory.StartNew(() => CommonHelper.ScrapReddit(allNames, Context.Channel));
                }
                else
                {
                    await Task.Factory.StartNew(() => CommonHelper.ScrapReddit(subredditName, Context.Channel));
                }
            }


            private async void CheckReddit()
            {
                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();

                foreach (var subreddit in subreddits)
                {
                    await Context.Channel.SendMessageAsync($"{subreddit.SubredditName} is active", false);
                }

                if (subreddits.Count == 0)
                {
                    await Context.Channel.SendMessageAsync($"No subreddits are currently active", false);
                }
            }

            private async void AddSubreddit(string subredditName)
            {
                var reddit = new RedditClient(Program.ApplicationSetting.RedditSetting.AppId, Program.ApplicationSetting.RedditSetting.RefreshToken, Program.ApplicationSetting.RedditSetting.AppSecret);
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    SubredditManager subManager = new SubredditManager(subredditName, reddit, context);
                    await Context.Channel.SendMessageAsync($"{subManager.SubredditName} was added to the list", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }
            }


            // TODO cleanup this mess


        }


        [Group("keyval")]
        public class KeyValuePairAdminModule : ModuleBase<SocketCommandContext>
        {
            List<string> SupportedTypes = new List<string>() { "Boolean", "Byte", "Char", "DateTime", "DBNull", "Decimal,", "Double", "Enum", "Int16", "Int32", "Int64", "SByte", "Single", "String", "UInt16", "UInt32", "UInt64" };

            private static KeyValueDBManager DBManager = DatabaseManager.KeyValueManager;

            [Command("help")]
            public async Task KeyValuePairAdminHelp()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("KeyValuePair Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin keyval help", "This message :)");
                builder.AddField("admin keyval get <key>", "Get a specific KeyValuePair by Key");
                builder.AddField("admin keyval add <key> <value> <type>", "Add new KeyValuePair");
                builder.AddField("admin keyval update <key> <value> <type>", "Update existing KeyValuePair (Creates one if the key doesn't exist)");
                builder.AddField("admin keyval delete <key>", "Deletes the KeyValuePair");
                builder.AddField("admin keyval list", "Lists all current KeyValuePairs stored in the DB");
                builder.AddField("admin keyval supported", "Lists supported types (IConvertible)");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("get")]
            public async Task GetKeyValuePair(string key)
            {
                var result = DBManager.Get(key);
                await Context.Channel.SendMessageAsync($"Key: **{key}** has the value: **{result.Value}** with type: **{result.Type}**");
            }

            [Command("add")]
            public async Task AddKeyValuePair(string key, string value, string type)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (!SupportedTypes.Contains(type))
                {
                    await Context.Channel.SendMessageAsync($"**{type}** is not supported");
                    return;
                }

                try
                {
                    var result = DBManager.Add(key, value, type);
                    await Context.Channel.SendMessageAsync($"Added new key: **{key}** with value: **{value}** of type **{type}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("update")]
            public async Task UpdateKeyValuePair(string key, string value, string type = null)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (!SupportedTypes.Contains(type))
                {
                    await Context.Channel.SendMessageAsync($"**{type}** is not supported");
                    return;
                }

                try
                {
                    var result = DBManager.Update(key, value, type);
                    await Context.Channel.SendMessageAsync($"Updated key: **{key}** with value: **{value}** of type **{type}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("delete")]
            public async Task DeleteKeyValuePair(string key)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                try
                {
                    DBManager.Delete(key);
                    await Context.Channel.SendMessageAsync($"Deleted key: **{key}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("list")]
            public async Task ListKeyValuePairs()
            {
                var allStoredKeyValuePairs = DBManager.GetAll();

                // TODO better way for future when many keys are stored

                string text = "";
                foreach (var item in allStoredKeyValuePairs)
                {
                    var line = $"{item.Key}:{item.Value}";
                    if (text.Length + line.Length > 1975)
                    {
                        await Context.Channel.SendMessageAsync(text);
                        text = "";
                    }

                    text += line + Environment.NewLine;
                }

                await Context.Channel.SendMessageAsync(text);
            }

            [Command("supported")]
            public async Task ListSupportedTypes()
            {
                await Context.Channel.SendMessageAsync(string.Join(", ", SupportedTypes));
            }
        }
    }
}
