namespace NadekoBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RequireGuildAttribute : PreconditionAttribute
{
    public RequireGuildAttribute(ulong gId)
    {
        GuildId = gId;
    }

    public ulong? GuildId { get; }
    public override string ErrorMessage { get; set; } = "此伺服器不可使用本指令";

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (context.Guild.Id == GuildId) return Task.FromResult(PreconditionResult.FromSuccess());
        else return Task.FromResult(PreconditionResult.FromError(ErrorMessage));
    }
}
