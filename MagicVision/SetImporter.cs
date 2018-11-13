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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagicVision
{
    public partial class SetImporter : Form
    {
        public SetImporter(List<ReferenceCard> referenceCards)
        {
            InitializeComponent();
            this.referenceCards = referenceCards;
        }

        private void SetImporter_Load(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            Task.Factory.StartNew(PopulateSetsAsync);

        }

        private async Task PopulateSetsAsync()
        {
            var typedSets = await Scryfall.GetSetsAsync();
            Invoke(new Action(() =>
            {
                checkedListBox1.Items.AddRange(typedSets);
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var items = checkedListBox1.CheckedItems.Cast<Set>().ToArray();
            if (!items.Any())
            {
                items = checkedListBox1.SelectedItems.Cast<Set>().ToArray();
            }
            DownloadSets(items).ConfigureAwait(false);

        }

        static readonly SemaphoreSlim downloadThrottler = new SemaphoreSlim(initialCount: 4);

        private async Task DownloadSets(params Set[] items)
        {
            var tasks = new List<Task>();
            foreach (var set in items)
            {
                Log($"Fetching cards for {set.Name}");
                var cards = await set.GetCardsAsync().ConfigureAwait(false);
                foreach (var card in cards)
                {
                    await downloadThrottler.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        await DownloadCrop(card).ContinueWith(GenerateHashAsync).ContinueWith(AddToDb);
                        downloadThrottler.Release();
                    }));
                }
            }
            await Task.WhenAll(tasks);
            Log("Done");
        }

        private async Task<(SfCard, HttpResponseMessage)> DownloadCrop(SfCard card)
        {
            Log($"Downloading {card.Name}");
            return (card, await Scryfall.client.GetAsync(card.Image_Uris.Art_Crop));
        }

        private async Task<(SfCard, ulong)> GenerateHashAsync(Task<(SfCard, HttpResponseMessage)> task)
        {
            (var card, var httpResponse) = await task;
            Log($"Hashing {card.Name}");
            var path = httpResponse.RequestMessage.RequestUri.AbsolutePath.Replace('/', '_');
            var bytes = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            File.WriteAllBytes(path, bytes);
            // Do Hash
            ulong cardHash = 0;
            Phash.ph_dct_imagehash(path, ref cardHash);
            File.Delete(path);
            return (card, cardHash);
        }

        static SemaphoreSlim inserter = new SemaphoreSlim(1);
        private readonly List<ReferenceCard> referenceCards;

        private async Task AddToDb(Task<Task<(SfCard, ulong)>> task)
        {
            (var card, var hash) = await await task;
            await inserter.WaitAsync();
            Log($"Inserting {card.Name}");
            var param = new Dictionary<string, object>
            {
                { "?name", card.Name },
                { "?hash", hash },
                { "?setcode", card.Set },
                { "?typeline", card.Type_Line },
                { "?manacost", card.Mana_Cost },
                { "?rarity", card.Rarity },
                { "?num", card.Collector_Number },
            };
            MainForm.sql.dbResult("INSERT INTO `cards` (`Name`, `pHash`, `Set`, `Type`, `Cost`, `Rarity`, `Num`) VALUES" +
                $"(?name, ?hash, ?setcode, ?typeline, ?manacost, ?rarity, ?num);", param);
            referenceCards.Add(new ReferenceCard
            {
                Name = card.Name,
                CollectorNumber = card.Collector_Number,
                pHash = hash,
                Set = card.Set,
            });
            inserter.Release();
        }

        private void Log(string v)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), v);
                return;
            }

            logBox.Items.Insert(0, v);
            if (logBox.Items.Count > 14)
                logBox.Items.RemoveAt(14);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var typedSets = await Scryfall.GetSetsAsync();
                var toImport = new List<Set>();
                var knownSets = referenceCards.GroupBy(c => c.Set).Select(g => g.Key).ToArray();

                foreach (var set in typedSets)
                {
                    if (knownSets.Contains(set.SetCode))
                        continue;
                    Log($"Queuing {set.Name}");
                    toImport.Add(set);
                }
                await DownloadSets(toImport.ToArray());
            });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var typedSets = await Scryfall.GetSetsAsync();
                var toImport = new List<Task>();
                var knownSets = referenceCards.GroupBy(c => c.Set).Select(g => g.Key).ToArray();
                var queue = new List<Set>();
                foreach (var set in typedSets)
                {
                    switch (set.Set_Type)
                    {
                        case "masterpiece":
                        case "promo":
                        case "token":
                        case "memorabilia":
                            continue;
                        case "from_the_vault":
                        case "box":
                        case "masters":
                        case "expansion":
                        case "core":
                        case "commander":
                        case "starter":
                        case "duel_deck":
                        case "spellbook":
                        case "draft_innovation":
                        case "funny":
                        case "archenemy":
                        case "planechase":
                            break;
                        default:
                            break;
                    }
                    if (knownSets.Contains(set.SetCode))
                        continue;
                    
                    Log($"Queuing {set.Name}");
                    queue.Add(set);
                    if (queue.Count > 3)
                    {
                        toImport.Add(DownloadSets(queue.ToArray()));
                        queue.Clear();
                        await Task.WhenAll(toImport);
                    }
                }
                toImport.Add(DownloadSets(queue.ToArray()));
                await Task.WhenAll(toImport);
                Log("Completed 'Normal' sets");
            });
        }
    }
}
