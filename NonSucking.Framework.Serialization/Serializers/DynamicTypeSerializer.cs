using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Attributes;
using NonSucking.Framework.Serialization.Serializers;

using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(11)]
    public class DynamicTypeSerializer
    {
        private static bool TryGetAttribute(ITypeSymbol? symbol, out AttributeData? attr)
        {
            if (symbol == null)
            {
                attr = null;
                return false;
            }
            if (!symbol.TryGetAttribute(AttributeTemplates.DynamicType, out attr))
            {
                return TryGetAttribute(symbol.BaseType, out attr);
            }

            return true;
        }
        private static bool IsValidType(MemberInfo property, out IEnumerable<ITypeSymbol>? possibleTypes)
        {
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.DynamicType, out var attr))
            {
                if (!TryGetAttribute(property.TypeSymbol, out attr))
                {
                    possibleTypes = null;
                    return false;
                }
            }

            possibleTypes = attr!.ConstructorArguments[0].Values.Select(x => x.Value).OfType<ITypeSymbol>();
            return true;
        }
        private static ThrowStatementSyntax ThrowException(string exceptionName)
        {
            return SyntaxFactory.ThrowStatement(
                SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName(exceptionName))
                    .WithArgumentList(SyntaxFactory.ArgumentList()));
        }
        private static ThrowStatementSyntax ThrowNotSupportedException()
        {
            return ThrowException($"{nameof(System)}.{nameof(NotSupportedException)}");
        }
        private static ThrowStatementSyntax ThrowInvalidCastException()
        {
            return ThrowException($"{nameof(System)}.{nameof(InvalidCastException)}");
        }

        internal static Continuation TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!IsValidType(property, out var possibleTypes))
                return Continuation.NotExecuted;


            int typeId = 0;

            var switchSections = new List<SwitchSectionSyntax>();

            var castedName = Helper.GetRandomNameFor("casted", property.CreateUniqueName());

            foreach (var t in possibleTypes!)
            {
                typeId++;

                if (!IsAssignable(property.TypeSymbol, t))
                    continue;

                var newMemberInfo =
                    property with { Name = castedName, TypeSymbol = t.WithNullableAnnotation(property.TypeSymbol.NullableAnnotation), Parent = "" };

                var innerSerialize = NoosonGenerator.CreateStatementForSerializing(newMemberInfo, context, writerName, includedSerializers, SerializerMask.DynamicTypeSerializer);


                var invocationExpression
                    = Statement
                        .Expression
                        .Invoke(writerName, "Write", new[] { new ValueArgument(typeId) })
                        .AsStatement();

                var b = BodyGenerator.Create(innerSerialize.ToMergedBlock().Append(SyntaxFactory.BreakStatement()).Prepend(invocationExpression).ToArray());
                switchSections.Add(SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CasePatternSwitchLabel(
                            SyntaxFactory.DeclarationPattern(
                                SyntaxFactory.ParseTypeName(t.ToDisplayString()),
                                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(castedName)))
                            , SyntaxFactory.Token(SyntaxKind.ColonToken))),
                    SyntaxFactory.SingletonList<StatementSyntax>(b)
                ));

            }
            switchSections.Add(SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()),
                SyntaxFactory.SingletonList<StatementSyntax>(ThrowNotSupportedException())));

            statements.Statements.Add(SyntaxFactory.SwitchStatement(SyntaxFactory.IdentifierName(property.FullName))
                .WithSections(new SyntaxList<SwitchSectionSyntax>(
                    switchSections
                )));
            return Continuation.Done;
        }

        private static INamedTypeSymbol ResolveType(Compilation compilation, Type? type)
        {
            var fullName = type?.FullName;
            if (fullName is null)
                throw new ArgumentException();
            var resolvedType = compilation.GetTypeByMetadataName(fullName);
            if (resolvedType is null)
                throw new NotSupportedException($"Unresolvable types are not supported: Could not resolve '{fullName}'");

            return resolvedType;
        }

        private static bool IsAssignable(ITypeSymbol typeToAssignTo, ITypeSymbol? typeToAssignFrom)
        {
            if (typeToAssignFrom is null)
                return false;
            if (SymbolEqualityComparer.Default.Equals(typeToAssignFrom, typeToAssignTo))
                return true;
            var assignable = IsAssignable(typeToAssignTo, typeToAssignFrom.BaseType);
            if (!assignable && typeToAssignTo.IsAbstract)
                return typeToAssignFrom.Interfaces.Any(x => IsAssignable(typeToAssignTo, x));
            return assignable;
        }
        internal static Continuation TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!IsValidType(property, out var possibleTypes))
                return Continuation.NotExecuted;

            var invocationExpression
                = Statement
                    .Expression
                    .Invoke(readerName, "ReadInt32")
                    .AsExpression();
            var typeIdName = Helper.GetRandomNameFor("typeID", property.CreateUniqueName());
            statements.Statements.Add(Statement.Declaration.DeclareAndAssign(typeIdName, invocationExpression));

            var propName = property.CreateUniqueName();

            statements.DeclareAndAssign(property, propName, property.TypeSymbol, null);

            int typeId = 0;

            var switchSections = new List<SwitchSectionSyntax>();

            foreach (var t in possibleTypes!)
            {
                typeId++;

                if (!IsAssignable(property.TypeSymbol, t))
                {
                    switchSections.Add(SyntaxFactory.SwitchSection(
                        SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(typeId)))),
                        SyntaxFactory.SingletonList<StatementSyntax>(ThrowInvalidCastException())));
                    continue;
                }

                var newMemberInfo =
                    property with { TypeSymbol = t.WithNullableAnnotation(property.TypeSymbol.NullableAnnotation) };

                var innerDeserialize = NoosonGenerator.CreateStatementForDeserializing(newMemberInfo, context, readerName, includedSerializers, SerializerMask.DynamicTypeSerializer);

                var resValue = innerDeserialize.VariableDeclarations.Single();

                innerDeserialize.Statements.Add(Statement.Declaration.Assign(propName, SyntaxFactory.IdentifierName(resValue.UniqueName)));

                var b = BodyGenerator.Create(innerDeserialize.ToMergedBlock().Append(SyntaxFactory.BreakStatement()).ToArray());
                switchSections.Add(SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(typeId)))),
                        SyntaxFactory.SingletonList<StatementSyntax>(b)
                    ));
            }

            switchSections.Add(SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()),
                SyntaxFactory.SingletonList<StatementSyntax>(ThrowNotSupportedException())));

            statements.Statements.Add(SyntaxFactory.SwitchStatement(SyntaxFactory.IdentifierName(typeIdName))
                .WithSections(new SyntaxList<SwitchSectionSyntax>(
                    switchSections
                )));
            return Continuation.Done;
        }
    }
}