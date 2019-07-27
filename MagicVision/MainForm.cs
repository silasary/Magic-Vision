/* Magic Vision
 * Created by Peter Simard
 * You are free to use this source code any way you wish, all I ask for is an attribution
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using DirectX.Capture;
using System.Diagnostics;
using MagicVision.DataClasses;
using PoolVision;
using System.IO;
using System.Threading.Tasks;

namespace MagicVision
{
    public partial class MainForm : Form {
        public static String refCardDir = Path.Combine(Path.GetDirectoryName(typeof(MainForm).Assembly.Location), "Crops");
        private Capture capture;
        private readonly Filters cameraFilters = new Filters();
        private static List<ReferenceCard> referenceCards = new List<ReferenceCard>();

        public static string SqlConString = "SERVER=127.0.0.1;" +
                "DATABASE=magiccards;" +
                "UID=magiccards;" +
                "Allow Zero Datetime=true;" +
                "Password='password'";

        public static MySqlClient sql = new MySqlClient( SqlConString );
        private ImageRecognition imageRecognition;
        private MagicCard[] magicCards;
        private Timer desktop_timer;

        public MainForm() {
            InitializeComponent();
        }

        private void RecalculateHashes_Click(object sender, EventArgs e)
        {
            new SetImporter(referenceCards, sql).Show();
            hashCalcButton.Enabled = false;
        }



        private void MainForm_Load( object sender, EventArgs e ) {
            for( int i = 0; i < cameraFilters.VideoInputDevices.Count; i++ ) {
                comboBox1.Items.Add( new CameraFilter( cameraFilters.VideoInputDevices[i] ) );
            }

            loadSourceCards();
            imageRecognition = new ImageRecognition(referenceCards);
        }

        private void loadSourceCards()
        {
            using (DataTable Reader = sql.dbResult("SELECT * FROM cards"))
            {
                foreach (DataRow r in Reader.Rows)
                {
                    var card = new ReferenceCard(r);

                    referenceCards.Add(card);
                }
            }
            if (!referenceCards.Any())
            {
                using (var setImporter = new SetImporter(referenceCards, sql))
                {
                    setImporter.Show();
                    hashCalcButton.Enabled = false;
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
            lock (ImageRecognition._locker)
            {
                if (magicCards == null)
                {
                    return;
                }

                foreach (MagicCard card in magicCards)
                {
                    Rectangle rect = new Rectangle(card.corners[0].X, card.corners[0].Y, (card.corners[1].X - card.corners[0].X), (card.corners[2].Y - card.corners[1].Y));
                    if (rect.Contains(e.Location))
                    {
                        Debug.WriteLine(card.referenceCard.Name);
                        DebugCard(card);

                    }
                }
            }
        }

        private void DebugCard(MagicCard card)
        {
            cardArtImage.Image = card.cardArtBitmap;
            cardImage.Image = card.cardBitmap;

            cardInfo.Text = "Card Name: " + card.referenceCard.Name + Environment.NewLine +
                "Set: " + (String)card.referenceCard.dataRow["Set"] + Environment.NewLine +
                "Type: " + (String)card.referenceCard.dataRow["Type"] + Environment.NewLine +
                "Casting Cost: " + (String)card.referenceCard.dataRow["Cost"] + Environment.NewLine +
                "Rarity: " + (String)card.referenceCard.dataRow["Rarity"] + Environment.NewLine +
                "Hamming: " + card.hammingValue + Environment.NewLine +
                "Area: " + card.area;
        }

        private void StartCameraButton_Click( object sender, EventArgs e ) {
            capture = new Capture( ( (CameraFilter)comboBox1.SelectedItem ).filter, cameraFilters.AudioInputDevices[0] );
            var vc = capture.VideoCaps;
            capture.FrameSize = new Size( 640, 480 );
            capture.PreviewWindow = cam;
            capture.FrameEvent2 += new Capture.HeFrame( CaptureDone );
            capture.GrapImg();
        }

        private void watchDesktopButton_Click(object sender, EventArgs e)
        {
            desktop_timer = new Timer
            {
                Interval = 30
            };
            desktop_timer.Tick += (s, ee) => { CaptureDesktop(); };
            desktop_timer.Enabled = true;
            // Make sure the timer is disposed when we close.
            if (components == null)
                components = new System.ComponentModel.Container();
            components.Add(desktop_timer);
        }

        private void CaptureDesktop()
        {
            var desktop = new Bitmap(640 * 2, 480 * 2);
            var gfxScreenshot = Graphics.FromImage(desktop);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy);

            imageRecognition.ScanImage(desktop);
            if (imageRecognition.magicCards.Count > 0)
            {
                magicCards = imageRecognition.magicCards.ToArray();
                //var Card = magicCards.
                DebugCard(magicCards[0]);
            }
            image_output.Image = imageRecognition.filteredBitmap;
            camWindow.Image = imageRecognition.cameraBitmap;
        }
    }
}
