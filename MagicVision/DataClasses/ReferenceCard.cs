using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MagicVision.DataClasses
{
    public class ReferenceCard
    {
        public int Id;
        public string CollectorNumber;
        public string Name;
        public UInt64 pHash;
        public Guid sf_id;
        public DataRow dataRow;

        public ReferenceCard()
        {

        }

        public ReferenceCard(DataRow r)
        {
            Id = (int)r["id"];
            Name = (String)r["Name"];
            pHash = UInt64.Parse((String)r["pHash"]);
            sf_id = (Guid)r["sf_id"];
            dataRow = r;
        }
    }
}
