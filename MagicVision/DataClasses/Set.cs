using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicVision.DataClasses
{
    class Set
    {
        [JsonProperty("code")]
        public string SetCode;
        [JsonProperty("Name")]
        public string Name;

        public string Set_Type;

        [JsonProperty("search_uri")]
        private string search_url;

        public async Task<SfCard[]> GetCardsAsync()
        {
            var cards = await Scryfall.EnumerateApiAsync<SfCard>(search_url).ConfigureAwait(false);
            return cards.ToArray();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
