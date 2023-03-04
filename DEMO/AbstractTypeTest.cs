using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public abstract partial class AbstractTypeTest
{
    public int Type { get; set; }
}

[Nooson]
public partial class InheritanceAbstractTypeTest : AbstractTypeTest
{
    public string Test2 { get; set; }
}