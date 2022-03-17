using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class DynamicTypes
{
    [NoosonDynamicType(typeof(D), typeof(C), typeof(B), typeof(A))]
    [Nooson]
    public partial class A
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
        
    }
    
    public B SomeProp { get; set; }
    
    [NoosonDynamicType(typeof(int), typeof(string))]
    public object SomeOther { get; set; }
}