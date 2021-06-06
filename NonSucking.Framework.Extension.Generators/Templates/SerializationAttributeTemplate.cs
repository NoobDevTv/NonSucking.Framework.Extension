using NonSucking.Framework.Extension.Generators.Templates;

namespace NonSucking.Framework.Extension.Generators.Attributes
{
    public class GenSerializationAttributeTemplate : Template
    {
        public override string Namespace { get; } = "NonSucking.Framework.Extension.Serialization";
        public override string Name { get; } = "GenSerializationAttribute";
        public override TemplateKind Kind => TemplateKind.Attribute;
    }
}
