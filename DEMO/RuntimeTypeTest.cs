using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson]
public partial class RuntimeTypeTestBase : IEquatable<RuntimeTypeTestBase>
{
    public int SomeValue { get; set; }

    public bool Equals(RuntimeTypeTestBase? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType())
            return false;

        return SomeValue == other.SomeValue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RuntimeTypeTestBase)obj);
    }

    public override int GetHashCode()
    {
        return SomeValue;
    }
}

[Nooson]
public partial class RuntimeTypeTestA : RuntimeTypeTestBase, IEquatable<RuntimeTypeTestA>
{
    public RuntimeTypeTestA(string someOtherValue)
    {
        SomeOtherValue = someOtherValue;
    }

    public string SomeOtherValue { get; }

    public bool Equals(RuntimeTypeTestA? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return SomeOtherValue == other.SomeOtherValue && SomeValue == other.SomeValue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RuntimeTypeTestA)obj);
    }

    public override int GetHashCode()
    {
        return SomeOtherValue.GetHashCode();
    }
}

[Nooson]
public partial class Generic<T> : IEquatable<Generic<T>>
{
    [NoosonDynamicType(Resolver = typeof(UnknownTypeResolver))]
    public T[] Prop { get; set; }


    public bool Equals(Generic<T>? other)
    {
        if (other is null)
            return false;
        return Prop.SequenceEqual(other.Prop);
    }
}

[Nooson]
public partial class RuntimeTypeTestB : RuntimeTypeTestBase, IEquatable<RuntimeTypeTestB>
{
    public RuntimeTypeTestB(int someOtherValue)
    {
        SomeOtherValue = someOtherValue;
    }

    public int SomeOtherValue { get; }

    public bool Equals(RuntimeTypeTestB? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SomeOtherValue == other.SomeOtherValue && SomeValue == other.SomeValue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RuntimeTypeTestB)obj);
    }

    public override int GetHashCode()
    {
        return SomeOtherValue;
    }
}

[Nooson]
public partial class RuntimeTypeTestC : RuntimeTypeTestBase
{
    public RuntimeTypeTestC(string someOtherValue)
    {
        SomeOtherValue = someOtherValue;
    }

    public string SomeOtherValue { get; }
}

[Nooson]
public partial class ContainingClass : IEquatable<ContainingClass>
{
    [NoosonDynamicType(typeof(RuntimeTypeTestC), Resolver = typeof(Resolver<string>))]
    public RuntimeTypeTestBase RuntimeValue { get; set; }

    public bool Equals(ContainingClass? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return RuntimeValue.Equals(other.RuntimeValue);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((ContainingClass)obj);
    }

    public override int GetHashCode()
    {
        return RuntimeValue.GetHashCode();
    }
}

internal class UnknownTypeResolver : NoosonRuntimeTypeResolver<string>
{
    public static UnknownTypeResolver Instance = new();
    protected override Type ResolveType<TReader>(string identifier)
    {
        return Type.GetType(identifier) ?? throw new ArgumentException();
    }

    protected override string SolveType<TWriter>(Type type)
    {
        return type.AssemblyQualifiedName ?? throw new ArgumentException();
    }
}


partial struct SomeWrapperStruct<T>
{
    public SomeWrapperStruct(T identifier)
    {
        Identifier = identifier;
    }

    public T Identifier { get; }
}
internal class Resolver<T> : NoosonRuntimeTypeResolver<SomeWrapperStruct<T>>
{
    public static Resolver<T> Instance = new();
    protected override Type ResolveType<TReader>(SomeWrapperStruct<T> identifier)
    {
        return Type.GetType(identifier.Identifier?.ToString() ?? "") ?? throw new ArgumentOutOfRangeException();
    }

    protected override SomeWrapperStruct<T> SolveType<TWriter>(Type type)
    {
        var val = type.AssemblyQualifiedName
                  ?? type.FullName
                  ?? throw new ArgumentOutOfRangeException();
        return new SomeWrapperStruct<T>((T)(object)val);
    }
}