using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class DynamicTypes
{
    [NoosonDynamicType(typeof(D), typeof(C), typeof(B), typeof(A))]
    public interface A
    {

    }

    [Nooson]
    public partial class B : A
    {

    }

    [Nooson]
    public partial class C : A
    {

    }

    [Nooson]
    public partial class D : B
    {
        public int Aaa { get; set; }
    }

    public A SomeProp { get; set; }
    private int Bab { get; set; }

    [NoosonDynamicType(typeof(int), typeof(string))]
    public object SomeOther { get; set; }
}
//     
//     void Test(System.IO.BinaryReader br)
//     {
//         this.Deserialize(br);
//     }
// }
