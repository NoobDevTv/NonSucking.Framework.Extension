using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization;

[StaticSerializer(30)]
internal static class KnownSimpleTypeSerializer
{
    enum KnownTypes
    {
        None,
        IPAddress,
        Guid,
        BigInteger
    }

    private static string AddBuffer(GeneratedSerializerCode statements, string propName, ExpressionSyntax size)
    {
        var newBufferStatement =
            SyntaxFactory.StackAllocArrayCreationExpression(
                SyntaxFactory
                    .ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList(size)))));
        var bufferName = Helper.GetRandomNameFor("buffer", propName);
        statements.Statements.Add(Statement
            .Declaration
            .DeclareAndAssign(bufferName,
                newBufferStatement,
                SyntaxFactory.ParseName("System.Span<byte>")));
        return bufferName;
    }
    private static SwitchExpressionSyntax CreateIpSize(ExpressionSyntax addrFamily)
    {
        var addressFamilyEnum =
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("Net")),
                    SyntaxFactory.IdentifierName("Sockets")),
                SyntaxFactory.IdentifierName("AddressFamily"));
        return SyntaxFactory.SwitchExpression(addrFamily)
            .WithArms(
                SyntaxFactory.SeparatedList<SwitchExpressionArmSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.ConstantPattern(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    addressFamilyEnum,
                                    SyntaxFactory.IdentifierName("InterNetworkV6"))
                            ),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(16))),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.ConstantPattern(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    addressFamilyEnum,
                                    SyntaxFactory.IdentifierName("InterNetwork"))
                            ),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(4))),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.DiscardPattern(),
                            SyntaxFactory.ThrowExpression(
                                SyntaxFactory.ObjectCreationExpression(
                                        SyntaxFactory.ParseTypeName("System.NotSupportedException"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))
                    }
                ));
    }

    private static ExpressionSyntax CreateBigIntSize(string propName)
    {
        return Statement
            .Expression
            .Invoke(propName, "GetByteCount").AsExpression();

    }
    private static KnownTypes GetKnownType(ISymbol typeSymbol)
    {
        if (typeSymbol.ContainingNamespace.ToDisplayString() == "System.Net"
            && typeSymbol.Name == nameof(System.Net.IPAddress))
        {
            return KnownTypes.IPAddress;
        }
        if (typeSymbol.ContainingNamespace.ToDisplayString() == "System"
            && typeSymbol.Name == nameof(System.Guid))
        {
            return KnownTypes.Guid;
        }
        if (typeSymbol.ContainingNamespace.ToDisplayString() == "System.Numerics"
            && typeSymbol.Name == nameof(System.Numerics.BigInteger))
        {
            return KnownTypes.BigInteger;
        }
        return KnownTypes.None;
    }

    internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,
        GeneratedSerializerCode statements)
    {
        var type = property.TypeSymbol;
        if (type is INamedTypeSymbol typeSymbol)
        {
            var propName = property.FullName;
            string? bufferName = null;
            bool hasOutSize = false;
            switch (GetKnownType(type))
            {
                case KnownTypes.IPAddress:
                    hasOutSize = true;
                    var addressFamilyProperty = property.TypeSymbol.GetMembers("AddressFamily").First();

                    var addressFamilyProp =
                        new MemberInfo(((IPropertySymbol)addressFamilyProperty).Type,
                            addressFamilyProperty,
                            "AddressFamily",
                            propName);

                    var addressFamilySer =
                        NoosonGenerator.GenerateStatementsForProps(
                            new[] { addressFamilyProp }, context, MethodType.Serialize);
                    foreach (var item in addressFamilySer)
                    {
                        statements.MergeWith(item);
                    }

                    bufferName = AddBuffer(statements,
                        propName,
                        CreateIpSize(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(propName),
                        SyntaxFactory.IdentifierName("AddressFamily"))));

                    break;
                case KnownTypes.Guid:
                    bufferName = AddBuffer(statements,
                        propName,
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(16)));
                    break;
                case KnownTypes.BigInteger:
                    hasOutSize = true;
                    var bigIntSize = CreateBigIntSize(propName);
                    var sizeName = Helper.GetRandomNameFor("size", propName);
                    var bigIntSizeVar = Statement.Declaration.DeclareAndAssign(sizeName, bigIntSize);
                    statements.Statements.Add(bigIntSizeVar);
                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { new VariableArgument(sizeName) })
                        .AsStatement());

                    bufferName = AddBuffer(statements, propName, SyntaxFactory.IdentifierName(sizeName));
                    break;
                default:
                    return false;
            }

            var tryWriteParams = new List<SyntaxNodeOrToken>
            {
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(bufferName))
            };

            if (hasOutSize)
            {
                tryWriteParams.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                tryWriteParams.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_"))
                    .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword)));
            }

            statements.Statements.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("_"),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(propName),
                                SyntaxFactory.IdentifierName("TryWriteBytes")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                    tryWriteParams))))));
            statements.Statements.Add(Statement
                .Expression
                .Invoke(writerName, "Write", arguments: new[] { new VariableArgument(bufferName) })
                .AsStatement());
            return true;
        }

        return false;
    }

    internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
        GeneratedSerializerCode statements)
    {
        var type = property.TypeSymbol;

        if (type is INamedTypeSymbol typeSymbol)
        {
            var propName = property.CreateUniqueName();
            string? bufferName = null;
            switch (GetKnownType(type))
            {
                case KnownTypes.IPAddress:
                    var addressFamilyProperty = property.TypeSymbol.GetMembers("AddressFamily").First();

                    var addressFamilyProp =
                        new MemberInfo(
                            ((IPropertySymbol)addressFamilyProperty).Type,
                            addressFamilyProperty,
                            "AddressFamily",
                            propName);

                    var addressFamilySer =
                        NoosonGenerator.GenerateStatementsForProps(
                            new[] { addressFamilyProp }, context, MethodType.Deserialize).Single();
                    statements.Statements.AddRange(addressFamilySer.ToMergedBlock());

                    var addrFamilyName =
                        addressFamilySer.VariableDeclarations.First(x => x.OriginalMember == addressFamilyProp).UniqueName;

                    bufferName = AddBuffer(statements, propName, CreateIpSize(SyntaxFactory.IdentifierName(addrFamilyName)));

                    break;
                case KnownTypes.Guid:
                    bufferName = AddBuffer(statements, propName, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(16)));
                    break;
                case KnownTypes.BigInteger:
                    var sizeName = Helper.GetRandomNameFor("size", propName);
                    var bigIntSize = Statement
                        .Expression
                        .Invoke(readerName, "ReadInt32")
                        .AsExpression();
                    var bigIntSizeVar = Statement.Declaration.DeclareAndAssign(sizeName, bigIntSize);
                    statements.Statements.Add(bigIntSizeVar);
                    bufferName = AddBuffer(statements, propName, SyntaxFactory.IdentifierName(sizeName));
                    break;
                default:
                    return false;
            }

            context.Usings.Add("NonSucking.Framework.Serialization");
            statements.Statements.Add(Statement
                .Expression
                .Invoke(readerName, "ReadBytes", arguments: new[] { new VariableArgument(bufferName) })
                .AsStatement());

            var createObj =
                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString()),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                        new[] { SyntaxFactory.Argument(SyntaxFactory.IdentifierName(bufferName)) })),
                    null);

            statements.DeclareAndAssign(property, propName, typeSymbol, createObj);
            return true;
        }
        return false;
    }
}