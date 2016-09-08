/* Magic Vision
 * Created by Peter Simard
 * You are free to use this source code any way you wish, all I ask for is an attribution
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using DirectX.Capture;
using System.Diagnostics;
using MagicVision.DataClasses;
using PoolVision;
using System.IO;

namespace MagicVision
{
    public partial class MainForm : Form {
        private String refCardDir = @"D:/Work/Magic OCR/cardimages/Crops/"; //@"C:\Users\Pete\Pictures\New Phyrexia\Crops\";
        private Capture capture = null;
        private Filters cameraFilters = new Filters();
        private List<ReferenceCard> referenceCards = new List<ReferenceCard>();

        public static string SqlConString = "SERVER=192.168.1.123;" +
                "DATABASE=magiccards;" +
                "UID=magiccards;" +
                "Allow Zero Datetime=true;" +
                "Password='password'";

        public MySqlClient sql = new MySqlClient( SqlConString );
        private ImageRecognition imageRecognition;
        private MagicCard[] magicCards;

        public MainForm() {
            InitializeComponent();
        }

        private void button1_Click( object sender, EventArgs e ) {
            foreach (ReferenceCard card in referenceCards) {
                var image = Path.Combine(refCardDir, (string)card.dataRow["Set"], card.cardId + ".jpg");
                if (File.Exists(image))
                {
                    Phash.ph_dct_imagehash(image, ref card.pHash );
                    sql.dbNone( "UPDATE cards SET pHash=" + card.pHash.ToString() + " WHERE id=" + card.cardId );
                }
            }
        }



        private void Form1_Load( object sender, EventArgs e ) {
            for( int i = 0; i < cameraFilters.VideoInputDevices.Count; i++ ) {
                comboBox1.Items.Add( new CameraFilter( cameraFilters.VideoInputDevices[i] ) );
            }

            loadSourceCards();
            imageRecognition = new ImageRecognition(referenceCards);
        }

        private void loadSourceCards() {
            using( DataTable Reader = sql.dbResult( "SELECT * FROM cards" ) ) {
                foreach( DataRow r in Reader.Rows ) {
                    ReferenceCard card = new ReferenceCard();
                    card.cardId = (String)r["id"];
                    card.name = (String)r["Name"];
                    card.pHash = UInt64.Parse( (String)r["pHash"] );
                    card.dataRow = r;

                    referenceCards.Add( card );
                }
            }
        }

        private void CaptureDone( System.Drawing.Bitmap e ) {
            imageRecognition.ScanImage(e);
            if (imageRecognition.magicCards.Count > 0)
                magicCards = imageRecognition.magicCards.ToArray();
            image_output.Image = imageRecognition.filteredBitmap;
            camWindow.Image = imageRecognition.cameraBitmap;

        }




        private void camWindow_MouseClick( object sender, MouseEventArgs e ) {
                foreach (MagicCard card in magicCards)
                {
                    Rectangle rect = new Rectangle(card.corners[0].X, card.corners[0].Y, (card.corners[1].X - card.corners[0].X), (card.corners[2].Y - card.corners[1].Y));
                    if (rect.Contains(e.Location))
                    {
                        Debug.WriteLine(card.referenceCard.name);
                        cardArtImage.Image = card.cardArtBitmap;
                        cardImage.Image = card.cardBitmap;

                        cardInfo.Text = "Card Name: " + card.referenceCard.name + Environment.NewLine +
                            "Set: " + (String)card.referenceCard.dataRow["Set"] + Environment.NewLine +
                            "Type: " + (String)card.referenceCard.dataRow["Type"] + Environment.NewLine +
                            "Casting Cost: " + (String)card.referenceCard.dataRow["Cost"] + Environment.NewLine +
                            "Rarity: " + (String)card.referenceCard.dataRow["Rarity"] + Environment.NewLine;

                    }
                }
        }

        private void button1_Click_1( object sender, EventArgs e ) {
            capture = new Capture( ( (CameraFilter)comboBox1.SelectedItem ).filter, cameraFilters.AudioInputDevices[0] );
            VideoCapabilities vc = capture.VideoCaps;
            capture.FrameSize = new Size( 640, 480 );
            capture.PreviewWindow = cam;
            capture.FrameEvent2 += new Capture.HeFrame( CaptureDone );
            capture.GrapImg();
        }
    }
}
