using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VaVare.Generators.Common;
using VaVare.Models.References;

using VaVare.Statements;

namespace NonSucking.Framework.Serialization.Serializers
{
    internal static class CtorSerializer
    {
        internal static GeneratedSerializerCode CallCtorAndSetProps(INamedTypeSymbol typeSymbol, ICollection<StatementSyntax> statements, MemberInfo instance, string instanceName)
        {
            var constructors
                = typeSymbol
                .Constructors
                .OrderByDescending(constructor =>
                    (constructor.TryGetAttribute(AttributeTemplates.PreferredCtor, out _) ? 0xFFFF1 : 0) //0xFFFF is the maximum amount of Parameters, so we add an additional one
                    + constructor.Parameters.Length)
                .ToList();

            var shouldContain = instanceName.Substring(0, instanceName.IndexOf(Helper.localVariableSuffix));

            var localDeclarations
                = statements
                .OfType<LocalDeclarationStatementSyntax>()
                .Concat(
                    statements
                    .OfType<BlockSyntax>()
                    .SelectMany(x => x.Statements.OfType<LocalDeclarationStatementSyntax>())
                 )
                .SelectMany(declaration => declaration.Declaration.Variables)
                .Select(variable => variable.Identifier.Text)
                .Where(text =>
                {
                    int firstIndex = text.IndexOf(Helper.localVariableSuffix);
                    if(firstIndex == -1)
                        return false;
                    firstIndex += +Helper.localVariableSuffix.Length;
                    int secondIndex = text.IndexOf(Helper.localVariableSuffix, firstIndex);
                    if (secondIndex == -1)
                        return false;
                    if (text.Substring(firstIndex, secondIndex - firstIndex) != shouldContain)
                        return false;
                    return true;
                })
                .ToList();

            var currentType
                = SyntaxFactory
                .ParseTypeName(typeSymbol.ToDisplayString());


            if (typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.IsAbstract)
            {
                var r = new GeneratedSerializerCode();
                r.DeclareAndAssign(instance, instanceName, instance.TypeSymbol, SyntaxFactory.ParseExpression("default"));
                return r;
            }

            var ctorArguments
                = GetStatementForCtorCall(constructors, localDeclarations, currentType, instance, instanceName, out var ctorArgumentNames);

            var propertyAssignments
                = AssignMissingSetterProperties(typeSymbol, localDeclarations, ctorArgumentNames);

            var ctorCallStatement = DeclareAssignCtor(currentType, instance, instanceName, ctorArguments, propertyAssignments);

            var ret = new GeneratedSerializerCode();
            
            ret.VariableDeclarations.Add(ctorCallStatement);
            //ret.Statements.AddRange(propertyAssignments);
            return ret;
        }


        internal static InitializerExpressionSyntax AssignMissingSetterProperties(ITypeSymbol typeSymbol, List<string> localDeclarations,
            List<string> ctorArguments)
        {

            //TODO Set Public props which have a set method via !IPropertySymbol.IsReadOnly
            localDeclarations
                = localDeclarations
                .Where(declaration =>
                    !ctorArguments.Any(argument => Helper.MatchIdentifierWithPropName(argument, declaration))
                )
                .ToList();

            List<(ITypeSymbol type, ISymbol symbol)> properties = new();

            foreach (var item in Helper.GetMembersWithBase(typeSymbol))
            {
                if (item.Symbol is IPropertySymbol property
                    && !property.IsReadOnly
                    && property.SetMethod is not null
                    && !ctorArguments.Any(argument => Helper.MatchIdentifierWithPropName(argument, property.Name)))
                {
                    properties.Add((property.Type, property));
                }
                else if (item.Symbol is IFieldSymbol field
                     && !field.IsReadOnly
                     && !ctorArguments.Any(argument => Helper.MatchIdentifierWithPropName(argument, field.Name)))
                {
                    properties.Add((field.Type, field));

                }
            }

            var initializers = new List<ExpressionSyntax>();

            foreach (var property in properties)
            {
                var declaration
                    = localDeclarations
                    .FirstOrDefault(declaration => Helper.MatchIdentifierWithPropName(declaration, property.symbol.Name));

                if (declaration is null)
                {
                    continue;
                }

                VariableReference declarationReference;

                //if (property.type.TypeKind == TypeKind.Array)
                //{
                //    var method = new MethodReference("ToArray");
                //    declarationReference = new MemberReference(declaration, method);
                //}
                //else
                //{
                    declarationReference = new VariableReference(declaration);
                //}

                var assignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(property.symbol.Name),
                    ReferenceGenerator.Create(declarationReference)).WithLeadingTrivia(SyntaxFactory.LineFeed);

                initializers.Add(assignmentExpression);
            }

            if (initializers.Count == 0)
                return null;

            var objectInitializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList(initializers));

            return objectInitializer;
        }

        internal static ArgumentListSyntax GetStatementForCtorCall(List<IMethodSymbol> constructors, List<string> localDeclarations, TypeSyntax currentType, MemberInfo instance, string instanceName, out List<string> ctorArguments)
        {
            ctorArguments = new List<string>();
            if (constructors.Count == 0)
            {
                return SyntaxFactory.ArgumentList();
            }

            foreach (var constructor in constructors)
            {
                bool constructorMatch = true;

                foreach (var parameter in constructor.Parameters)
                {
                    var parameterName = parameter.Name;

                    if (parameter.TryGetAttribute(AttributeTemplates.Parameter, out var newNameAttribute))
                    {
                        parameterName = newNameAttribute.ConstructorArguments[0].Value as string;
                    }

                    var matchedDeclaration
                        = localDeclarations
                        .FirstOrDefault(identifier => Helper.MatchIdentifierWithPropName(identifier, parameterName));

                    if (string.IsNullOrWhiteSpace(matchedDeclaration))
                    {
                        constructorMatch = false;
                        break;
                    }

                    ctorArguments.Add(matchedDeclaration);
                }


                if (constructorMatch)
                {
                    var ctorArgumentsString
                        = string.Join(",", ctorArguments);

                    var arguments
                        = SyntaxFactory
                        .ParseArgumentList($"({ctorArgumentsString})");

                    return arguments;

                }
                else
                {
                    ctorArguments.Clear();
                }
            }

            throw new NotSupportedException();
        }

        internal static GeneratedSerializerCode.SerializerVariable DeclareAssignCtor(TypeSyntax currentType, MemberInfo instance, string instanceName, ArgumentListSyntax arguments, InitializerExpressionSyntax initializer)
        {
            return new GeneratedSerializerCode.SerializerVariable(
                Statement.Declaration.Declare(instanceName, currentType),
                instance, instanceName,
                SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(currentType)
                    .WithArgumentList(arguments).WithInitializer(initializer).WithNewKeyword(SyntaxFactory.Token(SyntaxKind.NewKeyword)))
                );
        }
    }
}
