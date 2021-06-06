using NonSucking.Framework.Extension.Generators.Templates;

namespace NonSucking.Framework.Extension.Generators.Attributes
{
    public class NoosonPreferredCtorAttributeTemplate : Template
    {
        public override string Namespace { get; } = "NonSucking.Framework.Extension.Serialization";
        public override string Name { get; } = "NoosonPreferredCtorAttribute";
        public  override TemplateKind Kind { get; } = TemplateKind.Attribute;
    }
}
