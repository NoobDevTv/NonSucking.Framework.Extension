using System;
using System.Net;
using System.Numerics;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class KnownSimpleTypesTest
{
    public IPAddress Address { get; set; }
    public Guid Id { get; set; }
    public BigInteger BigNumber { get; set; }
}