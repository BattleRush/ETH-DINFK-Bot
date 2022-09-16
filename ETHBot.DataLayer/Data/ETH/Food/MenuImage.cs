using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class MenuImage
    {        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MenuImageId { get; set; }

        public string MenuImageUrl { get; set; }
        public string ImageSearchTerm { get; set; }
        public int Order { get; set; }
        public bool Available { get; set; }
        public bool Enabled { get; set; }
        public bool ManualUpload { get; set; }
        public string Language { get; set; }
    }
}
