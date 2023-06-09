#nullable disable
using Nadeko.Common;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.NadekoExpressions;

public class ExportedExpr
{
    public string Res { get; set; }
    public string Id { get; set; }
    public bool Ad { get; set; }
    public bool Dm { get; set; }
    public bool At { get; set; }
    public bool Ca { get; set; }
    public bool Oo { get; set; }
    public bool Ir { get; set; }
    public string[] React;

    public static ExportedExpr FromModel(NadekoExpression cr)
        => new()
        {
            Res = cr.Response,
            Id = ((kwum)cr.Id).ToString(),
            Ad = cr.AutoDeleteTrigger,
            At = cr.AllowTarget,
            Ca = cr.ContainsAnywhere,
            Dm = cr.DmResponse,
            Oo = cr.OwnerOnly,
            Ir = cr.IsRegex,
            React = string.IsNullOrWhiteSpace(cr.Reactions) ? null : cr.GetReactions()
        };
}