using DEMO.Base;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
internal partial class DemoBaseImplementation : BaseClassWithNooson
{
    public int Type { get; set; }
}

[Nooson]
internal partial class SecondDemoBaseImplementation : DemoBaseImplementation
{
    public int Type2 { get; set; }
}

[Nooson]
internal partial class StringDemoBaseImplementation : BaseClassWithNooson
{
    public string Test { get; set; }
}

[Nooson]
internal partial class NothingAddedDemoBaseImplementation : BaseClassWithNooson
{
}