﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    // TODO Accomodate normal ones
    // TODO rework and make emojiId the main id instead of the autogenerated one
    // TODO rename from emoji to emote
    /*public class EmojiStatistic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmojiInfoId { get; set; }
        public string EmojiName { get; set; }
        public ulong EmojiId { get; set; }
        public ulong FallbackEmojiId { get; set; }
        public bool Animated { get; set; }
        public int UsedAsReaction { get; set; }
        public int UsedInText { get; set; }
        public int UsedInTextOnce { get; set; }
        public int UsedByBots { get; set; }
        public string Url { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public byte[] ImageData { get; set; }
        public bool Blocked { get; set; }

        // TODO
        //public string Fingerprint { get; set; }
    }*/
}
