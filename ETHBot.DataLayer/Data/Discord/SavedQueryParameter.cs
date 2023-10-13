using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace ETHBot.DataLayer.Data.Discord
{
    // Not doing nxn relation for the reason that the messages could be edited and me being lazy
    public class SavedQueryParameter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SavedQueryParameterId { get; set; }

        public string ParameterName { get; set; }

        public string ParameterType { get; set; }

        public string DefaultValue { get; set; }

        [ForeignKey("SavedQuery")]
        public int SavedQueryId { get; set; }

        public SavedQuery SavedQuery { get; set; } // obsolete since the message has the author
    }
}
