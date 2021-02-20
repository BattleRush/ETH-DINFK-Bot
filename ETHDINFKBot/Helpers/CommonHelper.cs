using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public static class CommonHelper
    {
        public static Color DiscordBackgroundColor
        {
            get { return Color.FromArgb(54, 57, 63); }
        }

        public static bool ContainsForbiddenQuery(string command)
        {
            List<string> forbidden = new List<string>()
            {
                "alter",
                "analyze",
                "attach",
                "transaction",
                "comment",
                "commit",
                "create",
                "delete",
                "detach",
                "database",
                "drop",
                "insert",
                "pragma",
                "reindex",
                "release",
                "replace",
                "rollback",
                "savepoint",
                "update",
                "upsert",
                "vacuum",
                "`" // to not break any formatting
            };

            foreach (var item in forbidden)
            {
                if (command.ToLower().Contains(item.ToLower()))
                    return true;
            }

            return false;
        }

        public static Stream GetStream(Bitmap bitmap)
        {
            Stream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            return ms;

            //await Context.Channel.SendFileAsync(ms, "test.png");
        }

    }
}
