using NonSucking.Framework.Extension.Generators.Templates;
using System.IO;
using System.Reflection;

namespace NonSucking.Framework.Extension.Generators.Attributes
{
    public abstract class Template
    {
        private const string templateNamespace = "NonSucking.Framework.Extension.Generators.Templates";

        public abstract string Namespace { get; }
        public abstract string Name { get; }
        public abstract TemplateKind Kind { get; }
        public string FullName => $"{Namespace}.{Name}";

        private readonly string text;
        public Template()
        {
            var ressourceName = $"{templateNamespace}.{Kind}.{Name}.cs";

            using var ressourceStream
                = Assembly
                 .GetAssembly(typeof(GenSerializationAttributeTemplate))
                 .GetManifestResourceStream(ressourceName);

            using var reader = new StreamReader(ressourceStream);
            text = reader.ReadToEnd();
        }

        public override string ToString()
            => text;
    }
}