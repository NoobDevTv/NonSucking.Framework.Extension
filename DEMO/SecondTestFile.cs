using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NonSucking.Framework.Serialization;
using System.Collections;

namespace DEMO;

public class MyEnumerable<T> : IEnumerable<T>
{
    public string Name { get; set; }

    public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}

public class ByteLengthList2 : ByteLengthList
{
    public string ABC { get; set; }
    public ByteLengthList2() : base()
    {
    }
    public ByteLengthList2(IEnumerable<byte[]> byteArrays) : base(byteArrays)
    {
    }
    public ByteLengthList2(byte capacity) : base(capacity)
    {

    }
    public ByteLengthList2(params byte[][] bytes) : base(bytes)
    {

    }
    public ByteLengthList2(int capacity) : base(capacity)
    {

    }
}

public partial class ByteLengthList : List<byte[]>
{
    public string NameOfList { get; set; }

    public ByteLengthList() : base()
    {
    }
    public ByteLengthList(IEnumerable<byte[]> byteArrays) : base(byteArrays)
    {
    }
    public ByteLengthList(byte capacity) : base(capacity)
    {

    }
    public ByteLengthList(params byte[][] bytes) : base(bytes)
    {

    }
    public ByteLengthList(int capacity) : base(capacity)
    {

    }
}



public enum MessageType
{
    Get,
    Update,
    Options,
    Relay
};

public enum Command
{
    Off,
    On,
    WhoIAm,
    IP,
    Time,
    Temp,
    Brightness,
    RelativeBrightness,
    Color,
    Mode,
    OnChangedConnections,
    OnNewConnection,
    Mesh,
    Delay,
    RGB,
    Strobo,
    RGBCycle,
    LightWander,
    RGBWander,
    Reverse,
    SingleColor,
    DeviceMapping,
    Calibration,
    Ota,
    OtaPart

};
public abstract class BaseSmarthomeMessage
{
    [JsonProperty("id")]
    public virtual uint NodeId { get; set; }

    [JsonProperty("m"), JsonConverter(typeof(StringEnumConverter))]
    public virtual MessageType MessageType { get; set; }

    [JsonProperty("c"), JsonConverter(typeof(StringEnumConverter))]
    public virtual Command Command { get; set; }

}

[Nooson]
public partial class BinarySmarthomeMessage : BaseSmarthomeMessage
{
    public SmarthomeHeader Header { get; set; }
    public override uint NodeId { get => base.NodeId; set => base.NodeId = value; }
    public override MessageType MessageType { get => base.MessageType; set => base.MessageType = value; }
    public override Command Command { get => base.Command; set => base.Command = value; }

    public ByteLengthList Parameters { get; set; }

    public BinarySmarthomeMessage(uint nodeId, MessageType messageType, Command command, params byte[][] parameters) : this(nodeId, messageType, command, new ByteLengthList(parameters))
    {
    }

    public BinarySmarthomeMessage(uint nodeId, MessageType messageType, Command command, ByteLengthList parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters;
        Header = new SmarthomeHeader(1, SmarthomePackageType.Normal);
    }

    public BinarySmarthomeMessage(SmarthomePackageType packageType, uint nodeId, MessageType messageType, Command command, params byte[][] parameters) : this(packageType, nodeId, messageType, command, new ByteLengthList(parameters))
    {
    }

    public BinarySmarthomeMessage(SmarthomePackageType packageType, uint nodeId, MessageType messageType, Command command, ByteLengthList parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters;
        Header = new SmarthomeHeader(1, packageType);
    }

    public BinarySmarthomeMessage()
    {

    }
}
public enum SmarthomePackageType : byte
{
    Error = 0,
    Normal = 1,
    Ota = 123
}


public partial struct SmarthomeHeader
{
    public SmarthomeHeader(byte version, SmarthomePackageType type)
    {
        Version = version;
        Type = type;
    }

    public byte Version { get; set; }
    public SmarthomePackageType Type { get; set; }
}

