using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class NotSettablePropsTest
{
    public int OnlyGet { get; }
    public int WithInit { get; init; }
}