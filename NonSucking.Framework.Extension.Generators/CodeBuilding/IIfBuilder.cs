namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    public interface IIfBuilder
    {

    }

    public static class IIfBuilderExtension
    {
        public static IIfBuilder AddElseIf(this IIfBuilder builder, string condition) { }
        public static IBlockBuilder AddElse(this IIfBuilder builder) { }
    }
}
