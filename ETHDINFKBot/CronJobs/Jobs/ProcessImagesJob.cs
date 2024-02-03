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

        public ProcessImagesJob(IScheduleConfig<ProcessImagesJob> config, ILogger<ProcessImagesJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessChannels");
            }

            return Task.CompletedTask;
        }

        public List<DiscordFile> GetFilesToOcrProcess()
        {
            // call db to see files without ocr
            return null;
        }

        private async void DoOCR()
        {
            
        }

        

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
