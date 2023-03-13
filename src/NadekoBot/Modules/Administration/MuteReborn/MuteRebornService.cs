using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;
public class MuteRebornService : INService
{
    public enum SettingType { BuyMuteRebornTicketCost, EachTicketIncreaseMuteTime, EachTicketDecreaseMuteTime, MaxIncreaseMuteTime, GetAllSetting }
    public HashSet<string> MutingList = new();

    private readonly DbService _db;

    public MuteRebornService(DbService db)
    {
        _db = db;
    }

    public bool ToggleRebornStatus(IGuild guild)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include((x) => x.MuteRebornTickets));

                if (guildConfig == null)
                    return false;

                guildConfig.EnableMuteReborn = !guildConfig.EnableMuteReborn;
                uow.GuildConfigs.Update(guildConfig);
                uow.SaveChanges();

                return guildConfig.EnableMuteReborn;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"ToggleRebornStatus: {guild.Name}({guild.Id})");
            return false;
        }
    }

    public bool GetRebornStatus(IGuild guild)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include((x) => x.MuteRebornTickets));

                if (guildConfig == null)
                    return false;

                return guildConfig.EnableMuteReborn;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"GetRebornStatus: {guild.Name}({guild.Id})");
            return false;
        }
    }

    public int GetRebornSetting(IGuild guild, SettingType type)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set=>set);

                if (guildConfig == null)
                    throw new NullReferenceException();

                switch (type)
                {
                    case SettingType.BuyMuteRebornTicketCost:
                        return guildConfig.BuyMuteRebornTicketCost;
                    case SettingType.EachTicketIncreaseMuteTime:
                        return guildConfig.EachTicketIncreaseMuteTime;
                    case SettingType.EachTicketDecreaseMuteTime:
                        return guildConfig.EachTicketDecreaseMuteTime;
                    case SettingType.MaxIncreaseMuteTime:
                        return guildConfig.MaxIncreaseMuteTime;
                }
            }

            throw new NullReferenceException();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"GetRebornSetting: {guild.Name}({guild.Id})");
            throw;
        }
    }

    public int GetRebornTicketNum(IGuild guild, ulong userId)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include((x) => x.MuteRebornTickets));

                if (guildConfig == null)
                    return 0;

                var muteReborn = guildConfig.MuteRebornTickets.FirstOrDefault((x) => x.UserId == userId);
                if (muteReborn == null)
                    return 0;

                return muteReborn.RebornTicketNum;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"GetRebornTicketNum: {guild.Name}({guild.Id})");
            return 0;
        }
    }

    public bool CanReborn(IGuild guild, IUser user)
    {
        using (var uow = _db.GetDbContext())
        {
            var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.MuteRebornTickets));
            if (guildConfig == null)
                return false;
            if (!guildConfig.EnableMuteReborn)
                return false;

            var muteReborn = guildConfig.MuteRebornTickets.FirstOrDefault((x) => x.UserId == user.Id);
            if (muteReborn == null)
                return false;

            if (muteReborn.RebornTicketNum > 0)
                return true;

            return false;
        }
    }

    public async Task<(bool, string)> AddRebornTicketNumAsync(IGuild guild, IUser user, int num)
    => await AddRebornTicketNumAsync(guild, user.Id, num);

    public async Task<(bool, string)> AddRebornTicketNumAsync(IGuild guild, ulong user, int num)
    {
        try
        {
            int addNum = num;

            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.MuteRebornTickets));

                if (guildConfig == null)
                    return (false, "伺服器不在資料庫內");

                if (!guildConfig.EnableMuteReborn)
                    return (false, "死者蘇生未開啟");

                var muteReborn = guildConfig.MuteRebornTickets.FirstOrDefault((x) => x.UserId == user);
                if (muteReborn == null)
                {
                    guildConfig.MuteRebornTickets.Add(new MuteRebornTicket() { UserId = user, RebornTicketNum = num });
                }
                else
                {
                    num += muteReborn.RebornTicketNum;
                    muteReborn.RebornTicketNum = num;
                    uow.GuildConfigs.Update(guildConfig);
                }

                await uow.SaveChangesAsync().ConfigureAwait(false);

                return (true, $"<@{user}> 增加**{addNum}**，剩餘**{num}**次蘇生機會\n");

            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"AddRebornTicketNumAsyncSingel2: {guild.Name}({guild.Id})");
            return (false, "錯誤，請向 <@284989733229297664>(孤之界#1121) 詢問");
        }
    }

    public async Task<string> AddRebornTicketNumAsync(IGuild guild, List<IGuildUser> users, int num)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.MuteRebornTickets));

                if (guildConfig == null)
                    return "伺服器不在資料庫內";

                if (!guildConfig.EnableMuteReborn)
                    return "死者蘇生未開啟";

                string result = "";
                foreach (var user in users)
                {
                    int tempNum = num;
                    var muteReborn = guildConfig.MuteRebornTickets.FirstOrDefault((x) => x.UserId == user.Id);
                    if (muteReborn == null)
                        guildConfig.MuteRebornTickets.Add(new MuteRebornTicket() { UserId = user.Id, RebornTicketNum = tempNum });
                    else
                    {
                        tempNum += muteReborn.RebornTicketNum;
                        muteReborn.RebornTicketNum = tempNum;
                        uow.GuildConfigs.Update(guildConfig);
                    }

                    await uow.SaveChangesAsync().ConfigureAwait(false);

                    result += $"<@{user.Id}> 增加**{num}**，剩餘**{tempNum}**次蘇生機會\n";
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"AddRebornTicketNumAsyncList: {guild.Name}({guild.Id})");
            return $"錯誤，請向 <@284989733229297664>(孤之界#1121) 詢問";
        }
    }

    public List<MuteRebornTicket> ListRebornTicketNum(IGuild guild)
    {
        try
        {
            using (var uow = _db.GetDbContext())
            {
                var guildConfig = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.MuteRebornTickets));

                if (guildConfig == null)
                    return new List<MuteRebornTicket>();

                return guildConfig.MuteRebornTickets;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"ListRebornTicketNum: {guild.Name}({guild.Id})");
            return new List<MuteRebornTicket>();
        }
    }
}