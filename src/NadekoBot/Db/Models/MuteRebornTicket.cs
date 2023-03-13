namespace NadekoBot.Services.Database.Models;

public class MuteRebornTicket : DbEntity
{
    public ulong UserId { get; set; }
    public int RebornTicketNum { get; set; }
}