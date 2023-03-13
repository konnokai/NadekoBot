#nullable disable

using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Common.Umamusume;
using NadekoBot.Modules.Gambling.Services;
using System.Text.Json;

namespace NadekoBot.Modules.Gambling;
public partial class Gambling : GamblingModule<GamblingService>
{
    [Group]
    public partial class UmamusumeGamblingCommands : NadekoModule<UmamusumeGamblingService>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MuteRebornService _muteRebornService;

        public UmamusumeGamblingCommands(DiscordSocketClient client, IHttpClientFactory httpClientFactory, MuteRebornService muteRebornService)
        {
            _httpClientFactory = httpClientFactory;
            _muteRebornService = muteRebornService;
           
            client.ButtonExecuted += async (component) =>
            {
                if (component.HasResponded)
                    return;

                if (component.Data.CustomId.StartsWith("uma_"))
                {
                    string guid = component.Data.CustomId.Split('_')[1];
                    var umaGamblingData = _service.runningUmaGambling.FirstOrDefault((x) => x.UmaGuid.EndsWith(guid));
                    if (umaGamblingData != null && umaGamblingData.GamblingMessage.CreatedAt.AddMinutes(5) >= DateTimeOffset.UtcNow)
                    {
                        if (component.User.Id == umaGamblingData.GamblingUser.Id)
                        {
                            await component.RespondAsync($"你不可選擇自己的賭局", ephemeral: true);
                            return;
                        }

                        string rank = component.Data.CustomId.Split('_')[2] == "first" ? "1" : "2";
                        if (umaGamblingData.SelectedRankDic.ContainsKey(component.User.Id))
                        {
                            umaGamblingData.SelectedRankDic[component.User.Id] = rank;
                            await component.RespondAsync($"更改選擇: 第{rank}名", ephemeral: true);
                        }
                        else
                        {
                            umaGamblingData.SelectedRankDic.Add(component.User.Id, rank);
                            await component.RespondAsync($"選擇: 第{rank}名", ephemeral: true);
                        }

                        await umaGamblingData.SelectRankMessage.ModifyAsync((act) => 
                            act.Embed = _eb.Create()
                                .WithColor(EmbedColor.Ok)
                                .WithTitle("投票選擇清單")
                                .WithDescription(string.Join('\n', umaGamblingData.SelectedRankDic.Select((x) => $"<@{x.Key}>: 第{x.Value}名")))
                                .Build()
                            );
                    }
                    else
                    {
                        await component.RespondAsync($"該賭局已結束或取消", ephemeral: true);
                        await DisableButtonAsync(component.Message);
                    }
                }
                else if (component.Data.CustomId.StartsWith("umaend_"))
                {
                    if (component.User is SocketGuildUser)
                    {
                        var guild = ((SocketGuildChannel)component.Channel).Guild;
                        var user = component.User as SocketGuildUser;
                        if (user.GetRoles().Any((x) => x.Permissions.Administrator) || guild.OwnerId == user.Id)
                        {
                            var jsonFile = component.Message.Attachments.FirstOrDefault((x) => x.Filename == "rank.json");
                            if (jsonFile == null)
                            {
                                await component.RespondAsync("缺少 `rank.json` 檔案", ephemeral: true);
                                await DisableButtonAsync(component.Message);
                                return;
                            }

                            await component.DeferAsync(false);

                            using var httpClient = _httpClientFactory.CreateClient();
                            var jsonText = await httpClient.GetStringAsync(jsonFile.Url);
                            var json = JsonSerializer.Deserialize<Dictionary<ulong, string>>(jsonText);

                            string rank = component.Data.CustomId.Split('_')[1] == "first" ? "1" : "2";
                            string result = "";
                            foreach (var item in json)
                            {
                                var addResult = await _muteRebornService.AddRebornTicketNumAsync(guild, item.Key, item.Value == rank || item.Value == "0" ? 3 : -1);
                                result += addResult.Item2;

                                if (!addResult.Item1)
                                    break;
                            }

                            await component.FollowupAsync(embed: _eb.Create().WithOkColor().WithDescription(result).Build());
                            await DisableButtonAsync(component.Message);
                        }
                        else
                        {
                            await component.RespondAsync("你無權使用本功能", ephemeral: true);
                            return;
                        }
                    }
                }
            };
        }

