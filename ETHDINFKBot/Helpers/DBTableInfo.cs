using System;
using System.Collections.Generic;
using System.Text;

namespace ETHDINFKBot.Helpers
{
    public class DBTableInfo
    {
        public string TableName { get; set; }
        public List<DBFieldInfo> FieldInfos { get; set; }
    }


    public class DBFieldInfo
    {
        public int Id { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public bool Nullable { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // TODO other type
                                         // 
        public string GeneralType { get; set; }

        public ForeignKeyInfo ForeignKeyInfo { get; set; }

        // TODO link to tables
        // TODO link from tables
    }

    public class ForeignKeyInfo
    {
        public string FromTable { get; set; }
        public string FromTableFieldName { get; set; }
        public string ToTable { get; set; }
        public string ToTableFieldName { get; set; }
    }
}
