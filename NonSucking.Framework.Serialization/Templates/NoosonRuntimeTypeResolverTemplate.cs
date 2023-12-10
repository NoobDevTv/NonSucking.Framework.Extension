using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Templates;

namespace NonSucking.Framework.Serialization.AdditionalSource
{
    public class NoosonRuntimeTypeResolverTemplate : Template
    {
        public override string Namespace { get; } = "NonSucking.Framework.Serialization";
        public override string Name { get; } = "NoosonRuntimeTypeResolver";
        public override TemplateKind Kind { get; } = TemplateKind.AdditionalSource;
    }
}