
namespace NadekoBot.Modules.Gambling.Common.Umamusume;

internal class UmaGamblingData
{
    internal IUser GamblingUser { get; set; }
    internal IUserMessage GamblingMessage { get; set; }
    internal IUserMessage SelectRankMessage { get; set; }
    internal string AddMessage { get; set; }
    internal string UmaGuid { get; set; } = "";
    internal Dictionary<ulong, string> SelectedRankDic { get; set; } = new Dictionary<ulong, string>();

    public UmaGamblingData(IUser user, IUserMessage gamblingMessage, IUserMessage selectRankMessage, string addMessage, string guid)
    {
        GamblingUser = user;
        GamblingMessage = gamblingMessage;
        SelectRankMessage = selectRankMessage;
        AddMessage = (string.IsNullOrEmpty(addMessage) ? "無" : addMessage);
        UmaGuid = guid;
    }
}
