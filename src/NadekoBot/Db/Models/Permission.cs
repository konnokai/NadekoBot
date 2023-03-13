#nullable disable
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace NadekoBot.Services.Database.Models;

[DebuggerDisplay("{PrimaryTarget}{SecondaryTarget} {SecondaryTargetName} {State} {PrimaryTargetId}")]
public class Permissionv2 : DbEntity, IIndexed
{
    public int? GuildConfigId { get; set; }
    public int Index { get; set; }

    public PrimaryPermissionType PrimaryTarget { get; set; }
    public ulong PrimaryTargetId { get; set; }

    public SecondaryPermissionType SecondaryTarget { get; set; }
    public string SecondaryTargetName { get; set; }

    public bool IsCustomCommand { get; set; }

    public bool State { get; set; }

    [NotMapped]
    public static Permissionv2 AllowAllPerm
        => new()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.AllModules,
            SecondaryTargetName = "*",
            State = true,
            Index = 0
        };

    [NotMapped]
    public static Permissionv2 DenyAllActualExpressions
        => new()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.Module,
            SecondaryTargetName = "actualexpressions",
            State = false,
            Index = 1
        };

    [NotMapped]
    public static Permissionv2 AllowOwnerActualExpressions
        => new()
        {
            PrimaryTarget = PrimaryPermissionType.User,
            PrimaryTargetId = 284989733229297664,
            SecondaryTarget = SecondaryPermissionType.Module,
            SecondaryTargetName = "actualexpressions",
            State = true,
            Index = 2
        };

    public static List<Permissionv2> GetDefaultPermlist
        => new()
        {
            AllowAllPerm,
            DenyAllActualExpressions,
            AllowOwnerActualExpressions
        };
}

public enum PrimaryPermissionType
{
    User,
    Channel,
    Role,
    Server
}

public enum SecondaryPermissionType
{
    Module,
    Command,
    AllModules
}