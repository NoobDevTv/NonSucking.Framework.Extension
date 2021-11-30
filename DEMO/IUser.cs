using NonSucking.Framework.Serialization;

namespace DEMO
{
    [NoosonCustom(SerializeMethodName = "SerializeMe", DeserializeMethodName = "DeserializeMe", SerializeImplementationType = typeof(SUTMessage.User), DeserializeImplementationType = typeof(SUTMessage.User))]
    public interface IUser
    {
        string Name { get; set; }
    }
}