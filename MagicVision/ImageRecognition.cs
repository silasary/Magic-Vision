using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicVision
{
    class ImageRecognition
    {
        private void ScanImage(System.Drawing.Bitmap e)
        {
            lock (_locker)
            {
                magicCardsLastFrame = new List<MagicCard>(magicCards);
                magicCards.Clear();
                cameraBitmap = e;
                cameraBitmapLive = (Bitmap)cameraBitmap.Clone();
                detectQuads(cameraBitmap);
                matchCard();

                image_output.Image = filteredBitmap;
                camWindow.Image = cameraBitmap;
            }
        }
    }
}
