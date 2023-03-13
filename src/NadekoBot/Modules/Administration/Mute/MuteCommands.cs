#nullable disable
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class MuteCommands : NadekoModule<MuteService>
    {
        private readonly MuteRebornService _muteRebornService;
        public MuteCommands(MuteRebornService muteRebornService)
        {
            _muteRebornService = muteRebornService;
        }

        private async Task<bool> VerifyMutePermissions(IGuildUser runnerUser, IGuildUser targetUser)
        {
            if (runnerUser.Id == 284989733229297664)
                return true;

            var runnerUserRoles = runnerUser.GetRoles();
            var targetUserRoles = targetUser.GetRoles();
            if (runnerUser.Id != ctx.Guild.OwnerId
                && runnerUserRoles.Max(x => x.Position) <= targetUserRoles.Max(x => x.Position))
            {
                await ReplyErrorLocalizedAsync(strs.mute_perms);
                return false;
            }

            return true;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task MuteRole([Leftover] IRole role = null)
        {
            if (role is null)
            {
                var muteRole = await _service.GetMuteRole(ctx.Guild);
                await ReplyConfirmLocalizedAsync(strs.mute_role(Format.Code(muteRole.Name)));
                return;
            }

            if (ctx.User.Id != ctx.Guild.OwnerId
                && role.Position >= ((SocketGuildUser)ctx.User).Roles.Max(x => x.Position))
            {
                await ReplyErrorLocalizedAsync(strs.insuf_perms_u);
                return;
            }

            await _service.SetMuteRoleAsync(ctx.Guild.Id, role.Name);

            await ReplyConfirmLocalizedAsync(strs.mute_role_set);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        [Priority(0)]
        public async Task Mute(IGuildUser target, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, target))
                    return;

                await _service.MuteUser(target, ctx.User, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_muted(Format.Bold(target.ToString())));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in the mute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        [Priority(0)]
        public async Task Mute(StoopidTime time, [Leftover] string user)
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(1))
                return;

            user = user.Replace("<", "").Replace("@", "").Replace("!", "").Replace(">", "");
            var list = user.Trim().Split(new char[] { ' ' });

            if (list.Length == 1)
            {
                IGuildUser target = await Context.Guild.GetUserAsync(ulong.Parse(list[0]));
                await Mute(time, target);
            }
            else
            {
                foreach (var item in list)
                {
                    IGuildUser target = await Context.Guild.GetUserAsync(ulong.Parse(item));
                    try
                    {
                        if (!await VerifyMutePermissions((IGuildUser)ctx.User, target))
                            return;

                        await _service.TimedMute(target, ctx.User, time.Time).ConfigureAwait(false);
                        await ReplyConfirmLocalizedAsync(strs.user_muted_time(Format.Bold(target.ToString()), (int)time.Time.TotalMinutes)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        await ReplyErrorLocalizedAsync(strs.mute_error).ConfigureAwait(false);
                    }
                }
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task HardMute(StoopidTime time, [Leftover] string user)
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(1))
                return;

            user = user.Replace("<", "").Replace("@", "").Replace("!", "").Replace(">", "");
            var list = user.Trim().Split(new char[] { ' ' });

            foreach (var item in list)
            {
                IGuildUser target = await Context.Guild.GetUserAsync(ulong.Parse(item));
                if (target == null)
                    continue;

                if (target.Id == 284989733229297664)
                { 
                    await ctx.Channel.SendErrorAsync(_eb, "?");
                    continue; 
                }

                try
                {
                    await _service.TimedHardMute(target, ctx.User, time.Time);
                    await ReplyConfirmLocalizedAsync(strs.user_hard_muted_time(Format.Bold(target.ToString()), (int)time.Time.TotalMinutes)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await ReplyErrorLocalizedAsync(strs.mute_error).ConfigureAwait(false);
                }
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        [Priority(1)]
        public async Task Mute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                if (!_muteRebornService.MutingList.Add($"{Context.Guild.Id}-{user.Id}"))
                {
                    await Context.Channel.SendErrorAsync(_eb, "正在勞改當中").ConfigureAwait(false);
                    return;
                }

                try
                {
                    if (_muteRebornService.GetRebornStatus(Context.Guild))
                    {
                        int guildIncreaseMuteTime = _muteRebornService.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.EachTicketIncreaseMuteTime);
                        int guildMaxIncreaseMuteTime = _muteRebornService.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.MaxIncreaseMuteTime);
                        if (guildIncreaseMuteTime > guildMaxIncreaseMuteTime)
                        {
                            await Context.Channel.SendErrorAsync(_eb, $"因EachTicketIncreaseMuteTime({guildIncreaseMuteTime})設定數值比MaxIncreaseMuteTime({guildMaxIncreaseMuteTime})大\n" +
                                $"故無法增加勞改時間");
                        }
                        else
                        {
                            var dic = await Context.GetEmojiCountAsync(_eb, $"30秒加時開始，勞改對象: {user.Mention}\n" +
                                $"每個表情可消耗一張蘇生券，來增加對方 {guildIncreaseMuteTime} 分鐘的勞改時間\n" +
                                $"最多可增加 {guildMaxIncreaseMuteTime} 分鐘").ConfigureAwait(false);

                            int addTime = 0;
                            string resultText = "";
                            Dictionary<ulong, int> dic2 = new Dictionary<ulong, int>();
                            foreach (var emoteList in dic)
                            {
                                foreach (var item in emoteList.Value)
                                {
                                    var userNum = _muteRebornService.GetRebornTicketNum(Context.Guild, item);
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
                                var addResult = await _muteRebornService.AddRebornTicketNumAsync(Context.Guild, item.Key, -item.Value);
                                if (addResult.Item1)
                                    resultText += addResult.Item2;
                                else
                                    addTime -= guildIncreaseMuteTime;
                            }
                            if (addTime > 0)
                            {
                                time.Time += TimeSpan.FromMinutes(addTime);
                                await Context.Channel.SendConfirmAsync(_eb, resultText + $"總共被加了 {addTime} 分鐘").ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"AddMuteTIme: {ctx.Guild.Name}({ctx.Guild.Id}) / {user.Username}({user.Id})");

                    await Context.Channel.SendErrorAsync(_eb, "錯誤，請向 <@284989733229297664>(孤之界#1121) 詢問");
                }

                await _service.TimedMute(user, ctx.User, time.Time, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_muted_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));

                try
                {
                    if (_service.CanReborn(Context.Guild, user))
                    {
                        if (_service.UnTimers.TryGetValue(Context.Guild.Id, out var keyValuePairs) && keyValuePairs.TryGetValue((user.Id, MuteService.TimerType.Mute), out var timer))
                        {
                            await Context.SendYesNoConfirmAsync(_eb.Create(), $"{Format.Bold(user.ToString())} 剩餘 {_muteRebornService.GetRebornTicketNum(Context.Guild, user.Id)} 張甦生券，要使用嗎?", async (result) =>
                            {
                                if (result)
                                {
                                    int guildDecreaseMuteTime = _muteRebornService.GetRebornSetting(Context.Guild, MuteRebornService.SettingType.EachTicketDecreaseMuteTime);
                                    var temp = time.Time.Add(TimeSpan.FromMinutes(-guildDecreaseMuteTime)).Subtract(timer.Item2.Elapsed);
                                    string resultText = "";
                                    if (temp > TimeSpan.FromSeconds(30))
                                    {
                                        await _service.TimedMute(user, ctx.User, temp, reason: $"死者蘇生扣除 {guildDecreaseMuteTime} 分鐘").ConfigureAwait(false);
                                        resultText = $"已扣除 {guildDecreaseMuteTime} 分鐘\n你還需要勞改 {temp:hh\\時mm\\分ss\\秒}\n";
                                    }
                                    else
                                    {
                                        await _service.UnmuteUser(ctx.Guild.Id, user.Id, ctx.User, reason: "死者蘇生");
                                        resultText = "歡迎回來\n";
                                    }

                                    resultText += (await _muteRebornService.AddRebornTicketNumAsync(ctx.Guild, user, -1).ConfigureAwait(false)).Item2;
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
                Log.Warning(ex, "Error in mute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }

            _muteRebornService.MutingList.Remove($"{Context.Guild.Id}-{user.Id}");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        public async Task Unmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_unmuted(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Unhardmute(IGuildUser user)
        {
            try
            {
                await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, type: MuteType.HardChat).ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync(strs.user_unmuted(Format.Bold(user.ToString()))).ConfigureAwait(false);
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error).ConfigureAwait(false);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public async Task ChatMute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.MuteUser(user, ctx.User, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_mute(Format.Bold(user.ToString())));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in the chatmute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [Priority(1)]
        public async Task ChatMute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.TimedMute(user, ctx.User, time.Time, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_mute_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in chatmute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task ChatUnmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.Guild.Id, user.Id, ctx.User, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_unmute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        [Priority(0)]
        public async Task VoiceMute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.MuteUser(user, ctx.User, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_mute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        [Priority(1)]
        public async Task VoiceMute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.TimedMute(user, ctx.User, time.Time, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_mute_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        public async Task VoiceUnmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_unmute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }
    }
}