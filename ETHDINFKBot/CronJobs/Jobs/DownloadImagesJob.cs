using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class DownloadImagesJob : CronJobService
    {
        private readonly ILogger<DownloadImagesJob> _logger;
        private readonly string Name = "DownloadImagesJob";

        public DownloadImagesJob(IScheduleConfig<DownloadImagesJob> config, ILogger<DownloadImagesJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }
        public void BackupDB(string sourceConnectionString, string targetConnectionString)
        {
            try
            {
                // TODO job for maria db
                /*
                using (var location = new SqliteConnection(sourceConnectionString))
                using (var destination = new SqliteConnection(targetConnectionString))
                {
                    location.Open();
                    destination.Open();
                    location.BackupDatabase(destination);
                }*/
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed Backup");
                //textChannel.SendMessageAsync("Failed DB Backup: " + ex.Message);
            }
        }
        public override Task DoWork(CancellationToken cancellationToken)
        {

            ProcessChannels();


            return Task.CompletedTask;
        }

        private async void ProcessChannels()
        {
            // todo config
            ulong guildId = 747752542741725244;
            //ulong spamChannel = 768600365602963496;
            //var guild = Program.Client.GetGuild(guildId);
            //var textChannel = guild.GetTextChannel(spamChannel);

            var keyValueDBManager = DatabaseManager.KeyValueManager;

            string basePath = keyValueDBManager.Get<string>("ImageScrapeBasePath");

            int scrapePerRun = keyValueDBManager.Get<int>("MessageScrapePerRun");
            string imageScrapeChannelIdsString = keyValueDBManager.Get<string>("ImageScrapeChannelIds");

            if (string.IsNullOrWhiteSpace(basePath))
            {
                _logger.LogError("ImageScrapeBasePath not set");
                return;
            }

            if (scrapePerRun == 0)
                scrapePerRun = 10_000;

            if (imageScrapeChannelIdsString == null)
            {
                _logger.LogError("ImageScrapeChannelIds not set");
                return;
            }

            var imageScrapeChannelIds = imageScrapeChannelIdsString.Split(',').Select(x => ulong.Parse(x)).ToList();

            var guild = Program.Client.GetGuild(guildId);

            var channels = guild.Channels.Where(x => imageScrapeChannelIds.Contains(x.Id)).ToList();

            foreach (var channel in channels)
            {
                var textChannel = channel as SocketTextChannel;
                ulong lastMessageForChannel = keyValueDBManager.Get<ulong>($"LastScapedMessageForChannel_{channel.Id}");

                var messages = textChannel.GetMessagesAsync(lastMessageForChannel, Direction.Before, scrapePerRun).FlattenAsync().Result.ToList();

                if (messages.Count == 0)
                {
                    _logger.LogInformation($"No new messages left for channel {channel.Name}");
                    continue;
                }

                _logger.LogInformation($"Found {messages.Count} new messages for channel {channel.Name}");

                int botCount = 0;
                int noUrlCount = 0;

                foreach (var message in messages)
                {
                    if(message.Author.IsBot)
                    {
                        botCount++;
                        continue;
                    }

                    if (message.Attachments.Count == 0 && message.Embeds.Count == 0)
                    {
                        noUrlCount++;
                        continue;
                    }

                    List<string> urls = new List<string>();

                    foreach (var attachment in message.Attachments)
                    {
                        urls.Add(attachment.Url);
                    }

                    foreach (var embed in message.Embeds)
                    {
                        if (embed.Type == EmbedType.Image)
                        {
                            urls.Add(embed.Url);
                        }

                        if (embed.Type == EmbedType.Video)
                        {
                            urls.Add(embed.Url);
                        }

                        if (embed.Type == EmbedType.Rich)
                        {
                            urls.Add(embed.Url);
                        }
                    }

                    foreach (var url in urls)
                    {
                        DownloadFile(new HttpClient(), message, message.Id, url, urls.IndexOf(url), basePath, "");
                    }
                }

                // update last message id
                keyValueDBManager.Update($"LastScapedMessageForChannel_{channel.Id}", messages.Max(x => x.Id));
            }
        }

        private async void DownloadFile(HttpClient client, IMessage message, ulong messageId, string url, int index, string basePath, string downloadFileName)
        {
            // dont download webp images if possible
            url = url.Replace("&format=webp", "");

            // remove width and height query params
            url = Regex.Replace(url, @"&width=\d+", "");
            url = Regex.Replace(url, @"&height=\d+", "");

            // if the parameter is at the start with ? then remove it
            url = Regex.Replace(url, @"\?width=\d+", "?");
            url = Regex.Replace(url, @"\?height=\d+", "?");

            // if url ends with ? then remove it
            url = Regex.Replace(url, @"\?$", "");

            try
            {
                string fileName = downloadFileName;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = url.Split('/').Last();
                    fileName = fileName.Split('?').First();
                }

                fileName = fileName.ToLower(); // so no png and PNG


                if (!fileName.Contains("."))
                {
                    _logger.LogInformation($"Filename '{fileName}' is invalid from content: ```{message.Content}```", false);
                    throw new Exception("Invalid filename");
                }


                // remove any . except the last one
                string fileExtension = fileName.Split('.').Last();
                string name = fileName.Substring(0, fileName.Length - fileExtension.Length - 1);

                // limit filename to 150 chars max
                if (name.Length > 100)
                    name = name.Substring(0, 100);


                name = name.Replace(".", "");

                // remove any non alphanumeric chars from name
                name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

                fileName = $"{message.Id}_{index}_{name}.{fileExtension}";

                var emojiDate = SnowflakeUtils.FromSnowflake(messageId);
                string additionalFolder = $"{emojiDate.Year}-{emojiDate.Month:00}";

                // put image into folder Python/memes
                string filePath = Path.Combine(basePath, additionalFolder, fileName);

                if(Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // check if folder exists
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    _logger.LogInformation($"Folder {Path.GetDirectoryName(filePath)} does not exist", false);
                    _logger.LogInformation($"Content: ```{message.Content}```");

                }

                // check if file exists
                if (File.Exists(filePath))
                {
                    _logger.LogInformation($"File {filePath} already exists", false);
                    return;
                }


                // check if opening the url how big the file is
                // if its too big then skip
                var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                var headResponse = client.SendAsync(headRequest).Result;

                if (headResponse.Content.Headers.ContentLength > 10_000_000)
                {
                    _logger.LogInformation($"File {filePath} is too big: {headResponse.Content.Headers.ContentLength}", false);
                    return;
                }

                // check if the url is a downloadable file
                // if not then skip
                var headContentType = headResponse.Content.Headers.ContentType.MediaType;
                // check if image or video
                if (headContentType.StartsWith("image") || headContentType.StartsWith("video"))
                {
                    // download the file
                    byte[] bytes = client.GetByteArrayAsync(url).Result;

                    // get the file extension from the content type
                    string fileExtensionFromContentType = headContentType.Split('/').Last();

                    // check if the filename has the correct extension if not then replace it or add if missing
                    if (!fileName.EndsWith(fileExtensionFromContentType))
                    {
                        if(fileName.Contains("."))
                            fileName = fileName.Split('.').First() + "." + fileExtensionFromContentType;
                        else
                            fileName = fileName + "." + fileExtensionFromContentType;
                    }

                    File.WriteAllBytes(filePath, bytes);
                }
                else
                {
                    _logger.LogInformation($"File {filePath} is not an image or video: {headContentType}", false);
                }
            }
            catch (HttpException ex)
            {
                // if status code 404 then skip
                if (ex.HttpCode == HttpStatusCode.NotFound) return;

                _logger.LogInformation($"Download error in attachment url <{url}>: " + ex.Message.ToString(), false);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
