using MagicVision.DataClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagicVision
{
    public partial class SetImporter : Form
    {
        private readonly List<ReferenceCard> referenceCards;
        private readonly PoolVision.MySqlClient sql;

        public SetImporter(List<ReferenceCard> referenceCards, PoolVision.MySqlClient sql)
        {
            InitializeComponent();
            this.referenceCards = referenceCards ?? throw new ArgumentNullException(nameof(referenceCards));
            this.sql = sql ?? throw new ArgumentNullException(nameof(sql));
        }

        private void SetImporter_Load(object sender, EventArgs e)
        {
            Task.Factory.StartNew(PopulateSetsAsync);
        }

        public async Task PopulateSetsAsync()
        {
            try
            {
                status("Downloading Data");
                var cards = await DownloadBulkData();
                status($"Downloaded {cards.Length} cards");
                if (Visible)
                    Invoke(new Action(() =>
                    {
                        progressBar1.Maximum = cards.Length;
                    }));

                var i = 0;
                foreach (var card in cards)
                {
                    await HashCardAsync(card);
                    if (Visible)
                        Invoke(new Action(() =>
                        {
                            progressBar1.Value = ++i;
                        }));
                }
            }
            catch (Exception c)
            {

            }
        }

        async Task<SfCard[]> DownloadBulkData()
        {
            Directory.CreateDirectory("Images");
            const string BulkDataFilename = "bulk-data.json";
            const string uri = "https://archive.scryfall.com/json/scryfall-default-cards.json";
            var bd = new System.IO.FileInfo(BulkDataFilename);
            if (!bd.Exists || DateTime.Now.Subtract(bd.LastWriteTime).TotalHours > 24)
            {
                var client = new HttpClientDownloadWithProgress(uri, BulkDataFilename);
                client.ProgressChanged += (long? totalFileSize, long totalBytesDownloaded, double? progressPercentage) =>
                {
                    status($"Downloading Bulk Data... {totalBytesDownloaded}...");
                };
                await client.StartDownload();
            }
            var blob = File.ReadAllText(BulkDataFilename);
            return JsonConvert.DeserializeObject<SfCard[]>(blob);
        }

        private void status(string status)
        {
            if (Visible)
            {
                Invoke(new Action(() =>
                {
                    label1.Text = status;
                }));
            }
            else
            {
                Console.WriteLine(status);
            }
        }

        private async Task HashCardAsync(SfCard card)
        {
            if (card.promo == true)
            {
                if (card.promo_types != null && card.promo_types.Contains("datestamped"))
                    return;
            }
            var reference = referenceCards.SingleOrDefault(c => c.sf_id == card.id);

            if (reference == null)
            {
                var image = Path.Combine("Images", card.id + ".jpg");
                if (!File.Exists(image))
                {
                    var client = new HttpClientDownloadWithProgress(card.image_uris.large, image);
                    client.ProgressChanged += (long? totalFileSize, long totalBytesDownloaded, double? progressPercentage) =>
                    {
                        status($"Downloading {card.id}: {card.name} ({card.set}) ... {totalBytesDownloaded}...");
                    };
                    await client.StartDownload();
                }

                ulong hash = 0;
                Phash.ph_dct_imagehash(image, ref hash);
                var row = sql.InsertCard(card.name, hash.ToString(), card.set, card.type_line, card.mana_cost, card.rarity, card.id);
                using (DataTable Reader = sql.dbResult($"SELECT * FROM cards WHERE id = {row}"))
                    referenceCards.Add(new ReferenceCard(Reader.Rows[0]));
            }
        }
    }

    public struct SfCard
    {
        public Guid id;
        public string name;
        public string set;
        public Images image_uris;
        public string type_line;
        public string mana_cost;
        public string rarity;
        public bool promo;
        public bool variation;
        public string[] promo_types;
    }

    public class Images
    {
        public string png;
        public string large;
    }
}
