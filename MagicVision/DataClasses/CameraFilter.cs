using DirectX.Capture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicVision.DataClasses
{
    class CameraFilter
    {
        public Filter filter;
        public override String ToString()
        {
            return filter.Name;
        }

        public CameraFilter(Filter filt)
        {
            filter = filt;
        }
    }
}
