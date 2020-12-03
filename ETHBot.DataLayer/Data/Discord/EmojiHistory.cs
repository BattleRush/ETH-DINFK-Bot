using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class EmojiHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int EmojiHistoryId { get; set; }

        public bool IsReaction { get; set; }
        public bool IsBot { get; set; }
        public int Count { get; set; }
        public DateTime DateTimePosted { get; set; }

        [ForeignKey("EmojiStatistic")]
        public int EmojiStatisticId { get; set; }

        public EmojiStatistic EmojiStatistic { get; set; }
    }
}