        [Cmd]
#if DEBUG
        [RequireGuild(506083124015398932)]
#elif RELEASE
        [RequireGuild(738734668882640938)]
#endif
        public async Task Uma([Leftover] string text = "")
        {
            if (!ctx.Message.Attachments.Any())
            {
                await Context.Channel.SendErrorAsync(_eb, "需要包含圖片");
                return;
            }

            bool isCancel = false;
            var umaGamblingData = _service.runningUmaGambling.FirstOrDefault((x) => x.GamblingUser.Id == ctx.User.Id);
            if (umaGamblingData != null)
            {
                await Context.SendYesNoConfirmAsync(_eb.Create(), "你有賭局尚未結束，是否取消?", (act) =>
                {
                    if (act)
                    {
                        _service.runningUmaGambling.Remove(umaGamblingData);
                    }
                    else
                    {
                        Context.Channel.SendErrorAsync(_eb, $"{ctx.User} 請上傳比賽排名截圖並同時輸入 `~umaend` 以結束該賭局");
                        isCancel = true;
                    }
                });
            }

            if (isCancel)
                return;

            string umaGuid = "uma_" + Guid.NewGuid().ToString().Replace("-", "");

            var message = await ctx.Channel.SendMessageAsync("<@&830033656159666178> 開賭啦",
                   embed: _eb.Create(ctx)
                       .WithOkColor()
                       .WithTitle(ctx.User.ToString())
                       .WithDescription("附加訊息: " + (string.IsNullOrEmpty(text) ? "無" : text))
                       .WithImageUrl(ctx.Message.Attachments.First().Url)
                       .WithFooter("請在比賽結束後截圖排名，上傳截圖同時輸入 `~umaend` 以供管理員檢查")
                       .Build(),
                   components: new ComponentBuilder()
                       .WithButton("第一名", $"{umaGuid}_first", ButtonStyle.Success)
                       .WithButton("不是第一名", $"{umaGuid}_other", ButtonStyle.Danger)
                       .Build());

            var message2 = await message.ReplyAsync(
                 embed: _eb.Create(ctx)
                     .WithOkColor()
                     .WithTitle("投票選擇清單")
                     .WithDescription("無")
                     .Build());

            _service.runningUmaGambling.Add(new UmaGamblingData(ctx.User, message, message2, text, umaGuid));
        }


        [Cmd]
#if DEBUG
        [RequireGuild(506083124015398932)]
#elif RELEASE
        [RequireGuild(738734668882640938)]
#endif
        public async Task Umaend()
        {
            var umaGamblingData = _service.runningUmaGambling.FirstOrDefault((x) => x.GamblingUser.Id == ctx.User.Id);
            if (umaGamblingData == null)
            {
                await Context.Channel.SendErrorAsync(_eb, $"{ctx.User} 未開始賭局，請使用 `~uma` 新增賭局");
                return;
            }

            bool isCancel = false;
            if (!umaGamblingData.SelectedRankDic.Any())
            {
                await Context.SendYesNoConfirmAsync(_eb.Create(), $"{ctx.User} 你的賭局尚無人選則排名，是否取消本賭局?", async (act) =>
                {
                    if (act)
                    {
                        await DisableButtonAsync(umaGamblingData.GamblingMessage);
                        _service.runningUmaGambling.Remove(umaGamblingData);
                    }
                    isCancel = true;
                });
            }

            if (isCancel)
                return;

            if (!ctx.Message.Attachments.Any())
            {
                await Context.Channel.SendErrorAsync(_eb, "需要包含比賽後排名的截圖");
                return;
            }

#if DEBUG
            var channel = ctx.Channel;
#elif RELEASE
            var channel = await ctx.Guild.GetTextChannelAsync(961319987522445392);
#endif
            using var httpClient = _httpClientFactory.CreateClient();

            var bytes = await httpClient.GetByteArrayAsync(ctx.Message.Attachments.First().Url);
            using var imageStream = new MemoryStream(bytes);
            umaGamblingData.SelectedRankDic.Add(umaGamblingData.GamblingUser.Id, "0");
            using var stringStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(umaGamblingData.SelectedRankDic)));

            await channel.SendFilesAsync(
                attachments: new List<FileAttachment>() { new FileAttachment(imageStream, "rank.jpg"), new FileAttachment(stringStream, "rank.json") },
                embed: _eb.Create(ctx)
                    .WithOkColor()
                    .WithTitle(umaGamblingData.GamblingUser.ToString() + " 的賭局排名選擇清單")
                    .WithDescription($"附加訊息: {umaGamblingData.AddMessage}\n\n" +
                        Format.Url($"賭局連結", umaGamblingData.GamblingMessage.GetJumpUrl()))
                    .Build(),
                components: new ComponentBuilder()
                    .WithButton("第一名", $"umaend_first", ButtonStyle.Success)
                    .WithButton("不是第一名", $"umaend_other", ButtonStyle.Danger)
                    .Build());

            await DisableButtonAsync(umaGamblingData.GamblingMessage);

            _service.runningUmaGambling.Remove(umaGamblingData);

            await umaGamblingData.GamblingMessage.ReplyAsync(embed: _eb.Create().WithOkColor().WithDescription("已結束並封存本賭局").Build());
        }

        private async Task DisableButtonAsync(IUserMessage userMessage)
        {
            await userMessage.ModifyAsync((act) =>
            {
                act.Components = new Optional<MessageComponent>(new ComponentBuilder()
                    .WithButton("此賭局已結束", $"end", ButtonStyle.Primary, disabled: true)
                    .Build());
            });
        }
    }
}