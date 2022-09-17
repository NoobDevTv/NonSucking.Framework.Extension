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
    
    internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,
        GeneratedSerializerCode statements, SerializerMask includedSerializers)
    {
        var typeSymbol = property.TypeSymbol;
        if (!typeSymbol.IsUnmanagedType)
        {
            return false;
        }
        
        
        context.Usings.Add("NonSucking.Framework.Serialization");

        var argument = Helper.GetMemberAccessString(property);
        var writeMethod = GetGenericMethodSyntax(writerName, "WriteUnmanaged", typeSymbol);
        var invocation = writeMethod
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(argument)))));
        
        statements.Statements.Add(SyntaxFactory.ExpressionStatement(invocation));
        return true;
    }

    internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,
        GeneratedSerializerCode statements, SerializerMask includedSerializers)
    {
        var typeSymbol = property.TypeSymbol;
        if (!typeSymbol.IsUnmanagedType)
        {
            return false;
        }
        
        
        context.Usings.Add("NonSucking.Framework.Serialization");

        var readMethod = GetGenericMethodSyntax(readerName, "ReadUnmanaged", typeSymbol);

        statements.DeclareAndAssign(property, property.CreateUniqueName(), typeSymbol, readMethod);
        return true;
    }
}