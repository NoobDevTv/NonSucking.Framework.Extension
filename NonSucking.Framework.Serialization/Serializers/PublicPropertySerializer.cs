using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Serializers;

using static NonSucking.Framework.Serialization.NoosonGenerator;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(70)]
    internal static class PublicPropertySerializer
    {

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, NoosonGenerator.SerializerMask includedSerializers, int baseTypesLevelProperties = int.MaxValue)
        {
            var props
                = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                .Where(property =>
                    property.Name != "this[]")
               .Select(x => x with { Parent = property.FullName });

            var writeOnlies = props.Select(x => x.Symbol).OfType<IPropertySymbol>().Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            props = FilterPropsForNotWriteOnly(props);

            foreach (var prop in OrderProps(props))
            {
                var propCode = GenerateStatementsForMember(prop, context, MethodType.Serialize);
                if (propCode == null)
                    continue;
                statements.Statements.AddRange(propCode.ToMergedBlock());
            }

            return true;

        }

        private static IEnumerable<MemberInfo> FilterPropsForNotWriteOnly(IEnumerable<MemberInfo> props)
        {
            props = props.Where(x =>
            {
                if (x.Symbol is IPropertySymbol ps
                    && !ps.IsWriteOnly
                    && ps.GetMethod is not null)
                {
                    return true;
                }
                else if (x.Symbol is IFieldSymbol fs)
                {
                    return true;
                }
                return false;

            });
            return props;
        }

        private static IEnumerable<MemberInfo> OrderProps(IEnumerable<MemberInfo> props)
        {
            return props.OrderBy(x =>
                                 {
                                     var attr = x.Symbol.GetAttribute(AttributeTemplates.Order);
                                     if (attr == null)
                                         return int.MaxValue;
                                     return (int)attr.ConstructorArguments[0].Value!;
                                 });
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, NoosonGenerator.SerializerMask includedSerializers, int baseTypesLevelProperties = int.MaxValue)
        {
            var props
               = Helper.GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
               .Where(property =>
                   property.Name != "this[]")
               .Select(x => x with { Parent = property.Name });

            var writeOnlies = props.Select(x => x.Symbol).OfType<IPropertySymbol>().Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            props = FilterPropsForNotWriteOnly(props);

            foreach (var prop in OrderProps(props))
            {
                var propCode = GenerateStatementsForMember(prop, context, MethodType.Deserialize);
                if (propCode == null)
                    continue;
                statements.Statements.AddRange(propCode.ToMergedBlock());
            }

            try
            {

                var ctorSyntax = CtorSerializer.CallCtorAndSetProps((INamedTypeSymbol)property.TypeSymbol, statements.Statements, property, property.CreateUniqueName());
                statements.MergeWith(ctorSyntax);

            }
            catch (NotSupportedException)
            {
                context.AddDiagnostic("0006",
                   "",
                   "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments.",
                   property.Symbol,
                   DiagnosticSeverity.Error
                   );
            }
            return true;
        }
    }
}
