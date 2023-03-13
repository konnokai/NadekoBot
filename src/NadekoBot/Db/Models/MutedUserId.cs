#nullable disable
namespace NadekoBot.Services.Database.Models;

public class MutedUserId : DbEntity
{
    public ulong UserId { get; set; }
    public bool IsHardMute { get; set; } = false;

    public override int GetHashCode()
        => UserId.GetHashCode();

    public override bool Equals(object obj)
        => obj is MutedUserId mui ? mui.UserId == UserId && mui.IsHardMute == IsHardMute : false;
}