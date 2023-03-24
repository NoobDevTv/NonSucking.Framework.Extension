using System.ComponentModel;
using System.Globalization;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class TypeConverterTest
{
    [NoosonConversion(typeof(Converter), ConvertTo = typeof(int))]
    public bool ValueToConvert { get; set; }
}

[Nooson]
partial struct LegacyBool
{
    public LegacyBool(string value)
    {
        Value = value;
    }

    public string Value { get; }
    // public static implicit operator bool(LegacyBool v)
    // {
    //     return bool.Parse(v.Value);
    // }
    //
    // public static implicit operator LegacyBool(bool v)
    // {
    //     return new(v.ToString());
    // }
}


class Converter : INoosonConverter<bool, LegacyBool>, INoosonConverter<bool, int>
{
    public static Converter Instance { get; } = new();
    public bool TryConvert(bool val, out LegacyBool res)
    {
        res = new(val.ToString());
        return true;
    }

    public bool TryConvert(LegacyBool val, out bool res)
    {
        res = bool.Parse(val.Value);
        return true;
    }

    public bool TryConvert(bool val, out int res)
    {
        throw new NotImplementedException();
    }

    public bool TryConvert(int val, out bool res)
    {
        throw new NotImplementedException();
    }
}