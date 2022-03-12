using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class ApplicationSetting
    {
        public string DiscordToken { get; set; }
        public ulong Owner { get; set; }
        public ulong BaseGuild { get; set; }

        public string MariaDBFullUserName { get; set; }
        public string MariaDBReadOnlyUserName { get; set; }
        public string MariaDBName { get; set; }

        public ConnectionStringsSetting ConnectionStringsSetting { get; set; }

        public string CertFilePath { get; set; }
        public string FFMpegPath { get; set; }
        public string CDNPath { get; set; }
        public string BasePath { get; set; }

        public RedditSetting RedditSetting { get; set; }
        public PostgreSQLSetting PostgreSQLSetting { get; set; }
    }

    public class ConnectionStringsSetting
    {
        public string ConnectionString_Full { get; set; }
        public string ConnectionString_ReadOnly { get; set; }
    }

    public class RedditSetting
    {
        public string AppId { get; set; }
        public string RefreshToken { get; set; }
        public string AppSecret { get; set; }
    }

    public class PostgreSQLSetting
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string OwnerUsername { get; set; }
        public string OwnerPassword { get; set; }
        public string DMDBUserUsername { get; set; }
        public string DMDBUserPassword { get; set; }
    }

    // ignore for now

    /*      "Logging": {
        "LogLevel": {
          "Default": "Warning",
          "Microsoft": "Warning",
          "Microsoft.Hosting.Lifetime": "Warning"
        }
      }*/
}
