﻿using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

using CreamInstaller.Epic.GraphQL;
using CreamInstaller.Utility;

using Newtonsoft.Json;

namespace CreamInstaller.Epic;

internal static class EpicStore
{
    internal static async Task<List<(string id, string name, string product, string icon, string developer)>> ParseDlcIds(string categoryNamespace)
    {
        List<(string id, string name, string product, string icon, string developer)> dlcIds = new();
        Response response = await QueryGraphQL(categoryNamespace);
        if (response is null)
            return dlcIds;
        try { File.WriteAllText(ProgramData.AppInfoPath + @$"\{categoryNamespace}.json", JsonConvert.SerializeObject(response, Formatting.Indented)); } catch { }
        List<Element> elements = new(response.Data.Catalog.CatalogOffers.Elements);
        elements.AddRange(response.Data.Catalog.SearchStore.Elements);
        foreach (Element element in elements)
        {
            string product = null;
            try { product = element.CatalogNs.Mappings[0].PageSlug; } catch { }
            string icon = null;
            for (int i = 0; i < element.KeyImages?.Length; i++)
            {
                KeyImage keyImage = element.KeyImages[i];
                if (keyImage.Type == "Thumbnail")
                {
                    icon = keyImage.Url;
                    break;
                }
            }
            (string id, string name, string product, string icon, string developer) app = (element.Items[0].Id, element.Title, product, icon, element.Developer);
            if (!dlcIds.Contains(app))
                dlcIds.Add(app);
        }
        return dlcIds;
    }

    internal static async Task<Response> QueryGraphQL(string categoryNamespace)
    {
        string encoded = HttpUtility.UrlEncode(categoryNamespace);
        Request request = new(encoded);
        string payload = JsonConvert.SerializeObject(request);
        HttpContent content = new StringContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpClient client = HttpClientManager.HttpClient;
        if (client is null) return null;
        HttpResponseMessage httpResponse = await client.PostAsync("https://graphql.epicgames.com/graphql", content);
        httpResponse.EnsureSuccessStatusCode();
        string response = await httpResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Response>(response);
    }
}
