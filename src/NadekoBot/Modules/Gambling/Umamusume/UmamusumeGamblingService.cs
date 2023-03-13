using NadekoBot.Modules.Gambling.Common.Umamusume;

namespace NadekoBot.Modules.Gambling.Services;

public class UmamusumeGamblingService : INService
{
    internal List<UmaGamblingData> runningUmaGambling = new();
}
