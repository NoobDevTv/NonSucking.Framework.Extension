using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Statements;
using System.Linq;
using System.Linq.Expressions;

namespace NonSucking.Framework.Serialization;

[StaticSerializer(1)]
internal static class TypeConverterSerializer
{
    internal static AttributeData? GetAttribute(MemberInfo property)
    {
        System.Collections.Immutable.ImmutableArray<AttributeData> attributes = property.Symbol.GetAttributes();
        return attributes
                .FirstOrDefault(d =>
                                    d.AttributeClass?.ToDisplayString() ==
                                    AttributeTemplates.NoosonConversion.FullName);
    }

    internal static ITypeSymbol? GetConverterType(AttributeData attr)
    {
        if (attr is not { ConstructorArguments.Length: 1 })
            return null;
        return attr.ConstructorArguments[0].Value as ITypeSymbol;
    }
    internal static ITypeSymbol? GetConverterTypeToConvertTo(AttributeData attr)
    {
        if (attr is not { NamedArguments.Length: > 0 })
            return null;
        return attr.NamedArguments
            .Select(x => (KeyValuePair<string, TypedConstant>?)x)
            .FirstOrDefault(x => x is not null && x.Value.Key == "ConvertTo")?.Value.Value as ITypeSymbol;
    }

    internal static bool CheckPrerequisites(MemberInfo property, NoosonGeneratorContext context, out ITypeSymbol conversionType, out ITypeSymbol? genericConverter)
    {
        var attribute = GetAttribute(property);

        genericConverter = null;
        conversionType = null!;
        if (attribute is null)
        {
            return false;
        }

        var tempConversionType = GetConverterType(attribute);
        conversionType = tempConversionType
                         ?? throw new InvalidOperationException("Converter attribute found but, converter type was not set. This should never happen.");

        var genericConverterInterfaces = conversionType.AllInterfaces.Where(x => x.IsGenericType
                                                                               && x.OriginalDefinition.ToDisplayString() == "NonSucking.Framework.Serialization.INoosonConverter<, >").ToArray();
        if (genericConverterInterfaces.Length > 0)
        {
            if (!Helper.CheckSingleton(context, conversionType))
            {
                context.AddDiagnostic(Diagnostics.SingletonImplementationRequired.Format("type converters"), conversionType, DiagnosticSeverity.Error);
                return false;
            }

            var matchingInterfaces = genericConverterInterfaces.Select(x =>
                                                    Helper.IsAssignable(x.TypeArguments[0], property.TypeSymbol)
                                                      ? (otherIndex: 1, genericConverterInterface: x)
                                                      : Helper.IsAssignable(x.TypeArguments[1], property.TypeSymbol)
                                                          ? (otherIndex: 1, genericConverterInterface: x)
                                                          : (otherIndex: -1, genericConverterInterface: null!)).Where(x => x.otherIndex != -1).ToArray();

            genericConverter = conversionType;

            static ITypeSymbol GetConversionType((int otherIndex, INamedTypeSymbol genericConverterInterface) t)
            {
                return t.genericConverterInterface
                    .TypeArguments[t.otherIndex];
            }
            if (matchingInterfaces.Length == 1)
            {
                conversionType = GetConversionType(matchingInterfaces[0]);
            }
            else if (matchingInterfaces.Length > 0 && GetConverterTypeToConvertTo(attribute) is {} destType)
            {
                var matchingInterface = matchingInterfaces.FirstOrDefault(
                    x => SymbolEqualityComparer.Default.Equals(GetConversionType(x),
                        destType));
                if (matchingInterface.genericConverterInterface is null)
                {
                    context.AddDiagnostic(Diagnostics.NoValidConverter.Format("to", destType.ToDisplayString()), genericConverter, DiagnosticSeverity.Error);
                    return false;
                }

                conversionType = GetConversionType(matchingInterface);
            }
            else
            {
                if (matchingInterfaces.Length == 0)
                {
                    context.AddDiagnostic(Diagnostics.NoValidConverter.Format("from", property.TypeSymbol.ToDisplayString()), genericConverter, DiagnosticSeverity.Error);
                }
                else
                {
                    context.AddDiagnostic(
                        Diagnostics.ConverterConvertToNeeded,
                        attribute.ApplicationSyntaxReference?.GetLocation() ?? genericConverter.GetLocation() ?? Location.None,
                        DiagnosticSeverity.Error);
                }
                return false;
            }
        }

        return true;
    }
    internal static ExpressionStatementSyntax GenerateConversionCall(ITypeSymbol genericConverter, string from, string to)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(genericConverter.ToDisplayString()),
                    SyntaxFactory.IdentifierName("Instance")),
                SyntaxFactory.IdentifierName("TryConvert")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new []{
                 SyntaxFactory.Argument(SyntaxFactory.IdentifierName(from)),
                 SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), SyntaxFactory.IdentifierName(to))
             }))));
    }
    internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        if (!CheckPrerequisites(property, context, out var conversionType, out var genericConverter))
            return Continuation.NotExecuted;

        var destType = SyntaxFactory.ParseTypeName(conversionType.ToDisplayString());
        var propName = Helper.GetRandomNameFor(property.Name, property.Parent);
        if (genericConverter is not null)
        {
            var tempVariable = Helper.GetMemberAccessString(property);
            statements.Statements.Add(Statement.Declaration.Declare(propName, destType));
            var conversionCall = GenerateConversionCall(genericConverter, tempVariable, propName);
            statements.Statements.Add(conversionCall);
        }
        else
        {
            statements.Statements.Add(Statement.Declaration.DeclareAndAssign(propName, SyntaxFactory.IdentifierName(Helper.GetMemberAccessString(property)),
                destType, destType
            ));
        }

        property = new MemberInfo(conversionType, property.Symbol, propName);

        includedSerializers &= ~SerializerMask.TypeConverterSerializer;

        return Continuation.Retry;
    }
    internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        if (!CheckPrerequisites(property, context, out var conversionType, out var isGenericConverter))
            return Continuation.NotExecuted;

        property = new MemberInfo(conversionType, property.Symbol, property.Name, property.Parent);

        includedSerializers &= ~SerializerMask.TypeConverterSerializer;

        return Continuation.Retry;
    }
}

