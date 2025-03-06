using System;
using System.Collections.Generic;
using System.Text;

namespace ETHBot.DataLayer.Data.Enums
{
    [Flags]
    public enum BotPermissionType
    {
        None = 0,
        Read = 1,
        Write = 2,
        React = 4,
        EnableSave = 8,
        SaveMessage = 16,
        EnableType1Commands = 32,
        EnableType2Commands = 64,    // Usually the commands that lead to spam
        RemovedPingMessage = 128,
        EnableEmojiParsing = 256,
        EnableFoodCommands = 512
    }
}
