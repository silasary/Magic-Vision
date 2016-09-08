using AForge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MagicVision.DataClasses
{
    class MagicCard
    {
        public ReferenceCard referenceCard;
        public List<IntPoint> corners;
        public Bitmap cardBitmap;
        public Bitmap cardArtBitmap;
    }
}
