using NonSucking.Framework.Serialization;

namespace DEMO.Base;

[Nooson]
public partial class BaseClassWithNooson : BaseClassWithoutNoosonButMethods
{
}

public class BaseClassWithoutNoosonButMethods : BaseBaseClassWithoutNoosonButMethods
{
    public int TestProp1 { get; set; }

    ///<summary>
    ///Serializes the given <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> instance.
    ///</summary>
    ///<param name = "that">The instance to serialize.</param>
    ///<param name = "writer">The <see cref="System.IO.BinaryWriter"/> to serialize to.</param>
    public static void StaticSerialize(DEMO.Base.BaseClassWithoutNoosonButMethods that, System.IO.BinaryWriter writer)
    {
        that.OwnSerialize(writer);
    }

    ///<summary>
    ///Serializes this instance.
    ///</summary>
    ///<param name = "writer">The <see cref="System.IO.BinaryWriter"/> to serialize to.</param>
    public virtual void OwnSerialize(System.IO.BinaryWriter writer)
    {
        writer.Write(TestProp1);
    }

    ///<summary>
    ///Deserializes the properties of a <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> type.
    ///</summary>
    ///<param name = "reader">The <see cref="System.IO.BinaryReader"/> to deserialize from.</param>
    ///<param name = "TestProp1_️ret_️k">The deserialized instance of the property <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods.TestProp1"/>.</param>
    ///<param name = "TestProp1_️ret_️l">The deserialized instance of the property <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods.TestProp1"/>.</param>
    public static void ThisHasOutParams(System.IO.BinaryReader reader, out int Test2, out int TestProp1_️ret_️k)
    {
        TestProp1_️ret_️k = reader.ReadInt32();
        Test2 = reader.ReadInt32();
    }

    ///<summary>
    ///Deserializes a <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> instance.
    ///</summary>
    ///<param name = "reader">The <see cref="System.IO.BinaryReader"/> to deserialize from.</param>
    ///<returns>The deserialized instance.</returns>
    public static DEMO.Base.BaseClassWithoutNoosonButMethods WithCtorBase(System.IO.BinaryReader reader)
    {
        DEMO.Base.BaseClassWithoutNoosonButMethods ret_️_️o = default(DEMO.Base.BaseClassWithoutNoosonButMethods)!;
        DEMO.Base.BaseClassWithoutNoosonButMethods.ThisHasOutParams(reader, out var TestProp1_️ret_️m, out _);
        ret_️_️o = new DEMO.Base.BaseClassWithoutNoosonButMethods()
        {
            TestProp1 = TestProp1_️ret_️m
        };
        return ret_️_️o;
    }

    ///<summary>
    ///Deserializes into <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> instance.
    ///</summary>
    ///<param name = "that">The instance to deserialize into.</param>
    ///<param name = "reader">The <see cref="System.IO.BinaryReader"/> to deserialize from.</param>
    public static void IntoInstanceBase(DEMO.Base.BaseClassWithoutNoosonButMethods that, System.IO.BinaryReader reader)
    {
        DEMO.Base.BaseClassWithoutNoosonButMethods.ThisHasOutParams(reader, out var TestProp1_️ret_️s, out _);
        that.TestProp1 = TestProp1_️ret_️s;
    }

    ///<summary>
    ///Deserializes into <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> instance.
    ///</summary>
    ///<param name = "reader">The <see cref="System.IO.BinaryReader"/> to deserialize from.</param>
    public virtual void OnInstanceBase(System.IO.BinaryReader reader)
    {
        DEMO.Base.BaseClassWithoutNoosonButMethods.IntoInstanceBase(this, reader);
    }

}


public class BaseBaseClassWithoutNoosonButMethods
{
    ///<summary>
    ///Deserializes the properties of a <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods"/> type.
    ///</summary>
    ///<param name = "reader">The <see cref="System.IO.BinaryReader"/> to deserialize from.</param>
    ///<param name = "TestProp1_️ret_️k">The deserialized instance of the property <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods.TestProp1"/>.</param>
    ///<param name = "TestProp1_️ret_️l">The deserialized instance of the property <see cref="DEMO.Base.BaseClassWithoutNoosonButMethods.TestProp1"/>.</param>
    public static void ThisHasOutParams(System.IO.BinaryReader reader, out string p, out int TestProp1_️ret_️k)
    {
        TestProp1_️ret_️k = reader.ReadInt32();
        p = "";
    }


}