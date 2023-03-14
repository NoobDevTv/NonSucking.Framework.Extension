using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace TestILStuff;

public class Program
{
    public class SomeClass
    {
        public int A { get; }
        public int B { get; init; }

        public SomeClass(int a)
        {
            A = a;
        }
    }

    public static void ReuseInstance(SomeClass input, int newValue)
    {
        Type type = typeof(SomeClass);
        ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(int) });

        DynamicMethod method = new DynamicMethod("Main", typeof(void), new Type[] { typeof(SomeClass), typeof(int) });
        ILGenerator il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, ctor);     // Call the constructor
        il.Emit(OpCodes.Ret);            // Return

        method.Invoke(null, new object[]{ input, newValue });
    }
    public static SomeClass CreateInstance(int value) => new SomeClass(value) {B = 24 };
    
    static void Main(string[] args)
    {
        var a = CreateInstance(12);
        Console.WriteLine($"Value: {a.A}");
        ReuseInstance(a, 13);
        Console.WriteLine($"Value: {a.A}");
    }
}
