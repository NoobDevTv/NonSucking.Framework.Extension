using NonSucking.Framework.Serialization.Templates;

namespace NonSucking.Framework.Serialization.Attributes
{
    public class NoosonPreferredCtorAttributeTemplate : Template
    {
        public override string Namespace { get; } = "NonSucking.Framework.Serialization";
        public override string Name { get; } = "NoosonPreferredCtorAttribute";
        public  override TemplateKind Kind { get; } = TemplateKind.Attribute;
    }
}
