using Microsoft.CodeAnalysis;

using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Templates;

using System;
using System.Linq;

namespace NonSucking.Framework.Serialization
{
    public static class AttributeTemplates
    {
        internal static readonly NoosonConfigurationAttributeTemplate NoosonConfiguration = new();

        internal static readonly NoosonIgnoreAttributeTemplate Ignore = new();
        internal static readonly NoosonPreferredCtorAttributeTemplate PreferredCtor = new();
        internal static readonly NoosonParameterAttributeTemplate Parameter = new();
        internal static readonly NoosonCustomAttributeTemplate Custom = new();
        internal static readonly NoosonAttributeTemplate GenSerializationAttribute = new();
        internal static readonly NoosonOrderAttributeTemplate Order = new();
        internal static readonly NoosonIncludeAttributeTemplate Include = new();
        internal static readonly NoosonDynamicTypeAttributeTemplate DynamicType = new();
        internal static readonly NoosonVersioningAttributeTemplate Versioning = new();
        internal static readonly NoosonConversionAttributeTemplate NoosonConversion = new();


        public static AttributeData? GetAttribute(this ISymbol symbol, Template attributeTemplate)
        {
            if (attributeTemplate is null)
                throw new ArgumentNullException(nameof(attributeTemplate));
            else if (attributeTemplate.Kind != TemplateKind.Attribute)
                throw new ArgumentException(nameof(attributeTemplate) + " is not attribute");

            return symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == attributeTemplate.FullName);
        }

        public static bool TryGetAttribute(this ISymbol symbol, Template attributeTemplate, out AttributeData? attributeData)
        {
            attributeData = GetAttribute(symbol, attributeTemplate);
            return attributeData is not null;
        }
    }

}
