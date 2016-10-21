﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        public class PlayingRotateCommands : ModuleBase
        {
            private static Logger _log { get; }
            public static List<PlayingStatus> RotatingStatusMessages { get; }
            public static bool RotatingStatuses { get; private set; } = false;

            static PlayingRotateCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.BotConfig.GetOrCreate();
                    RotatingStatusMessages = conf.RotatingStatusMessages;
                    RotatingStatuses = conf.RotatingStatuses;
                }

                _log = LogManager.GetCurrentClassLogger();

                var t = Task.Run(async () =>
                {
                    var index = 0;
                    do
                    {
                        try
                        {
                            if (!RotatingStatuses)
                                continue;
                            else
                            {
                                if (index >= RotatingStatusMessages.Count)
                                    index = 0;

                                if (!RotatingStatusMessages.Any())
                                    continue;
                                var status = RotatingStatusMessages[index++].Status;
                                if (string.IsNullOrWhiteSpace(status))
                                    continue;
                                PlayingPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value()));
                                await NadekoBot.Client.CurrentUser
                                        .ModifyStatusAsync(mpp => mpp.Game = new Discord.API.Game() { Name = status })
                                        .ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn("Rotating playing status errored.\n" + ex);
                        }
                        finally
                        {
                            await Task.Delay(15000);
                        }
                    } while (true);
                });
            }

            public static Dictionary<string, Func<string>> PlayingPlaceholders { get; } =
                new Dictionary<string, Func<string>> {
                    {"%servers%", () => NadekoBot.Client.Guilds.Count().ToString()},
                    {"%users%", () => NadekoBot.Client.Guilds.Select(s => s.Users.Count).Sum().ToString()},
                    {"%playing%", () => {
                            var cnt = Music.Music.MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                            if (cnt != 1) return cnt.ToString();
                            try {
                                var mp = Music.Music.MusicPlayers.FirstOrDefault();
                                return mp.Value.CurrentSong.SongInfo.Title;
                            }
                            catch {
                                return "No songs";
                            }
                        }
                    },
                    {"%queued%", () => Music.Music.MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count).ToString()}
                };

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                var channel = (SocketTextChannel)Context.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    RotatingStatuses = config.RotatingStatuses = !config.RotatingStatuses;
                    await uow.CompleteAsync();
                }
                if (RotatingStatuses)
                    await channel.SendMessageAsync("`Rotating playing status enabled.`");
                else
                    await channel.SendMessageAsync("`Rotating playing status disabled.`");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task AddPlaying([Remainder] string status)
            {
                var channel = (SocketTextChannel)Context.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    var toAdd = new PlayingStatus { Status = status };
                    config.RotatingStatusMessages.Add(toAdd);
                    RotatingStatusMessages.Add(toAdd);
                    await uow.CompleteAsync();
                }

                await channel.SendMessageAsync("`Added.`").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                var channel = (SocketTextChannel)Context.Channel;


                if (!RotatingStatusMessages.Any())
                    await channel.SendMessageAsync("`No rotating playing statuses set.`");
                else
                {
                    var i = 1;
                    await channel.SendMessageAsync($"{Context.Message.Author.Mention} `Here is a list of rotating statuses:`\n\n\t" + string.Join("\n\t", RotatingStatusMessages.Select(rs => $"`{i++}.` {rs.Status}")));
                }

            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                var channel = (SocketTextChannel)Context.Channel;
                index -= 1;

                string msg = "";
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    if (index >= config.RotatingStatusMessages.Count)
                        return;
                    msg = config.RotatingStatusMessages[index].Status;
                    config.RotatingStatusMessages.RemoveAt(index);
                    RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }
                await channel.SendMessageAsync($"{Context.Message.Author.Mention}`Removed the the playing message:` {msg}").ConfigureAwait(false);
            }
        }
    }
}