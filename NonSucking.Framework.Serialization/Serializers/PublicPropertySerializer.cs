using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static NonSucking.Framework.Serialization.NoosonGenerator;

using VaVare.Statements;
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    internal static class PublicPropertySerializer
    {

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, int baseTypesLevelProperties = int.MaxValue)
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

            statements.Statements.AddRange(
                GenerateStatementsForProps(
                   props
                       .Select(x => x with { Parent = property.FullName })
                       .ToArray(),
                   context,
                   MethodType.Serialize
               ).SelectMany(x => x.ToMergedBlock()));

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

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, int baseTypesLevelProperties = int.MaxValue)
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


            string randomForThisScope = Helper.GetRandomNameFor("", property.Parent);
            var statementList
                = GenerateStatementsForProps(
                    props.ToArray(),
                    context,
                    MethodType.Deserialize
                ).SelectMany(x => x.ToMergedBlock()).ToList();

            statements.Statements.AddRange(statementList);
            try
            {

                var ctorSyntax = CtorSerializer.CallCtorAndSetProps((INamedTypeSymbol)property.TypeSymbol, statementList, property, property.CreateUniqueName());
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
