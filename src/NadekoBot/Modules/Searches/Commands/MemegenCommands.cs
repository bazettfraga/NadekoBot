using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System.Net.Http;
using NadekoBot.Extensions;
using Discord.WebSocket;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Memelist()
        {
            var channel = (SocketTextChannel)Context.Channel;
            HttpClientHandler handler = new HttpClientHandler();

            handler.AllowAutoRedirect = false;

            using (var http = new HttpClient(handler))
            {
                http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                string rawJson = "";
                try
                {
                    rawJson = await http.GetStringAsync("https://memegen.link/api/templates/").ConfigureAwait(false);                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(rawJson)
                                          .Select(kvp => Path.GetFileName(kvp.Value));

                await channel.SendTableAsync(data, x => $"{x,-17}", 3);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Memegen(string meme, string topText, string botText)
        {
            var channel = (SocketTextChannel)Context.Channel;

            var top = Uri.EscapeDataString(topText.Replace(' ', '-'));
            var bot = Uri.EscapeDataString(botText.Replace(' ', '-'));
            await channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg");
        }
    }
}
