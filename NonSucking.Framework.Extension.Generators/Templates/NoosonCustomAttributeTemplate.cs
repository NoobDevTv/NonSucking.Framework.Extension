using NonSucking.Framework.Extension.Generators.Templates;

namespace NonSucking.Framework.Extension.Generators.Attributes
{
    public class NoosonCustomAttributeTemplate : Template
    {
        public override string Namespace { get; } = "NonSucking.Framework.Extension.Serialization";
        public override string Name { get; } = "NoosonCustomAttribute";
        public override TemplateKind Kind { get; } = TemplateKind.Attribute;
    }
}
