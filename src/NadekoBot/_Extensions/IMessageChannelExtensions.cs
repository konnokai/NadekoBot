using LinqToDB;

namespace NadekoBot.Extensions;

public static class MessageChannelExtensions
{
    // main overload that all other send methods reduce to
    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false,
        MessageComponent? components = null)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        return channel.SendMessageAsync(plainText,
            embed: embed,
            embeds: embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray(),
            components: components,
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });
    }

    public static async Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        NadekoInteraction? inter,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false)
    {
        var msg = await channel.SendAsync(plainText,
            embed,
            embeds,
            sanitizeAll,
            inter?.CreateComponent());

        if (inter is not null)
            await inter.RunAsync(msg);

        return msg;
    }

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        SmartText text,
        bool sanitizeAll = false)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText,
                set.IsValid ? set.GetEmbed().Build() : null,
                sanitizeAll: sanitizeAll),
            SmartPlainText st => channel.SendAsync(st.Text,
                default(Embed),
                sanitizeAll: sanitizeAll),
            SmartEmbedTextArray arr => channel.SendAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build())),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static Task<IUserMessage> EmbedAsync(
        this IMessageChannel ch,
        IEmbedBuilder? embed,
        string plainText = "",
        IReadOnlyCollection<IEmbedBuilder>? embeds = null,
        NadekoInteraction? inter = null)
        => ch.SendAsync(plainText,
            inter,
            embed: embed?.Build(),
            embeds: embeds?.Map(x => x.Build()));

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string text,
        MessageType type,
        NadekoInteraction? inter = null)
    {
        var builder = eb.Create().WithDescription(text);

        builder = (type switch
        {
            MessageType.Error => builder.WithErrorColor(),
            MessageType.Ok => builder.WithOkColor(),
            MessageType.Pending => builder.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        });

        return ch.EmbedAsync(builder, inter: inter);
    }

    // regular send overloads
    public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MessageType.Error);

    public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MessageType.Ok);

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        MessageType type,
        string? title,
        string text,
        string? url = null,
        string? footer = null)
    {
        var embed = eb.Create()
                      .WithDescription(text)
                      .WithTitle(title);

        if (url is not null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed.WithFooter(footer);

        embed = type switch
        {
            MessageType.Error => embed.WithErrorColor(),
            MessageType.Ok => embed.WithOkColor(),
            MessageType.Pending => embed.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        return ch.EmbedAsync(embed);
    }

    // embed title and optional footer overloads

    public static Task<IUserMessage> SendConfirmAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string? title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MessageType.Ok, title, text, url, footer);

    public static Task<IUserMessage> SendErrorAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MessageType.Error, title, text, url, footer);

    // weird stuff

    public static Task<IUserMessage> SendTableAsync<T>(
        this IMessageChannel ch,
        string seed,
        IEnumerable<T> items,
        Func<T, string> howToPrint,
        int columns = 3)
        => ch.SendMessageAsync($@"{seed}```css
{items.Chunk(columns)
      .Select(ig => string.Concat(ig.Select(howToPrint)))
      .Join("\n")}
```");

    public static Task<IUserMessage> SendTableAsync<T>(
        this IMessageChannel ch,
        IEnumerable<T> items,
        Func<T, string> howToPrint,
        int columns = 3)
        => ch.SendTableAsync("", items, howToPrint, columns);

    public static Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, IEmbedBuilder> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
        => ctx.SendPaginatedConfirmAsync(currentPage,
            x => Task.FromResult(pageFunc(x)),
            totalElements,
            itemsPerPage,
            addPaginatedFooter);

    private const string BUTTON_LEFT = "BUTTON_LEFT";
    private const string BUTTON_RIGHT = "BUTTON_RIGHT";
    private const string BUTTON_YES = "BUTTON_YES";
    private const string BUTTON_NO = "BUTTON_NO";

    private static readonly IEmote _arrowLeft = new Emoji("⬅");
    private static readonly IEmote _arrowRight = new Emoji("➡");

    public static Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
        => ctx.SendPaginatedConfirmAsync(currentPage,
            pageFunc,
            default(Func<int, ValueTask<SimpleInteraction<object>?>>),
            totalElements,
            itemsPerPage,
            addPaginatedFooter);

    public static async Task SendPaginatedConfirmAsync<T>(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        Func<int, ValueTask<SimpleInteraction<T>?>>? interFactory,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
    {
        var lastPage = (totalElements - 1) / itemsPerPage;

        var embed = await pageFunc(currentPage);

        if (addPaginatedFooter)
            embed.AddPaginatedFooter(currentPage, lastPage);

        SimpleInteraction<T>? maybeInter = null;
        async Task<ComponentBuilder> GetComponentBuilder()
        {
            var cb = new ComponentBuilder();

            cb.WithButton(new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId(BUTTON_LEFT)
                .WithDisabled(lastPage == 0)
                .WithEmote(_arrowLeft)
                .WithDisabled(currentPage <= 0));

            if (interFactory is not null)
            {
                maybeInter = await interFactory(currentPage);

                if (maybeInter is not null)
                    cb.WithButton(maybeInter.Button);
            }

            cb.WithButton(new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId(BUTTON_RIGHT)
                .WithDisabled(lastPage == 0 || currentPage >= lastPage)
                .WithEmote(_arrowRight));

            return cb;
        }

        async Task UpdatePageAsync(SocketMessageComponent smc)
        {
            var toSend = await pageFunc(currentPage);
            if (addPaginatedFooter)
                toSend.AddPaginatedFooter(currentPage, lastPage);

            var component = (await GetComponentBuilder()).Build();

            await smc.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = toSend.Build();
                x.Components = component;
            });
        }

        var component = (await GetComponentBuilder()).Build();
        var msg = await ctx.Channel.SendAsync(null, embed: embed.Build(), components: component);

        async Task OnInteractionAsync(SocketInteraction si)
        {
            try
            {
                if (si is not SocketMessageComponent smc)
                    return;

                if (smc.Message.Id != msg.Id)
                    return;

                await si.DeferAsync();
                if (smc.User.Id != ctx.User.Id)
                    return;

                if (smc.Data.CustomId == BUTTON_LEFT)
                {
                    if (currentPage == 0)
                        return;

                    --currentPage;
                    _ = UpdatePageAsync(smc);
                }
                else if (smc.Data.CustomId == BUTTON_RIGHT)
                {
                    if (currentPage >= lastPage)
                        return;

                    ++currentPage;
                    _ = UpdatePageAsync(smc);
                }
                else if (maybeInter is { } inter && inter.Button.CustomId == smc.Data.CustomId)
                {
                    await inter.TriggerAsync(smc);
                    _ = UpdatePageAsync(smc);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in pagination: {ErrorMessage}", ex.Message);
            }
        }

        if (lastPage == 0 && interFactory is null)
            return;

        var client = (DiscordSocketClient)ctx.Client;

        client.InteractionCreated += OnInteractionAsync;

        await Task.Delay(30_000);

        client.InteractionCreated -= OnInteractionAsync;

        await msg.ModifyAsync(mp => mp.Components = new ComponentBuilder().Build());
    }

    private static readonly Emoji _okEmoji = new Emoji("✅");
    private static readonly Emoji _errorEmoji = new Emoji("❌");
    private static readonly Emoji _warnEmoji = new Emoji("⚠️");

    public static Task ReactAsync(this ICommandContext ctx, MessageType type)
    {
        var emoji = type switch
        {
            MessageType.Error => _errorEmoji,
            MessageType.Pending => _warnEmoji,
            MessageType.Ok => _okEmoji,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        return ctx.Message.AddReactionAsync(emoji);
    }

    public static async Task SendYesNoConfirmAsync(this ICommandContext ctx, IEmbedBuilder eb, string text, Action<bool> action, IUser? user = null, bool withNo = true)
    {
        ComponentBuilder GetComponentBuilder()
        {
            var cb = new ComponentBuilder();

            cb.WithButton(new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId(BUTTON_YES)
                .WithEmote(_okEmoji));

            if (withNo)
            {
                cb.WithButton(new ButtonBuilder()
                    .WithStyle(ButtonStyle.Danger)
                    .WithCustomId(BUTTON_NO)
                    .WithEmote(_errorEmoji));
            }

            return cb;
        }

        async Task RemoveComponentAsync(SocketMessageComponent smc)
        {            
            await smc.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = eb.Build();
                x.Components = new ComponentBuilder().Build(); // 按下後就移除掉按鈕
            });
        }

        eb.WithOkColor().WithDescription(text);

        var component = GetComponentBuilder().Build();
        var msg = await ctx.Channel.SendAsync(null, embed: eb.Build(), components: component);
        bool isSelect = false;

        async Task OnInteractionAsync(SocketInteraction si)
        {
            try
            {
                if (si.HasResponded)
                    return;

                if (si is not SocketMessageComponent smc)
                    return;

                if (smc.Message.Id != msg.Id)
                    return;

                if (isSelect)
                    return;

                if (user != null && smc.User.Id != user.Id)
                {
                    await si.RespondAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription("你不可使用本按鈕").Build(), ephemeral: true);
                    return;
                }
                else if (user == null && smc.User.Id != ctx.User.Id)
                {
                    await si.RespondAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription("你不可使用本按鈕").Build(), ephemeral: true);
                    return;
                }

                await si.DeferAsync();

                if (smc.Data.CustomId == BUTTON_YES)
                {
                    action(true);
                    isSelect = true;
                    _ = RemoveComponentAsync(smc);
                }
                else if (smc.Data.CustomId == BUTTON_NO)
                {
                    action(false);
                    isSelect = true;
                    _ = RemoveComponentAsync(smc);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SendYesNoConfirmAsync pagination: {ErrorMessage}", ex.Message);
            }
        }

        var client = (DiscordSocketClient)ctx.Client;

        client.InteractionCreated += OnInteractionAsync;

        int i = 60;
        do
        {
            i--;
            await Task.Delay(500);
        } while (!isSelect && i >= 0);

        client.InteractionCreated -= OnInteractionAsync;

        try
        { 
            await msg.ModifyAsync(mp => mp.Components = new ComponentBuilder().Build());
        }
        catch (HttpException discordEx) when (discordEx.DiscordCode == DiscordErrorCode.UnknownMessage)
        {
            Log.Information("訊息已刪除，略過");
        }
    }

    public static async Task<Dictionary<IEmote, List<ulong>>> GetEmojiCountAsync(this ICommandContext ctx, IEmbedBuilderService eb, string text)
    {
        var dic = new Dictionary<IEmote, List<ulong>>();
        var msg = await ctx.Channel.SendConfirmAsync(eb, text).ConfigureAwait(false);
        await Task.Delay(30000).ConfigureAwait(false);

        try
        {
            msg = await ctx.Channel.GetMessageAsync(msg.Id).ConfigureAwait(false) as IUserMessage;

            foreach (var item in msg.Reactions)
            {
                var list = await msg.GetReactionUsersAsync(item.Key, 30).FlattenAsync().ConfigureAwait(false);
                foreach (var item2 in list)
                {
                    if (dic.ContainsKey(item.Key))
                        dic[item.Key].Add(item2.Id);
                    else
                        dic.Add(item.Key, new List<ulong>() { item2.Id });
                }
            }

            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }
        catch
        {
            await ctx.Channel.SendErrorAsync(eb, "原訊息已刪除導致無法統計，略過加時");
        }

        return dic;
    }

    public static Task OkAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Ok);

    public static Task ErrorAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Error);

    public static Task WarningAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Pending);
}

public enum MessageType
{
    Ok,
    Pending,
    Error
}