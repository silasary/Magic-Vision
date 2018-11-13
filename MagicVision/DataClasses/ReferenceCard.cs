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
        public ulong pHash;
        public DataRow dataRow;
        public string Set;

    }

    class SfCard
    {
        public string Id;
        public string Name;

        public string Set;
        public string Collector_Number;

        public ImageUris Image_Uris;
        public string Type_Line;
        internal string Mana_Cost;
        internal string Rarity;
    }

    class ImageUris
    {
        public string Art_Crop;
    }
}
