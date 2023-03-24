using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NonSucking.Framework.Serialization.Serializers;

using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization;

[StaticSerializer(31)]
internal static class UnmanagedTypeSerializer
{
    private static bool IsManagedType(ITypeSymbol typeSymbol)
    {
        return !typeSymbol.IsUnmanagedType
               || typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Any(IsManagedType);
    }
    private static InvocationExpressionSyntax GetGenericMethodSyntax(string variable, string methodName, ITypeSymbol type)
    {
        var method = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier(methodName),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    SyntaxFactory.IdentifierName(type.ToDisplayString()))));
        var access = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(variable), method);
        return SyntaxFactory.InvocationExpression(access);
    }

    internal static Continuation TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,
        GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        var typeSymbol = property.TypeSymbol;
        if (IsManagedType(typeSymbol))
        {
            return Continuation.NotExecuted;
        }

        context.GeneratedFile.Usings.Add(context.GlobalContext.Config.GeneratedNamespace);

        var argument = Helper.GetMemberAccessString(property);
        var writeMethod = GetGenericMethodSyntax(writerName, "WriteUnmanaged", typeSymbol);
        var invocation = writeMethod
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(argument)))));

        statements.Statements.Add(SyntaxFactory.ExpressionStatement(invocation));
        return Continuation.Done;
    }

    internal static Continuation TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
        GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
    {
        var typeSymbol = property.TypeSymbol;
        if (IsManagedType(typeSymbol))
        {
            return Continuation.NotExecuted;
        }

        context.GeneratedFile.Usings.Add(context.GlobalContext.Config.GeneratedNamespace);

        var readMethod = GetGenericMethodSyntax(readerName, "ReadUnmanaged", typeSymbol);

        statements.DeclareAndAssign(property, property.CreateUniqueName(), typeSymbol, readMethod);
        return Continuation.Done;
    }
}