using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MagicVision.DataClasses
{
    class ReferenceCard
    {
        public int Id;
        public string CollectorNumber;
        public string Name;
        public UInt64 pHash;
        public DataRow dataRow;
    }
}
