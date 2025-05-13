using Discord;
using Discord.Net;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Data;
using ETHDINFKBot.Helpers;
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
    public class ProcessImagesJob : CronJobService
    {
        private readonly ILogger<ProcessImagesJob> _logger;
        private readonly string Name = "ProcessImagesJob";

        private HttpClient HttpClient { get; }

        private FileDBManager FileDBManager { get; }

        public ProcessImagesJob(IScheduleConfig<ProcessImagesJob> config, ILogger<ProcessImagesJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            FileDBManager = FileDBManager.Instance();

            _logger = logger;
            HttpClient = new HttpClient();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }


        public override Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                // get 1000 files to process
                FileDBManager fileDBManager = FileDBManager.Instance();

                var files = fileDBManager.GetFilesToOcrProcess();

                if (files == null || files.Count == 0)
                {
                    //_logger.LogInformation("No files to process");
                    return Task.CompletedTask;
                }

                Console.WriteLine($"Found {files.Count} files to process");

                foreach (var file in files)
                {
                    var success = DoOCR(file).Result;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessChannels");
            }

            return Task.CompletedTask;
        }

        private async Task<bool> DoOCR(DiscordFile discordFile)
        {
            try
            {
                string path = discordFile.FullPath;

                int port = 13225;
                string url = "http://localhost:" + port + "/run_ocr";

                // parameter is path_to_file
                var payload = new { path_to_file = path };

                StringContent json = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var result = await HttpClient.PostAsync(url, json);

                if(result.StatusCode != HttpStatusCode.OK)
                {
                    // sleep for 1 second and try again
                    Thread.Sleep(1000);
                    return false;
                }

                if (result.StatusCode == HttpStatusCode.OK)
                {
                    // get result
                    var content = await result.Content.ReadAsStringAsync();
                    // save result in db

                    /*# process results into json
    # structure of json:
    # [{
    #   "text": "text",
    #   "coordinates": {
    #       "top_left": [x, y],
    #       "top_right": [x, y],
    #       "bottom_left": [x, y],
    #       "bottom_right": [x, y]
    #   },
    #   "confidence": confidence
    # }]*/

                    // string to object
                    dynamic jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                    string fullText = "";
                    foreach (var item in jsonResult)
                    {
                        string text = item.text;
                        string confidenceStr = item.confidence;

                        // allow for comma and dot
                        double confidence = double.Parse(confidenceStr, System.Globalization.CultureInfo.InvariantCulture);

                        int[] topLeft = item.coordinates.top_left.ToObject<int[]>();
                        int[] topRight = item.coordinates.top_right.ToObject<int[]>();
                        int[] bottomLeft = item.coordinates.bottom_left.ToObject<int[]>();
                        int[] bottomRight = item.coordinates.bottom_right.ToObject<int[]>();

                        OcrBox ocrBox = new OcrBox()
                        {
                            Text = text,
                            Probability = confidence,
                            TopLeftX = topLeft[0],
                            TopLeftY = topLeft[1],
                            TopRightX = topRight[0],
                            TopRightY = topRight[1],
                            BottomRightX = bottomRight[0],
                            BottomRightY = bottomRight[1],
                            BottomLeftX = bottomLeft[0],
                            BottomLeftY = bottomLeft[1],
                            DiscordFileId = discordFile.DiscordFileId,
                        };

                        // save in db
                        FileDBManager.SaveOcrBox(ocrBox);

                        fullText += text + " ";
                    }

                    discordFile.OcrText = fullText;
                    discordFile.OcrDone = true;

                    // update file with full text
                    FileDBManager.UpdateDiscordFile(discordFile);

                    //Console.WriteLine(content);
                }
            }
            catch(Exception ex)
            {
                Thread.Sleep(60_000);
                Console.WriteLine("Error in DoOCR");
                Console.WriteLine(ex);
            }

            return true;
        }



        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
