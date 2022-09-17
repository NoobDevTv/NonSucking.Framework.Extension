using System;
using System.Drawing;
using System.Globalization;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial record UnmanagedTypes
{
    public Point SomePos { get; set; }
    public DateTime SomeTime { get; set; }
}