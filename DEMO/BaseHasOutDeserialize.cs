using NonSucking.Framework.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEMO;
internal class BaseHasOutDeserialize
{
    public int MyProperty { get; set; }
    public int MyProperty2 { get; set; }
    public int MyProperty3 { get; set; }

    public static void Deserialize(BinaryReader reader, out int myProperty, out int myProperty2, out int myProperty3, out bool frenchFries)
    {
        myProperty = myProperty2 = myProperty3 = 0;
        frenchFries = false;
    }
}
[Nooson]
internal partial class HasBaseHasOutDeserialize : BaseHasOutDeserialize
{
    public string Type { get; set; }
}
