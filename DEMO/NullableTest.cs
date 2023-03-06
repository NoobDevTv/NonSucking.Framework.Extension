#nullable enable

using NonSucking.Framework.Serialization;

namespace DEMO;

public class SomeData
{
    public int Abc { get; set; }
}

public struct SomeValueData
{
    public int Abc { get; set; }
}

[Nooson]
public partial class NullableEnabledTest
{
    public SomeData NotNull { get; private set; } = new () { Abc = 12};
    public SomeData? Null { get; private set; } = null;
    
    public int? NullableValueType { get; private set; }
    
    public SomeValueData? SomeValueData { get; private set; }
}

#nullable disable

[Nooson]
public partial class NullableDisabledTest
{
    public SomeData NotNull { get; private set; } = new () { Abc = 12};
#pragma warning disable CS8632
    public SomeData? Null { get; private set; } = null;
#pragma warning restore CS8632

    public int? NullableValueType { get; private set; }
    
    public SomeValueData? SomeValueData { get; private set; }
}