namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    public interface IClassBuilder : IBlockBuilder, IStaticClassBuilder
    {
    

    }

    public static class IClassBuilderExtension
    {
        public static IPropertyBuilder AddProperty(this IClassBuilder builder) { }
        public static IClassBuilder AddField(this IClassBuilder builder, AccessModifier accessModifier, string type, string name) { }
        public static IMethodBuilder AddMethod(this IClassBuilder builder, AccessModifier accessModifier) { }
        public static IClassBuilder AddClass(this IClassBuilder builder, AccessModifier accessModifier, string name) { }
    }
}
