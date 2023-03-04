using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization;

using System.Text.Json.Serialization;

namespace DEMO;

[Nooson]
public partial class Versioning
{
    [NoosonOrder(int.MinValue)] public int Version { get; set; }

    [NoosonOrder(1)] public string SecondProp { get; set; }

    [NoosonOrder(2)]
    [NoosonVersioning(nameof(Checker), "", nameof(Version), nameof(SecondProp))]
    public string NewProp { get; set; }

    [NoosonOrder(3)]
    [NoosonVersioning(nameof(Checker), "\"asd\"", nameof(Version), nameof(NewProp))]
    public string NewProp2 { get; set; }

    [NoosonOrder(3)]
    [NoosonVersioning(nameof(Checker), "123", nameof(Version), nameof(NewProp))]
    public long NewProp3 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version">The abc <see cref="Version"/></param>
    /// <param name="another"></param>
    /// <returns></returns>
    private static bool Checker(int version, string another)
    {
        // defaultValue = default;
        return false;
    }

    private void Test2 ()
    {
        Test(out var i);
    }

    private void Test(out int i)
    {
        i = 0;
    }

    /////<summary>
    /////Deserializes a <see cref="Versioning"/> instance.
    /////</summary>
    /////<returns>The deserialized instance.</returns>
    //public static Versioning Deserialize(System.IO.BinaryReader reader)
    //{
    //    DEMO.Versioning ret____uH = default(DEMO.Versioning)!;

    //    ret____uH = new DEMO.Versioning()
    //    {
    //        Version = Version__ret__uC,
    //        SecondProp = SecondProp__ret__uD,
    //        NewProp = NewProp__ret__uE,
    //        NewProp2 = NewProp2__ret__uF,
    //        NewProp3 = NewProp3__ret__uG
    //    };
    //    return ret____uH;
    //}

    //public static void Deserialized(System.IO.BinaryReader reader, out ...)
    //{
    //    {
    //        int Version__ret__uC = reader.ReadInt32();
    //        string SecondProp__ret__uD = reader.ReadString();
    //        string NewProp__ret__uE = default(string)!;
    //        if (Checker(Version__ret__uC, SecondProp__ret__uD))
    //        {
    //            NewProp__ret__uE = reader.ReadString();
    //        }

    //        string NewProp2__ret__uF = default(string)!;
    //        if (Checker(Version__ret__uC, NewProp__ret__uE))
    //        {
    //            NewProp2__ret__uF = reader.ReadString();
    //        }
    //        else
    //            NewProp2__ret__uF = "asd";
    //        long NewProp3__ret__uG = default(long)!;
    //        if (Checker(Version__ret__uC, NewProp__ret__uE))
    //        {
    //            NewProp3__ret__uG = reader.ReadInt64();
    //        }
    //        else
    //            NewProp3__ret__uG = 123;
    //    }
    //    ... = variable;
    //}
}