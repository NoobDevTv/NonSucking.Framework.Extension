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

    [Flags]
    internal enum Initializer
    {
        None = 0,
        Ctor = 1 << 0,
        /// <summary>
        /// Will enable <see cref="Ctor"/> aswell
        /// </summary>
        InitializerList = (1 << 1) | Ctor,
        Properties = 1 << 2,
    }

    internal static class CtorSerializer
    {
        
        internal static GeneratedSerializerCode CallCtorAndSetProps(INamedTypeSymbol typeSymbol, GeneratedSerializerCode localVariableNames, MemberInfo instance, string instanceName, Initializer initializer)
        {
            var localDeclarations = GetLocalDeclarations(localVariableNames, instanceName);
            if (instanceName != Consts.InstanceParameterName && (typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.IsAbstract))
            {
                var r = new GeneratedSerializerCode();

                r.DeclareAndAssign(instance, instanceName, instance.TypeSymbol, SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString()))));

                return r;
            }

            var ret = new GeneratedSerializerCode();
            List<string> ctorArgumentNames = new();
            ArgumentListSyntax? ctorArguments = null;
            if ((initializer & Initializer.Ctor) > 0)
            {
                List<IMethodSymbol> constructors = GetCtors(typeSymbol);
                ctorArguments
                    = GetStatementForCtorCall(constructors, localDeclarations, out ctorArgumentNames);
            }


            InitializerExpressionSyntax? propertyAssignments = null;
            if ((initializer & Initializer.InitializerList) > 0)
            {
                propertyAssignments = AssignMissingInitializerProperties(typeSymbol, localDeclarations, ctorArgumentNames);
            }

            if ((initializer & Initializer.Properties) > 0)
            {
                var assignemntExpressions
                = AssignMissingSetterProperties(instanceName, typeSymbol, localDeclarations, ctorArgumentNames);
                ret.Statements.AddRange(assignemntExpressions);

            }

            if ((initializer & Initializer.Ctor) > 0)
            {

                var currentType
                = SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString());
                var ctorCallStatement = DeclareAssignCtor(currentType, instance, instanceName, ctorArguments!, propertyAssignments);
                ret.VariableDeclarations.Add(ctorCallStatement);
            }

            return ret;
        }

        private static List<IMethodSymbol> GetCtors(INamedTypeSymbol typeSymbol)
        {
            var constructors
                = typeSymbol
                .Constructors
                .OrderByDescending(constructor =>
                    (constructor.TryGetAttribute(AttributeTemplates.PreferredCtor, out _) ? 0xFFFF1 : 0) //0xFFFF is the maximum amount of Parameters, so we add an additional one
                    + constructor.Parameters.Length)
                .ToList();
            return constructors;
        }

        private static List<string> GetLocalDeclarations(GeneratedSerializerCode localVariableNames, string instanceName)
        {
            var indexOf = instanceName.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
            IEnumerable<GeneratedSerializerCode.SerializerVariable> vars;
            if (indexOf < 0)
            {
                vars = localVariableNames.VariableDeclarations;
            }
            else
            {
                var shouldContain = instanceName.Substring(0, indexOf);

                vars
                    = localVariableNames.VariableDeclarations
                        .Where(v =>
                               {
                                   var text = v.UniqueName;
                                   int firstIndex = text.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
                                   if (firstIndex == -1)
                                       return false;
                                   firstIndex += Consts.LocalVariableSuffix.Length;
                                   int secondIndex = text.IndexOf(Consts.LocalVariableSuffix, firstIndex, StringComparison.Ordinal);
                                   if (secondIndex == -1)
                                       return false;
                                   if (text.Substring(firstIndex, secondIndex - firstIndex) != shouldContain)
                                       return false;
                                   return true;
                               })
                        .ToList();
            }
            return vars.Select(x => x.UniqueName).ToList();
        }

        internal static List<ExpressionStatementSyntax> AssignMissingSetterProperties(string instanceName, ITypeSymbol typeSymbol, List<string> localDeclarations,
            List<string> ctorArguments)
        {
            List<(ITypeSymbol type, ISymbol symbol)> properties = GetPropertiesForDeclaration(typeSymbol, ref localDeclarations, ctorArguments, false);

            var assignments = new List<ExpressionStatementSyntax>();

            foreach (var property in properties)
            {
                var declaration
                    = localDeclarations
                    .FirstOrDefault(declaration => Helper.MatchIdentifierWithPropName(property.symbol.Name, declaration));

                if (declaration is null)
                {
                    continue;
                }


                VariableReference declarationReference = new VariableReference(declaration);
                VariableReference memberReference = new VariableReference(instanceName, new MemberReference(property.symbol.Name));

                assignments.Add(Statement.Declaration.Assign(memberReference, declarationReference));
            }

            return assignments;
        }


        internal static InitializerExpressionSyntax? AssignMissingInitializerProperties(ITypeSymbol typeSymbol, List<string> localDeclarations,
            List<string> ctorArguments)
        {
            List<(ITypeSymbol type, ISymbol symbol)> properties = GetPropertiesForDeclaration(typeSymbol, ref localDeclarations, ctorArguments);

            var initializers = new List<ExpressionSyntax>();

            foreach (var property in properties)
            {
                var declaration
                    = localDeclarations
                    .FirstOrDefault(declaration => Helper.MatchIdentifierWithPropName(property.symbol.Name, declaration));

                if (declaration is null)
                {
                    continue;
                }

                VariableReference declarationReference = new VariableReference(declaration);

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

        private static List<(ITypeSymbol type, ISymbol symbol)> GetPropertiesForDeclaration(ITypeSymbol typeSymbol, ref List<string> localDeclarations, List<string> ctorArguments, bool includeInitOnly = true)
        {
            localDeclarations
                = localDeclarations
                .Where(declaration =>
                    !ctorArguments.Any(argument => Helper.MatchIdentifierWithPropName(argument, declaration))
                )
                .ToList();

            List<(ITypeSymbol type, ISymbol symbol)> properties = new();

            foreach ((MemberInfo item, int _) in Helper.GetMembersWithBase(typeSymbol))
            {
                if (item.Symbol.IsStatic)
                    continue;

                if (item.Symbol is IPropertySymbol property
                    && !property.IsReadOnly
                    && property.SetMethod is IMethodSymbol sm
                    && (includeInitOnly || !sm.IsInitOnly)
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

            return properties;
        }

        internal static ArgumentListSyntax GetStatementForCtorCall(List<IMethodSymbol> constructors, List<string> localDeclarations, out List<string> ctorArguments)
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
                        parameterName = (string)newNameAttribute!.ConstructorArguments[0].Value!;
                    }

                    var matchedDeclaration
                        = localDeclarations
                        .FirstOrDefault(identifier => Helper.MatchIdentifierWithPropName(parameterName, identifier));

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

        internal static GeneratedSerializerCode.SerializerVariable DeclareAssignCtor(TypeSyntax currentType, MemberInfo instance, string instanceName, ArgumentListSyntax arguments, InitializerExpressionSyntax? initializer)
        {
            return new GeneratedSerializerCode.SerializerVariable(
                 currentType,
                instance, instanceName,
                SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(currentType)
                    .WithArgumentList(arguments).WithInitializer(initializer).WithNewKeyword(SyntaxFactory.Token(SyntaxKind.NewKeyword))),
                 false
                );
        }

    }
}
