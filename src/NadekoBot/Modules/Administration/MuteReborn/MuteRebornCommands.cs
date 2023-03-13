using NadekoBot.Db;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Gambling.Services;
using MuteService = NadekoBot.Modules.Administration.Services.MuteService;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public class MuteRebornCommands : NadekoModule<MuteRebornService>
    {
        private readonly MuteService _muteService;
        private readonly DbService _db;
        private readonly GamblingConfigService _gss;
        private readonly ICurrencyService _cs;

        private string CurrencySign => _gss.Data.Currency.Sign;

        public MuteRebornCommands(MuteService MuteService, DbService db, GamblingConfigService gss, ICurrencyService cs)
        {
            _muteService = MuteService;
            _db = db;
            _gss = gss;
            _cs = cs;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task ToggleMuteReborn()
        {
            var result = _service.ToggleRebornStatus(Context.Guild);
            await Context.Channel.SendConfirmAsync(_eb, "死者蘇生已" + (result ? "開啟" : "關閉")).ConfigureAwait(false);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task SettingMuteReborn(MuteRebornService.SettingType type = MuteRebornService.SettingType.GetAllSetting, int value = 0)
        {
            using (var db = _db.GetDbContext())
            {
                var guild = db.GuildConfigsForId(Context.Guild.Id, set => set);

                switch (type)
                {
                    case MuteRebornService.SettingType.BuyMuteRebornTicketCost:
                        {
                            if (value == 0)
                            {
                                await Context.Channel.SendConfirmAsync(_eb, $"購買甦生券需花費: {guild.BuyMuteRebornTicketCost}{CurrencySign}");
                                return;
                            }

                            if (value < 1000 || value > 100000)
                            {
                                await Context.Channel.SendErrorAsync(_eb, "金額僅可限制在1000~100000內");
                                return;
                            }

                            guild.BuyMuteRebornTicketCost = value;
                            await Context.Channel.SendConfirmAsync(_eb, $"購買甦生券需花費: {guild.BuyMuteRebornTicketCost}{CurrencySign}");
                        }
                        break;
                    case MuteRebornService.SettingType.EachTicketIncreaseMuteTime:
                        {
                            if (value == 0)
                            {
                                await Context.Channel.SendConfirmAsync(_eb, $"每張甦生券可增加: {guild.EachTicketIncreaseMuteTime}分");
                                return;
                            }

                            if (value < 5 || value > 120)
                            {
                                await Context.Channel.SendErrorAsync(_eb, "時間僅可限制在5~120內");
                                return;
                            }

                            guild.EachTicketIncreaseMuteTime = value;
                            await Context.Channel.SendConfirmAsync(_eb, $"每張甦生券可增加: {guild.EachTicketIncreaseMuteTime}分" +
                                (guild.EachTicketIncreaseMuteTime > guild.MaxIncreaseMuteTime ? "\n請注意EachTicketIncreaseMuteTime數值比MaxIncreaseMuteTime大，將無法增加勞改時間" : ""));
                        }
                        break;
                    case MuteRebornService.SettingType.EachTicketDecreaseMuteTime:
                        {
                            if (value == 0)
                            {
                                await Context.Channel.SendConfirmAsync(_eb, $"每張甦生券可減少: {guild.EachTicketDecreaseMuteTime}分");
                                return;
                            }

                            if (value < 5 || value > 120)
                            {
                                await Context.Channel.SendErrorAsync(_eb, "時間僅可限制在5~120內");
                                return;
                            }

                            guild.EachTicketDecreaseMuteTime = value;
                            await Context.Channel.SendConfirmAsync(_eb, $"每張甦生券可減少: {guild.EachTicketDecreaseMuteTime}分");
                        }
                        break;
                    case MuteRebornService.SettingType.MaxIncreaseMuteTime:
                        {
                            if (value == 0)
                            {
                                await Context.Channel.SendConfirmAsync(_eb, $"最大可增加勞改時間: {guild.MaxIncreaseMuteTime}分");
                                return;
                            }

                            if (value < 10 || value > 360)
                            {
                                await Context.Channel.SendErrorAsync(_eb, "時間僅可限制在10~360內");
                                return;
                            }

                            guild.MaxIncreaseMuteTime = value;
                            await Context.Channel.SendConfirmAsync(_eb, $"最大可增加勞改時間: {guild.MaxIncreaseMuteTime}分" +
                                (guild.EachTicketIncreaseMuteTime > guild.MaxIncreaseMuteTime ? "\n請注意EachTicketIncreaseMuteTime數值比MaxIncreaseMuteTime大，將無法增加勞改時間" : ""));
                        }
                        break;
                    case MuteRebornService.SettingType.GetAllSetting:
                        {
                            await Context.Channel.SendConfirmAsync(_eb, $"購買甦生券需花費: {guild.BuyMuteRebornTicketCost}{CurrencySign}\n" +
                                $"每張甦生券可增加: {guild.EachTicketIncreaseMuteTime}分\n" +
                                $"每張甦生券可減少: {guild.EachTicketDecreaseMuteTime}分\n" +
                                $"最大可增加勞改時間: {guild.MaxIncreaseMuteTime}分" +
                                (guild.EachTicketIncreaseMuteTime > guild.MaxIncreaseMuteTime ? "\n請注意EachTicketIncreaseMuteTime數值比MaxIncreaseMuteTime大，將無法增加勞改時間" : ""));
                        }
                        break;
                }

                db.SaveChanges();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public async Task AddMuteRebornTicketNum(int num, IGuildUser user)
        {
            var result = await _service.AddRebornTicketNumAsync(Context.Guild, user, num).ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync(_eb, result.Item2).ConfigureAwait(false);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(1)]
        public async Task AddMuteRebornTicketNum(int num, [Remainder] string users)
        {
            var list = new List<string>(users.Replace("<@", "").Replace("!", "").Replace(">", "")
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct());
            var userList = new List<IGuildUser>();

            foreach (var item in list)
            {
                var user = await Context.Guild.GetUserAsync(ulong.Parse(item));
                if (user != null)
                    userList.Add(user);
            }

            var result = await _service.AddRebornTicketNumAsync(Context.Guild, userList, num).ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync(_eb, result).ConfigureAwait(false);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ListMuteRebornTicketNum(int page = 0)
        {
            var resultReborn = _service.ListRebornTicketNum(Context.Guild);

            if (resultReborn.Count == 0)
            {
                await Context.Channel.SendErrorAsync(_eb, "未設定過死者蘇生").ConfigureAwait(false);
                return;
            }

            var result = resultReborn.OrderByDescending((x) => x.RebornTicketNum).Select((x) => $"<@{x.UserId}>: {x.RebornTicketNum}");
            await Context.SendPaginatedConfirmAsync(page, (num) =>
            {
                return _eb.Create().WithOkColor()
                    .WithTitle("死者蘇生持有數")
                    .WithDescription(string.Join('\n', result.Skip(num * 15).Take(15)));
            }, result.Count(), 15);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task ShowMuteRebornTicketNum(IUser? user = null)
            => await ShowMuteRebornTicketNum(user == null ? Context.User.Id : user.Id);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task ShowMuteRebornTicketNum(ulong userId = 0)
        {
            if (userId == 0)
                userId = Context.User.Id;

            var resultReborn = _service.ListRebornTicketNum(Context.Guild);

            if (resultReborn.Count == 0)
            {
                await Context.Channel.SendErrorAsync(_eb, "未設定過死者蘇生").ConfigureAwait(false);
                return;
            }

            var muteReborn = resultReborn.FirstOrDefault((x) => x.UserId == userId);
            if (muteReborn == null)
            {
                await Context.Channel.SendConfirmAsync(_eb, $"<@{userId}> 的次數為: 0").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendConfirmAsync(_eb, $"<@{userId}> 的次數為: {muteReborn.RebornTicketNum}").ConfigureAwait(false);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task BuyMuteRebornTicket(int num = 1)
        {
            if (num <= 0)
            {
                await Context.Channel.SendErrorAsync(_eb, "購買數量需大於一張").ConfigureAwait(false);
                return;
            }

            using (var uow = _db.GetDbContext())
            {
                var currency = await _cs.GetBalanceAsync(Context.User.Id);
                var buyCost = uow.GuildConfigsForId(Context.Guild.Id, set => set).BuyMuteRebornTicketCost * num;

                if (currency < buyCost)
                {
                    await Context.Channel.SendErrorAsync(_eb, $"你的錢錢不夠，加油好嗎\n你還缺 {buyCost - currency}{CurrencySign} 才能購買").ConfigureAwait(false);
                    return;
                }
                                
                if (await _cs.RemoveAsync(Context.User.Id, buyCost, new("MuteRebornTicket", "Buy")))
                {
                    var result = await _service.AddRebornTicketNumAsync(Context.Guild, Context.User, num);
                    if (result.Item1)
                        await Context.Channel.SendConfirmAsync(_eb, result.Item2).ConfigureAwait(false);
                    else
                        await Context.Channel.SendErrorAsync(_eb, $"內部錯誤，已扣除金額但無法購買\n請向管理員要求直接增加次數: {num}").ConfigureAwait(false);

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SiNe(IGuildUser user)
        {
            if (!_service.GetRebornStatus(Context.Guild))
            {
                await Context.Channel.SendErrorAsync(_eb, "未設定過死者蘇生").ConfigureAwait(false);
                return;
            }

            if (!_service.CanReborn(Context.Guild, Context.User))
            {
                await Context.Channel.SendErrorAsync(_eb, "蘇生券不足阿🈹").ConfigureAwait(false);
                return;
            }

            if (!_service.MutingList.Add($"{Context.Guild.Id}-{user.Id}"))
            {
                await Context.Channel.SendErrorAsync(_eb, "正在勞改當中").ConfigureAwait(false);
                return;
            }

            var muteReborn = await _service.AddRebornTicketNumAsync(Context.Guild, (IGuildUser)Context.User, -1);
            if (muteReborn.Item1)
                await SiNeMute(TimeSpan.FromMinutes(_service.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.EachTicketIncreaseMuteTime)), user, muteReborn.Item2);
            else
                await Context.Channel.SendErrorAsync(_eb, muteReborn.Item2).ConfigureAwait(false);
        }

        private async Task SiNeMute(TimeSpan time, IGuildUser user, string str)
        {
            if (time < TimeSpan.FromMinutes(1) || time > TimeSpan.FromDays(1))
                return;

            try
            {
                try
                {
                    if (_service.GetRebornStatus(Context.Guild))
                    {
                        int guildIncreaseMuteTime = _service.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.EachTicketIncreaseMuteTime);
                        int guildMaxIncreaseMuteTime = _service.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.MaxIncreaseMuteTime);
                        if (guildIncreaseMuteTime > guildMaxIncreaseMuteTime)
                        {
                            await Context.Channel.SendConfirmAsync(_eb, $"{str}" +
                                $"因EachTicketIncreaseMuteTime({guildIncreaseMuteTime})設定數值比MaxIncreaseMuteTime({guildMaxIncreaseMuteTime})大\n" +
                                $"故無法增加勞改時間");
                        }
                        else
                        {
                            var dic = await Context.GetEmojiCountAsync(_eb, $"{str}30秒加時開始，勞改對象: {user.Mention}\n" +
                                $"每個表情可消耗一張蘇生券，來增加對方 {guildIncreaseMuteTime} 分鐘的勞改時間\n" +
                                $"最多可增加 {guildMaxIncreaseMuteTime} 分鐘").ConfigureAwait(false);

                            int addTime = 0;
                            string resultText = "";
                            Dictionary<ulong, int> dic2 = new Dictionary<ulong, int>();
                            foreach (var emoteList in dic)
                            {
                                foreach (var item in emoteList.Value)
                                {
                                    var userNum = _service.GetRebornTicketNum(Context.Guild, item);
                                    if (userNum <= 0)
                                        continue;

                                    if (dic2.ContainsKey(item) && userNum == dic2[item])
                                        continue;

                                    if (dic2.ContainsKey(item))
                                        dic2[item]++;
                                    else
                                        dic2.Add(item, 1);

                                    if (addTime + guildIncreaseMuteTime >= guildMaxIncreaseMuteTime)
                                    {
                                        addTime = guildMaxIncreaseMuteTime;
                                        break;
                                    }

                                    addTime += guildIncreaseMuteTime;
                                }

                                if (addTime >= guildMaxIncreaseMuteTime)
                                    break;
                            }

                            foreach (var item in dic2)
                            {
                                var addResult = await _service.AddRebornTicketNumAsync(Context.Guild, item.Key, -item.Value);
                                if (addResult.Item1)
                                    resultText += addResult.Item2;
                                else
                                    addTime -= guildIncreaseMuteTime;
                            }

                            if (addTime > 0)
                            {
                                time += TimeSpan.FromMinutes(addTime);
                                await Context.Channel.SendConfirmAsync(_eb, resultText + $"總共被加了 {addTime} 分鐘").ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"AddMuteTIme: {ctx.Guild.Name}({ctx.Guild.Id}) / {user.Username}({user.Id})");
                    await Context.Channel.SendConfirmAsync(_eb, "錯誤，請向 <@284989733229297664>(孤之界#1121) 詢問");
                }

                if (user.Id == 284989733229297664)
                {
                    _service.MutingList.Remove($"{Context.Guild.Id}-{user.Id}");
                    user = (IGuildUser)ctx.User;
                    await ctx.Channel.SendMessageAsync(embed: _eb.Create().WithOkColor().WithImageUrl("https://konnokai.me/nadeko/potter.png").Build());
                }

                await _muteService.TimedMute(user, ctx.User, time, MuteType.Chat, "主動勞改").ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync(strs.user_chat_mute_time(Format.Bold(user.ToString()), (int)time.TotalMinutes)).ConfigureAwait(false);

                try
                {
                    if (_service.CanReborn(Context.Guild, user))
                    {
                        if (_muteService.UnTimers.TryGetValue(Context.Guild.Id, out var keyValuePairs) && keyValuePairs.TryGetValue((user.Id, MuteService.TimerType.Mute), out var timer))
                        {
                            await Context.SendYesNoConfirmAsync(_eb.Create(), $"{Format.Bold(user.ToString())} 剩餘 {_service.GetRebornTicketNum(Context.Guild, user.Id)} 張甦生券，要使用嗎", async (result) =>
                            {
                                if (result)
                                {
                                    int guildDecreaseMuteTime = _service.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.EachTicketDecreaseMuteTime);
                                    var temp = time.Add(TimeSpan.FromMinutes(-guildDecreaseMuteTime)).Subtract(timer.Item2.Elapsed);
                                    string resultText = "";
                                    if (temp > TimeSpan.FromSeconds(30))
                                    {
                                        await _muteService.TimedMute(user, ctx.User, temp, reason: $"死者蘇生扣除 {guildDecreaseMuteTime} 分鐘").ConfigureAwait(false);
                                        resultText = $"已扣除 {guildDecreaseMuteTime} 分鐘\n你還需要勞改 {temp:hh\\時mm\\分ss\\秒}\n";
                                    }
                                    else
                                    {
                                        await _muteService.UnmuteUser(ctx.Guild.Id, user.Id, ctx.User, reason: "死者蘇生");
                                        resultText = "歡迎回來\n";
                                    }

                                    resultText += (await _service.AddRebornTicketNumAsync(ctx.Guild, user, -1).ConfigureAwait(false)).Item2;
                                    await Context.Channel.SendConfirmAsync(_eb, resultText).ConfigureAwait(false);
                                }
                                else
                                {
                                    await Context.Channel.SendConfirmAsync(_eb, "好ㄅ，勞改愉快").ConfigureAwait(false);
                                }
                            }, user, false).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"SiNeMuteReborn: {ctx.Guild.Name}({ctx.Guild.Id}) / {user.Username}({user.Id})");
                    await Context.Channel.SendConfirmAsync(_eb, "錯誤，請向 <@284989733229297664>(孤之界#1121) 詢問");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "");
                await ReplyErrorLocalizedAsync(strs.mute_error).ConfigureAwait(false);
            }

            _service.MutingList.Remove($"{Context.Guild.Id}-{user.Id}");
        }
    }
}