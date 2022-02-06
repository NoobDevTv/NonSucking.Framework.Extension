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

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, int baseTypesLevelProperties = int.MaxValue)
        {
            var propsAndFields
                = Helper
                .GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
                .Where(property =>
                    property.Name != "this[]")
               .Select(x => x with { Parent = property.Name });
            var props = propsAndFields.Select(x => x.Symbol).OfType<IPropertySymbol>();
            var writeOnlies = props.Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            var initOnlies = props.Where(x => x.SetMethod?.IsInitOnly ?? false);
            foreach (var onlyWrite in initOnlies)
            {
                context.AddDiagnostic("0011",
                       "",
                       "Properties who are init only are (currently) not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            propsAndFields = FilterPropsForNotWriteOnly(propsAndFields);

            statements.AddRange(
                GenerateStatementsForProps(
                   propsAndFields
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
                    && ps.SetMethod is not null
                    && !ps.SetMethod.IsInitOnly
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
            var propsAndFields
               = Helper
               .GetMembersWithBase(property.TypeSymbol, baseTypesLevelProperties)
               .Where(property =>
                   property.Name != "this[]")
               .Select(x => x with { Parent = property.Name });
            var props = propsAndFields.Select(x => x.Symbol).OfType<IPropertySymbol>();

            var writeOnlies = props.Where(x => x.IsWriteOnly || x.GetMethod is null);
            foreach (var onlyWrite in writeOnlies)
            {
                context.AddDiagnostic("0007",
                       "",
                       "Properties who are write only are not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            var initOnlies = props.Where(x => x.SetMethod?.IsInitOnly ?? false);
            foreach (var onlyWrite in initOnlies)
            {
                context.AddDiagnostic("0011",
                       "",
                       "Properties who are init only are (currently) not supported. Implemented a custom serializer method or ignore this property.",
                       property.TypeSymbol,
                       DiagnosticSeverity.Error
                       );
            }

            propsAndFields = FilterPropsForNotWriteOnly(propsAndFields);


            string randomForThisScope = Helper.GetRandomNameFor("", property.Parent);
            var statementList
                = GenerateStatementsForProps(
                    propsAndFields.ToArray(),
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