[StaticSerializer(1, IsFinalizer = true)]
internal static class TypeConverterCompletionSerializer
{
    internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context,
        string writerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        return Continuation.NotExecuted;
    }

    private static ITypeSymbol GetType(ISymbol symbol)
    {
        if (symbol is IPropertySymbol prop)
            return prop.Type;
        if (symbol is IFieldSymbol field)
            return field.Type;
        throw new ArgumentException();
    }
    internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context,
        string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        if ((includedSerializers & SerializerMask.TypeConverterSerializer) != SerializerMask.None)
            return Continuation.NotExecuted;
        if (!TypeConverterSerializer.CheckPrerequisites(property, context, out var conversionType, out var genericConverter))
            return Continuation.NotExecuted;

        conversionType = GetType(property.Symbol);
        var deserializedValueToConvert = statements.VariableDeclarations.Single();
        var destType = SyntaxFactory.ParseTypeName(conversionType.ToDisplayString());
        statements.Merge();
        var newVariable = new GeneratedSerializerCode.SerializerVariable(
            destType,
            property,
            Helper.GetRandomNameFor(property.Symbol.Name, property.Parent), null
        );
        if (genericConverter is not null)
        {
            var conversionCall = TypeConverterSerializer.GenerateConversionCall(genericConverter, deserializedValueToConvert.UniqueName, newVariable.UniqueName);
            statements.Statements.Add(conversionCall);
        }
        else
        {
            statements.Statements.Add(Statement.Declaration.Assign(newVariable.UniqueName,
                SyntaxFactory.IdentifierName(deserializedValueToConvert.UniqueName), destType));
        }


        statements.VariableDeclarations.Add(newVariable);

        property = new MemberInfo(conversionType, property.Symbol, property.Name, property.Parent);

        return Continuation.Done;
    }
}