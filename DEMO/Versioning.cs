using Microsoft.CodeAnalysis.CSharp.Syntax;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class Versioning
{
    [NoosonOrder(int.MinValue)] public int Version { get; set; }

    [NoosonOrder(1)] public string SecondProp { get; set; }

    [NoosonOrder(2)]
    [NoosonVersioning(nameof(Checker), "", nameof(Version), nameof(SecondProp))]
    public string NewProp { get; set; }

    [NoosonOrder(3)]
    [NoosonVersioning(nameof(Checker), "\"asd\"", nameof(Version), nameof(NewProp))]
    public string NewProp2 { get; set; }

    [NoosonOrder(3)]
    [NoosonVersioning(nameof(Checker), "123", nameof(Version), nameof(NewProp))]
    public long NewProp3 { get; set; }

    private static bool Checker(int version, string another)
    {
        // defaultValue = default;
        return false;
    }
}