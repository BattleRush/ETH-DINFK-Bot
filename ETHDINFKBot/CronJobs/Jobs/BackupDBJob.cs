using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class BackupDBJob : CronJobService
    {
        private readonly ILogger<BackupDBJob> _logger;
        private readonly string Name = "BackupDBJob";

        public BackupDBJob(IScheduleConfig<BackupDBJob> config, ILogger<BackupDBJob> logger)
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
            using (var location = new SqliteConnection(sourceConnectionString))
            using (var destination = new SqliteConnection(targetConnectionString))
            {
                location.Open();
                destination.Open();
                location.BackupDatabase(destination, "main", "main");
            }
        }
        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            Console.WriteLine("Run BACKUP");

            var dbBackupPath = Path.Combine(Program.BasePath, "Database", "Backup", $"ETHBot_Job_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.db");

            BackupDB(Program.ConnectionString, $"Data Source={dbBackupPath}"); // todo get these 2 from settings and create 2. connection string dynamic

            // TODO check if we can delete any older backups

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
