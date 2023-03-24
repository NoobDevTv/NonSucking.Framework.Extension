using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using VaVare.Statements;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using static NonSucking.Framework.Serialization.NoosonGenerator;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(-1)]
    internal static class VersioningSerializer
    {
        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.Versioning, out var versioningAttribute))
                return Continuation.NotExecuted;

            var methodName = versioningAttribute!.ConstructorArguments[0].Value!.ToString();

            var variableMappings = property.ScopeVariableNameMappings;
            var parameters = versioningAttribute.ConstructorArguments[2].Values
                .Select(x => variableMappings![x.Value!.ToString()]);

            var isNotNullCheck = Statement.Expression.Invoke(
                    methodName,
                    arguments: parameters
                        .Select(x =>
                            new ValueArgument((object) x))
                        .ToArray())
                .AsExpression();

            var m = new MemberInfo(property.TypeSymbol, property.Symbol, property.Name,
                property.Parent);
            var innerSerialize = NoosonGenerator.CreateStatementForSerializing(m, context, readerName,
                includedSerializers, SerializerMask.VersioningSerializer);

            var b = BodyGenerator.Create(innerSerialize.ToMergedBlock().ToArray());

            statements.Statements.Add(SyntaxFactory.IfStatement(isNotNullCheck, b));


            return Continuation.Done;
        }
        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers,
            int baseTypesLevelProperties = int.MaxValue)
        {
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.Versioning, out var versioningAttribute))
                return Continuation.NotExecuted;


            var elementType = property.TypeSymbol;
            var m = new MemberInfo(elementType, property.Symbol, property.Name, property.Parent);

            var innerDeserialize = CreateStatementForDeserializing(m, context, readerName, includedSerializers,
                SerializerMask.VersioningSerializer);
            
            IEnumerable<StatementSyntax> innerStatements = innerDeserialize.MergeBlocksSeperated(statements);
            
            var b = BodyGenerator.Create(innerStatements.ToArray());

            var methodName = versioningAttribute!.ConstructorArguments[0].Value!.ToString();

            var variableMappings = property.ScopeVariableNameMappings;
            var parameters = versioningAttribute.ConstructorArguments[2].Values
                .Select(x => variableMappings![x.Value!.ToString()]);

            var isNotNullCheck = Statement.Expression.Invoke(
                    methodName,
                    arguments: parameters
                        .Select(x =>
                            new ValueArgument((object) x))
                        .ToArray())
                .AsExpression();

            var ifClause = SyntaxFactory.IfStatement(isNotNullCheck, b);
            var defaultExpr = versioningAttribute!.ConstructorArguments[1].Value!.ToString();

            if (!string.IsNullOrWhiteSpace(defaultExpr))
            {
                var assignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(innerDeserialize.VariableDeclarations.First().UniqueName),
                    SyntaxFactory.ParseExpression(defaultExpr)).WithLeadingTrivia(SyntaxFactory.LineFeed);

                ifClause = ifClause.WithElse(
                    
                    SyntaxFactory.ElseClause(SyntaxFactory.ExpressionStatement(assignmentExpression)));
            }

            statements.Statements.Add(ifClause);
            return Continuation.Done;
        }
    }
}