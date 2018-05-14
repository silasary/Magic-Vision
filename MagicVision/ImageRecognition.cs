using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using MagicVision.DataClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace MagicVision
{
    class ImageRecognition
    {
        public Bitmap cameraBitmap { get; private set; } = new Bitmap( 640, 480 );
        private Bitmap cameraBitmapLive;
        public Bitmap filteredBitmap {get; private set;}
        private Bitmap cardBitmap;
        private Bitmap cardArtBitmap;

        internal static readonly object _locker = new object();

        public List<MagicCard> magicCards { get; private set; } = new List<MagicCard>();
        private List<MagicCard> magicCardsLastFrame = new List<MagicCard>();
        private IEnumerable<ReferenceCard> referenceCards;

        public ImageRecognition(IEnumerable<ReferenceCard> referenceCards)
        {
            this.referenceCards = referenceCards;
        }

        internal void ScanImage(Bitmap e)
        {
            lock (_locker)
            {
                magicCardsLastFrame = new List<MagicCard>(magicCards);
                magicCards.Clear();
                cameraBitmap = e;
                cameraBitmapLive = (Bitmap)cameraBitmap.Clone();
                detectQuads(cameraBitmap);
                matchCard();
            }
        }

        private void matchCard()
        {
            var cardTempId = 0;
            foreach (MagicCard card in magicCards)
            {
                cardTempId++;
                // Write the image to disk to be read by the pHash library.. should really find
                // a way to pass a pointer to image data directly
                card.cardArtBitmap.Save("tempCard" + cardTempId + ".jpg", ImageFormat.Jpeg);


                // Calculate art bitmap hash
                UInt64 cardHash = 0;
                Phash.ph_dct_imagehash("tempCard" + cardTempId + ".jpg", ref cardHash);

                var lowestHamming = int.MaxValue;
                ReferenceCard bestMatch = null;

                foreach (ReferenceCard referenceCard in referenceCards)
                {
                    int hamming = Phash.HammingDistance(referenceCard.pHash, cardHash);
                    if (hamming < lowestHamming)
                    {
                        lowestHamming = hamming;
                        bestMatch = referenceCard;
                    }
                }

                if (bestMatch != null)
                {
                    card.referenceCard = bestMatch;
                    card.hammingValue = lowestHamming;
                    //Debug.WriteLine("Highest Similarity: " + bestMatch.name + " ID: " + bestMatch.cardId.ToString());

                    Graphics g = Graphics.FromImage(cameraBitmap);
                    var font = new Font("Tahoma", 25);
                    g.DrawString(bestMatch.Name, font, Brushes.Black, new PointF(card.corners[0].X - 29, card.corners[0].Y - 39));
                    g.DrawString(bestMatch.Name, font, Brushes.Yellow, new PointF(card.corners[0].X - 30, card.corners[0].Y - 40));
                    g.Dispose();
                }
            }
        }

        double GetDeterminant(double x1, double y1, double x2, double y2)
        {
            return x1 * y2 - x2 * y1;
        }

        double GetArea(IList<IntPoint> vertices)
        {
            if (vertices.Count < 3)
            {
                return 0;
            }
            double area = GetDeterminant(vertices[vertices.Count - 1].X, vertices[vertices.Count - 1].Y, vertices[0].X, vertices[0].Y);
            for (int i = 1; i < vertices.Count; i++)
            {
                area += GetDeterminant(vertices[i - 1].X, vertices[i - 1].Y, vertices[i].X, vertices[i].Y);
            }
            return area / 2;
        }

        private void detectQuads(Bitmap bitmap)
        {
            // Greyscale
            filteredBitmap = Grayscale.CommonAlgorithms.BT709.Apply(bitmap);

            // edge filter
            SobelEdgeDetector edgeFilter = new SobelEdgeDetector();
            edgeFilter.ApplyInPlace(filteredBitmap);

            // Threshhold filter
            Threshold threshholdFilter = new Threshold(190);
            threshholdFilter.ApplyInPlace(filteredBitmap);

            BitmapData bitmapData = filteredBitmap.LockBits(
                new Rectangle(0, 0, filteredBitmap.Width, filteredBitmap.Height),
                ImageLockMode.ReadWrite, filteredBitmap.PixelFormat);


            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 125;
            blobCounter.MinWidth = 125;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            filteredBitmap.UnlockBits(bitmapData);

            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Bitmap bm = new Bitmap(filteredBitmap.Width, filteredBitmap.Height, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(bm);
            g.DrawImage(filteredBitmap, 0, 0);

            Pen pen = new Pen(Color.Red, 5);
            List<IntPoint> cardPositions = new List<IntPoint>();


            // Loop through detected shapes
            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners;
                bool sameCard = false;

                // is triangle or quadrilateral
                if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                {
                    // get sub-type
                    PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);

                    // Only return 4 corner rectanges
                    if ((subType == PolygonSubType.Parallelogram || subType == PolygonSubType.Rectangle) && corners.Count == 4)
                    {
                        // Check if its sideways, if so rearrange the corners so it's veritcal
                        rearrangeCorners(corners);

                        // Prevent it from detecting the same card twice
                        foreach (IntPoint point in cardPositions)
                        {
                            if (corners[0].DistanceTo(point) < 40)
                                sameCard = true;
                        }

                        if (sameCard)
                            continue;

                        // Hack to prevent it from detecting smaller sections of the card instead of the whole card
                        if (GetArea(corners) < 20000)
                            continue;

                        cardPositions.Add(corners[0]);

                        g.DrawPolygon(pen, ToPointsArray(corners));

                        // Extract the card bitmap
                        QuadrilateralTransformation transformFilter = new QuadrilateralTransformation(corners, 211, 298);
                        cardBitmap = transformFilter.Apply(cameraBitmap);

                        List<IntPoint> artCorners = new List<IntPoint>();
                        artCorners.Add(new IntPoint(14, 35));
                        artCorners.Add(new IntPoint(193, 35));
                        artCorners.Add(new IntPoint(193, 168));
                        artCorners.Add(new IntPoint(14, 168));

                        // Extract the art bitmap
                        QuadrilateralTransformation cartArtFilter = new QuadrilateralTransformation(artCorners, 183, 133);
                        cardArtBitmap = cartArtFilter.Apply(cardBitmap);

                        MagicCard card = new MagicCard();
                        card.corners = corners;
                        card.cardBitmap = cardBitmap;
                        card.cardArtBitmap = cardArtBitmap;
                        card.area = GetArea(corners);

                        magicCards.Add(card);
                    }
                }
            }

            pen.Dispose();
            g.Dispose();

            filteredBitmap = bm;
        }

        // Move the corners a fixed amount
        private void shiftCorners(List<IntPoint> corners, IntPoint point)
        {
            int xOffset = point.X - corners[0].X;
            int yOffset = point.Y - corners[0].Y;

            for (int x = 0; x < corners.Count; x++)
            {
                IntPoint point2 = corners[x];

                point2.X += xOffset;
                point2.Y += yOffset;

                corners[x] = point2;
            }
        }


        private void rearrangeCorners(List<IntPoint> corners)
        {
            float[] pointDistances = new float[4];

            for (int x = 0; x < corners.Count; x++)
            {
                IntPoint point = corners[x];

                pointDistances[x] = point.DistanceTo((x == (corners.Count - 1) ? corners[0] : corners[x + 1]));
            }

            float shortestDist = float.MaxValue;
            Int32 shortestSide = Int32.MaxValue;

            for (int x = 0; x < corners.Count; x++)
            {
                if (pointDistances[x] < shortestDist)
                {
                    shortestSide = x;
                    shortestDist = pointDistances[x];
                }
            }

            if (shortestSide != 0 && shortestSide != 2)
            {
                IntPoint endPoint = corners[0];
                corners.RemoveAt(0);
                corners.Add(endPoint);
            }
        }

        // Conver list of AForge.NET's points to array of .NET points
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }

    }
}
