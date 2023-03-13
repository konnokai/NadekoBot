#nullable disable
namespace NadekoBot.Services.Database.Models;

public class UnmuteTimer : DbEntity
{
    public ulong UserId { get; set; }
    public DateTime UnmuteAt { get; set; }
    public bool IsHardMute { get; set; } = false;

    public override int GetHashCode()
        => UserId.GetHashCode();

    public override bool Equals(object obj)
        => obj is UnmuteTimer ut ? ut.UserId == UserId : false;
}