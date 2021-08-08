namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    public interface IStaticClassBuilder
    {

    }

    public static class IStaticClassBuilderExtension
    {

        public IPropertyBuilder AddStaticProperty(this IStaticClassBuilder builder) { }
        public IClassBuilder AddStaticField(this IStaticClassBuilder builder, AccessModifier accessModifier, string type, string name) { }
        public IMethodBuilder AddStaticMethod(this IStaticClassBuilder builder, AccessModifier accessModifier) { }
    }
}
