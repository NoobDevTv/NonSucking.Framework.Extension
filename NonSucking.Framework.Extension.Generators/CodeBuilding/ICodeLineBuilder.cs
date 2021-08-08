using System.Linq.Expressions;

namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    public interface ICodeLineBuilder : IBlockBuilder
    {
    }

    public static class ICodeLineBuilderExtensions
    {
        public static IVariableBuilder AddVariable(this ICodeLineBuilder clb, string type, string name) { } //string str;
        public static IIfBuilder AddIf(this ICodeLineBuilder clb, string condition) {
         //var ifEx  = Expression.IfThen(Expression.Assign()).ToString()
        }
        public static ILoopBuilder AddLoop(this ICodeLineBuilder clb, LoopType type, string initializer) { }
        public static ICodeLineBuilder AddMethodCall(this ICodeLineBuilder clb, string name, params string[] args) { }
        public static IMethodBuilder CustomLine(this ICodeLineBuilder clb, string codeLine) { }
    }
}
