using MagicVision.DataClasses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicVision
{
    class Scryfall
    {
        public static readonly HttpClient client = new HttpClient();

        public static async Task<Set[]> GetSetsAsync()
        {
            const string RequestUri = "https://api.scryfall.com/sets/";
            return (await EnumerateApiAsync<Set>(RequestUri)).ToArray();
        }

        public static async Task<IEnumerable<T>> EnumerateApiAsync<T>(string RequestUri)
        {
            var results = new List<T>();

            var json = await client.GetStringAsync(RequestUri).ConfigureAwait(false);
            var list = JObject.Parse(json);

            foreach (var obj in list.Value<JArray>("data"))
            {
                results.Add(obj.ToObject<T>());
            }
            if (list.Value<bool>("has_more"))
            {
                var next = await EnumerateApiAsync<T>(list.Value<string>("next_page")).ConfigureAwait(false);
                results.AddRange(next);
            }
            return results;
        }
    }
}
