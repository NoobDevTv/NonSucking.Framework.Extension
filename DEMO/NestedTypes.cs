using NonSucking.Framework.Serialization;

namespace DEMO;

public partial class NestedTypes
{
    [Nooson]
    public partial struct NestedStruct
    {
        public int Prop { get; set; }
    }

    // [Nooson]
    public partial record NestedRecord(int Prop);

    // [Nooson]
    public partial record struct NestedRecordStruct(int Prop);

    public partial class NestedGeneric<T>
    {
        // [Nooson]
        public partial class NestedGenericClass
        {
            public int Prop { get; set; }
        }
    }
}