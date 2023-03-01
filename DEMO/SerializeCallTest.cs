using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NonSucking.Framework.Serialization;

namespace DEMO;
[Nooson]
public partial class SerializeCallTest
{
    public OverrideSerializeCalLTest MyProperty { get; set; }
    public List<OverrideSerializeCalLTest> MyProperties { get; set; }
}

public class OverrideSerializeCalLTest : BaseSerializeCallTest
{
    public int MyProperty { get; set; }
    public static OverrideSerializeCalLTest Deserialize(BinaryReader br)
    {
        throw new NotImplementedException();
    }
    //public override void Deserialize(BinaryReader br)
    //{
    //    throw new NotImplementedException();
    //}

    public override void Serialize(BinaryWriter bw)
    {
        throw new NotImplementedException();
    }
}

public abstract class BaseSerializeCallTest
{
    public int MyBaseProp { get; set; }

    public virtual void Serialize(BinaryWriter bw)
    {
    }
    //public abstract void Deserialize(BinaryReader br);

}
