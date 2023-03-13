#pragma warning disable CS8602 // 可能 null 參考的取值 (dereference)。
#pragma warning disable CS8605 // Unboxing 可能 null 值。
#pragma warning disable CS8600 // 正在將 Null 常值或可能的 Null 值轉換為不可為 Null 的型別。

using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Administration.Services;

public class CharacterDesignService : INService
{
    private DiscordSocketClient _client;
    private readonly ImagesConfig _ic;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IImageCache _images;
    private readonly DbService _db;


    public CharacterDesignService(DiscordSocketClient client, ImagesConfig ic, IHttpClientFactory factory, IImageCache images, DbService db)
    {
        _client = client;
        _ic = ic;
        _httpFactory = factory;
        _images = images;
        _db = db;
    }

    public async Task<(bool, CharacterDesign?)> AddCharDesignAsync(string designName, string charAvatar, string playingList)
    {
        if (File.Exists(designName.ToDesignPath() + "design.json"))
            return (false, null);
        if (!Directory.Exists(designName.ToDesignPath()))
            Directory.CreateDirectory(designName.ToDesignPath());

        try
        {
            using (var http = _httpFactory.CreateClient())
            using (var sr = await http.GetAsync(charAvatar, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                if (!sr.IsImage())
                    return (false, null);

                var imgData = await sr.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                await File.WriteAllBytesAsync(designName.ToDesignPath() + "avatar.png", imgData).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "");
            return (false, null);
        }

        try
        {
            var xpByte = await _images.GetXpBackgroundImageAsync();
            if (xpByte != null)
                await File.WriteAllBytesAsync(designName.ToDesignPath() + "xp_bg.png", xpByte).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "");
            return (false, null);
        }

        var @char = new CharacterDesign();
        var list = playingList.Split(new char[] { '|' });
        foreach (string item in list)
        {
            var temp = item.Split(new char[] { ':' });
            if (Enum.TryParse(typeof(ActivityType), temp[0], true, out object type))
                @char.PlayingList.Add(new RotatingPlayingStatus() { Type = (ActivityType)type, Status = temp[1] });
        }

        try
        {
            await File.WriteAllTextAsync(designName.ToDesignPath() + "design.json", JsonConvert.SerializeObject(@char)).ConfigureAwait(false);
            return (true, @char);
        }
        catch
        {
            return (false, null);
        }
    }

    public (bool, CharacterDesign?) AddCharDesignPlayingStatus(string designName, string playingList)
    {
        if (!File.Exists(designName.ToDesignPath() + "design.json"))
            return (false, null);

        CharacterDesign? @char = JsonConvert.DeserializeObject<CharacterDesign>(File.ReadAllText(designName.ToDesignPath() + "design.json"));

        var list = playingList.Split(new char[] { '|' });
        foreach (string item in list)
        {
            var playingStatus = item.Split(new char[] { ':' });
            if (Enum.TryParse(typeof(ActivityType), playingStatus[0], true, out object? type))
                @char.PlayingList.Add(new RotatingPlayingStatus() { Type = (ActivityType)type, Status = playingStatus[1] });
        }

        try
        {
            File.WriteAllText(designName.ToDesignPath() + "design.json", JsonConvert.SerializeObject(@char));
            return (true, @char);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<bool> ChangeCharDesignAsync(string designName)
    {
        if (!File.Exists(designName.ToDesignPath() + "design.json") || !File.Exists(designName.ToDesignPath() + "avatar.png"))
            return false;
        if (_client.CurrentUser.Username == designName)
            return false;

        try
        {
            await _client.CurrentUser.ModifyAsync((x) => x.Username = designName).ConfigureAwait(false);
            await _client.CurrentUser.ModifyAsync((x) => x.Avatar = new Image(designName.ToDesignPath() + "avatar.png")).ConfigureAwait(false);

            if (File.Exists($"{designName.ToDesignPath()}xp_bg.png"))
                _ic.ModifyConfig((act) => act.Xp.Bg = new Uri($"file://{AppDomain.CurrentDomain.BaseDirectory}{designName.ToDesignPath()}xp_bg.png"));

            CharacterDesign? characterDesign = JsonConvert.DeserializeObject<CharacterDesign>(await File.ReadAllTextAsync(designName.ToDesignPath() + "design.json"));
            if (characterDesign != null && characterDesign.PlayingList.Count > 0)
            {
                using (var uow = _db.GetDbContext())
                {
                    var config = uow.RotatingStatus;
                    config.RemoveRange(config.ToArray());

                    foreach (var item in characterDesign.PlayingList.Select((x) => (x.Type, x.Status)))
                        config.Add(new RotatingPlayingStatus() { Type = item.Type, Status = item.Status });
                    await uow.SaveChangesAsync();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "");
            return false;
        }
    }

    public string[]? ListCharDesign()
    {
        if (!Directory.Exists("data/char_design"))
            return null;
        return Directory.GetDirectories("data/char_design", "*", SearchOption.TopDirectoryOnly).Select((x) => Path.GetFileName(x)).OrderBy((x) => x).ToArray();
    }

    public async Task<(bool, CharacterDesign?)> SaveCharDesignAsync(string designName)
    {
        //if (File.Exists(designName.ToDesignPath() + "design.json")) return (false, null);
        if (!Directory.Exists(designName.ToDesignPath()))
            Directory.CreateDirectory(designName.ToDesignPath());

        try
        {
            using (var http = _httpFactory.CreateClient())
            using (var sr = await http.GetAsync(_client.CurrentUser.GetAvatarUrl(), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                if (!sr.IsImage())
                    return (false, null);

                var imgData = await sr.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                await File.WriteAllBytesAsync(designName.ToDesignPath() + "avatar.png", imgData).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "");
            return (false, null);
        }

        try
        {
            var xpByte = await _images.GetXpBackgroundImageAsync();
            if (xpByte != null)
                await File.WriteAllBytesAsync(designName.ToDesignPath() + "xp_bg.png", xpByte).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "");
            return (false, null);
        }

        try
        {
            using (var uow = _db.GetDbContext())
            {
                var @char = new CharacterDesign() { PlayingList = uow.RotatingStatus.ToList() };
                await File.WriteAllTextAsync(designName.ToDesignPath() + "design.json", JsonConvert.SerializeObject(@char)).ConfigureAwait(false);
                return (true, @char);
            }
        }
        catch
        {
            return (false, null);
        }
    }

    public bool DeleteCharDesign(string designName)
    {
        try
        {
            Directory.Delete($"data/char_design/{designName}", true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public class CharacterDesign
    {
        public List<RotatingPlayingStatus> PlayingList { get; set; } = new();
    }
}