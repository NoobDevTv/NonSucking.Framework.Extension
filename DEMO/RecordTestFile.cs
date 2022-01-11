using System;
using System.Runtime.CompilerServices;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial record TestRecord
{
}

[Nooson]
public partial record struct TestRecordStruct
{
}

[Nooson]
public partial record TestRecordCustomContract
{
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get => typeof(TestRecordCustomContract);
    }
}