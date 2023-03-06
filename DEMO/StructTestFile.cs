using NonSucking.Framework.Serialization;

using System.Linq;

namespace DEMO;

[Nooson]
public partial struct TestStruct<T> where T :SUTMessage
{
    public int Bbq { get; set; }


}

public class Test
{
    public virtual void Abc<T>() 
    {

    }

}
[Nooson]
public partial class Test2 : Test
{
    public override void Abc<T>() where T: default
    {
    }
}