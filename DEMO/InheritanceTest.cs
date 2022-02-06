using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class InheritanceBaseClass
{
    public int SomeValue { get; set; }
}

[Nooson]
public partial class InheritanceTest : InheritanceBaseClass
{
    public string OtherValue { get; set; }
}