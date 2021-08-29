using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NonSucking.Framework.Extension.Generators.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace NonSucking.Framework.Extension.Generators
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

        public List<VisitInfo> ClassesToAugment { get; } = new List<VisitInfo>();

        private readonly NoosonAttributeTemplate attributeTemplate;

        public SyntaxReceiver(NoosonAttributeTemplate attributeTemplate)
        {
            this.attributeTemplate = attributeTemplate;
        }

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any field with at least one attribute is a candidate for property generation
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                //classDeclarationSyntax.AttributeLists.FirstOrDefault(a => a.Attributes.FirstOrDefault(at => at.));

                INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                System.Collections.Immutable.ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                var attribute
                    = attributes
                    .FirstOrDefault(d => d?.AttributeClass.ToDisplayString() == attributeTemplate.FullName);

                if (attribute == default)
                {
                    return;
                }

                List<TypeGroupInfo> properties
                    = classDeclarationSyntax
                        .Members
                        .OfType<PropertyDeclarationSyntax>()
                        .GroupBy(x => x.Type)
                        .OrderBy(x => x.Key.ToString())
                        .Select(
                            group =>
                            {
                                MemberInfo[] propertyInfos
                                = group
                                .Select(p =>
                                    {
                                        var prop = context.SemanticModel.GetDeclaredSymbol(p);
                                        return new MemberInfo(prop.Type, prop, prop.Name);
                                    }
                                )
                                .ToArray();

                                return new TypeGroupInfo(group.Key, context.SemanticModel.GetSymbolInfo(group.Key), propertyInfos);

                            })
                        .ToList();

                ClassesToAugment.Add(new VisitInfo(classDeclarationSyntax, typeSymbol, attribute, properties));
            }
        }
    }

}
