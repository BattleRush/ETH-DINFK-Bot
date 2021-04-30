using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    public class PlacePerformanceInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlacePerformanceHistoryId { get; set; }

        public DateTime DateTime { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int AvgTimeInMs { get; set; }
    }
}
