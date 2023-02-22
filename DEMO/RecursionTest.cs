namespace DEMO;

using NonSucking.Framework.Serialization;

[Nooson]
public partial class RecursionTest
{
    public TestRecursion Recursion { get; set; }

    public class TestRecursion
    {
        
        public TestRecursion2 TestProp { get; set; }
    }
    public class TestRecursion2
    {
        public TestRecursion TestProp { get; set; }
    }

}
