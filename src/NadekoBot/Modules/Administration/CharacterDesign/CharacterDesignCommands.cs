using NadekoBot.Modules.Administration.Services;
using static NadekoBot.Modules.Administration.Services.CharacterDesignService;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    public class CharacterDesignCommands : NadekoModule<CharacterDesignService>
    {
        private DiscordSocketClient client;
        public CharacterDesignCommands(DiscordSocketClient client)
        {
            this.client = client;
        }

        [Cmd]
        [OwnerOnly]
        public async Task AddCharDesign(string designName = "", string charAvatar = "", [Leftover] string playingList = "")
        {
            if (string.IsNullOrEmpty(designName) || string.IsNullOrEmpty(charAvatar) || string.IsNullOrEmpty(playingList))
                return;

            (bool, CharacterDesign?) result = await _service.AddCharDesignAsync(designName, charAvatar, playingList).ConfigureAwait(false);

            if (result.Item1 && result.Item2 != null)
            {
                var embed = _eb.Create().WithOkColor()
                    .WithTitle("人設新增成功!")
                    .WithThumbnailUrl(charAvatar)
                    .WithDescription($"名稱: {designName}\n\n" +
                    $"PlayingStatus:\n" +
                    $"{string.Join('\n', result.Item2.PlayingList.Select(rs => $"{rs.Type} {rs.Status}"))}");

                await ReplyAsync(null, false, embed.Build());
            }
            else
                await ReplyAsync("人設已存在");
        }

        [Cmd]
        [OwnerOnly]
        public async Task AddCharDesignPlayingStatus(string designName = "", [Leftover] string playingList = "" )
        {
            if (string.IsNullOrEmpty(designName) || string.IsNullOrEmpty(playingList))
                return;

            (bool, CharacterDesign?) result = _service.AddCharDesignPlayingStatus(designName, playingList);

            if (result.Item1 && result.Item2 != null)
            {
                var embed = _eb.Create().WithOkColor()
                    .WithTitle("人設狀態新增成功!")
                    .WithDescription($"名稱: {designName}\n\n" +
                    $"PlayingStatus:" +
                    $"\n{string.Join('\n', result.Item2.PlayingList.Select(rs => $"{rs.Type} {rs.Status}"))}");

                await ReplyAsync(null, false, embed.Build());
            }
            else
                await ReplyAsync("人設狀態新增失敗");
        }

        [Cmd]
        [OwnerOnly]
        public async Task ChangeCharDesign([Leftover]string designName = "")
        {
            if (string.IsNullOrEmpty(designName))
                return;

            if (await _service.ChangeCharDesignAsync(designName).ConfigureAwait(false))
                await ReplyAsync("人設切換成功!");
            else
                await ReplyAsync("人設切換失敗");
        }

        [Cmd]
        [OwnerOnly]
        public async Task ListCharDesign()
        {
            var list = _service.ListCharDesign();

            if (list == null || list.Length == 0)
                await ReplyAsync("人設清單空白");
            else
                await ctx.SendPaginatedConfirmAsync(0, (row) =>
            {
                return _eb.Create().WithOkColor()
                .WithTitle("可用人設")
                .WithDescription(string.Join('\n', list.Skip(10 * row).Take(10)));
            }, list.Count(), 10);
        }

        [Cmd]
        [OwnerOnly]
        public async Task SaveCharDesign([Leftover] string designName = "")
        {
            if (string.IsNullOrEmpty(designName))
                designName = client.CurrentUser.Username;

            (bool, CharacterDesign?) result = await _service.SaveCharDesignAsync(designName).ConfigureAwait(false);

            if (result.Item1 && result.Item2 != null)
            {
                var embed = _eb.Create().WithOkColor()
                    .WithTitle("人設保存成功!")
                    .WithThumbnailUrl(client.CurrentUser.GetAvatarUrl())
                    .WithDescription($"名稱: {designName}\n\n" +
                    $"PlayingStatus:" +
                    $"\n{string.Join('\n', result.Item2.PlayingList.Select(rs => $"{rs.Type} {rs.Status}"))}");

                await ReplyAsync(null, false, embed.Build());
            }
            else
                await ReplyAsync("人設保存失敗");
        }

        [Cmd]
        [OwnerOnly]
        public async Task DeleteCharDesign([Leftover] string designName = "")
        {
            if (string.IsNullOrEmpty(designName))
                designName = client.CurrentUser.Username;

            if (await PromptUserConfirmAsync(_eb.Create().WithOkColor().WithDescription($"確定要刪除 {designName} 的人設嗎?")))
            {
                if (_service.DeleteCharDesign(designName)) await ReplyAsync("人設刪除成功");
                else await ReplyAsync("人設刪除失敗");
            }
        }
    }
}