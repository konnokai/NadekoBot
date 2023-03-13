#nullable disable
using NadekoBot.Db.Models;

namespace NadekoBot.Services.Database.Models;

public class GuildConfig : DbEntity
{
    public ulong GuildId { get; set; }

    public string Prefix { get; set; }

    public bool DeleteMessageOnCommand { get; set; }
    public HashSet<DelMsgOnCmdChannel> DelMsgOnCmdChannels { get; set; } = new();

    public string AutoAssignRoleIds { get; set; }

    //greet stuff
    public int AutoDeleteGreetMessagesTimer { get; set; } = 0;
    public int AutoDeleteByeMessagesTimer { get; set; } = 0;

    public ulong GreetMessageChannelId { get; set; }
    public ulong ByeMessageChannelId { get; set; }

    public bool SendDmGreetMessage { get; set; }
    public string DmGreetMessageText { get; set; } = "歡迎 %user.fullname% 來到 %server%";

    public bool SendChannelGreetMessage { get; set; }
    public string ChannelGreetMessageText { get; set; } = "歡迎 %user.fullname% 來到 %server%";

    public bool SendChannelByeMessage { get; set; }
    public string ChannelByeMessageText { get; set; } = "%user.fullname% 已離開";

    //self assignable roles
    public bool ExclusiveSelfAssignedRoles { get; set; }
    public bool AutoDeleteSelfAssignedRoleMessages { get; set; }

    //stream notifications
    public HashSet<FollowedStream> FollowedStreams { get; set; } = new();

    //currencyGeneration
    public HashSet<GCChannelId> GenerateCurrencyChannelIds { get; set; } = new();

    public List<Permissionv2> Permissions { get; set; }
    public bool VerbosePermissions { get; set; } = false;
    public string PermissionRole { get; set; }

    public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new();

    //filtering
    public bool FilterInvites { get; set; }
    public bool FilterLinks { get; set; }
    public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new();
    public HashSet<FilterLinksChannelId> FilterLinksChannelIds { get; set; } = new();

    //public bool FilterLinks { get; set; }
    //public HashSet<FilterLinksChannelId> FilterLinksChannels { get; set; } = new HashSet<FilterLinksChannelId>();

    public bool FilterWords { get; set; }
    public HashSet<FilteredWord> FilteredWords { get; set; } = new();
    public HashSet<FilterWordsChannelId> FilterWordsChannelIds { get; set; } = new();

    public HashSet<MutedUserId> MutedUsers { get; set; } = new();

    public string MuteRoleName { get; set; }
    public bool CleverbotEnabled { get; set; }

    public AntiRaidSetting AntiRaidSetting { get; set; }
    public AntiSpamSetting AntiSpamSetting { get; set; }
    public AntiAltSetting AntiAltSetting { get; set; }

    public string Locale { get; set; } = "zh-TW";
    public string TimeZoneId { get; set; } = "Asia/Taipei";

    public HashSet<UnmuteTimer> UnmuteTimers { get; set; } = new();
    public HashSet<UnbanTimer> UnbanTimer { get; set; } = new();
    public HashSet<UnroleTimer> UnroleTimer { get; set; } = new();
    public HashSet<VcRoleInfo> VcRoleInfos { get; set; }
    public HashSet<CommandAlias> CommandAliases { get; set; } = new();
    public List<WarningPunishment> WarnPunishments { get; set; } = new();
    public bool WarningsInitialized { get; set; }
    public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers { get; set; }
    public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles { get; set; }

    public List<ShopEntry> ShopEntries { get; set; }
    public ulong? GameVoiceChannel { get; set; }
    public bool VerboseErrors { get; set; } = true;

    public StreamRoleSettings StreamRole { get; set; }

    public XpSettings XpSettings { get; set; }
    public List<FeedSub> FeedSubs { get; set; } = new();
    public bool NotifyStreamOffline { get; set; }
    public bool DeleteStreamOnlineMessage { get; set; }
    public List<GroupName> SelfAssignableRoleGroupNames { get; set; }
    public int WarnExpireHours { get; set; }
    public WarnExpireAction WarnExpireAction { get; set; } = WarnExpireAction.Clear;

    public bool DisableGlobalExpressions { get; set; } = false;

    #region Boost Message

    public bool SendBoostMessage { get; set; }
    public string BoostMessage { get; set; } = "%user% 加成了伺服器!";
    public ulong BoostMessageChannelId { get; set; }
    public int BoostMessageDeleteAfter { get; set; }

    #endregion

    #region MuteReborn
    public bool EnableMuteReborn { get; set; } = false;
    public List<MuteRebornTicket> MuteRebornTickets { get; set; } = new();
    public int BuyMuteRebornTicketCost { get; set; } = 10000;
    public int EachTicketIncreaseMuteTime { get; set; } = 5;
    public int EachTicketDecreaseMuteTime { get; set; } = 30;
    public int MaxIncreaseMuteTime { get; set; } = 30;
    #endregion
}