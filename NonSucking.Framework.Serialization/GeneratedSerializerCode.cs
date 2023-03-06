using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization;

public class GeneratedSerializerCode
{
    public List<SerializerVariable> VariableDeclarations { get; } = new();
    public List<StatementSyntax> Statements { get; } = new();

    public IEnumerable<StatementSyntax> ToMergedBlock()
    {
        foreach (var declaration in VariableDeclarations)
        {
            if (declaration.UniqueName != Consts.InstanceParameterName)
                yield return declaration.ToDeclarationAndAssignment();
        }

        foreach (var statement in Statements)
        {
            yield return statement;
        }
    }

    public IEnumerable<StatementSyntax> MergeBlocksSeperated(GeneratedSerializerCode other,
        Func<SerializerVariable, TypeSyntax>? variableTransformer = null)
    {
        foreach (var declaration in VariableDeclarations)
        {
            var newDeclaration = variableTransformer is null
                                    ? declaration.TypeSyntax
                                    : variableTransformer(declaration);

            other.VariableDeclarations.Add(new SerializerVariable(newDeclaration, declaration.OriginalMember, declaration.UniqueName, null));
        }
        return VariableDeclarations.Where(x => x.InitialValue is not null).Select(x => x.GetAssignment()).Concat(Statements);
    }

    public void MergeWith(GeneratedSerializerCode other, bool emitVariables = true)
    {
        if (Statements.Count == 0)
        {
            foreach (var d in other.VariableDeclarations)
                VariableDeclarations.Add(d);
        }
        else if (emitVariables)
        {
            foreach (var d in other.VariableDeclarations)
            {
                VariableDeclarations.Add(new SerializerVariable(d.TypeSyntax, d.OriginalMember, d.UniqueName, null));
                Statements.Add(d.GetAssignment());
            }
        }
        else
        {
            foreach (var d in other.VariableDeclarations)
            {
                Statements.Add(d.ToDeclarationAndAssignment());
            }
        }
        foreach (var s in other.Statements)
            Statements.Add(s);
    }


    public void DeclareAndAssign(MemberInfo member, string memberName, ITypeSymbol type, ExpressionSyntax? valueExpression)
    {
        var typeSyntax = SyntaxFactory.ParseTypeName(type.ToDisplayString()); // TODO better type handling?
        DeclareAndAssign(member, memberName, typeSyntax, valueExpression);
    }
    public void DeclareAndAssign(MemberInfo member, string memberName, TypeSyntax typeSyntax, ExpressionSyntax? valueExpression)
    {
        if (Statements.Count == 0)
        {
            VariableDeclarations.Add(
                new SerializerVariable(typeSyntax, member, memberName,
                        valueExpression is null ? null : SyntaxFactory.EqualsValueClause(valueExpression)
                    )
                );
        }
        else
        {
            VariableDeclarations.Add(new SerializerVariable(typeSyntax, member, memberName, null));
            if (valueExpression is not null)
                Statements.Add(Statement.Declaration.Assign(memberName, valueExpression));
        }
    }

    public void Clear()
    {
        VariableDeclarations.Clear();
        Statements.Clear();
    }

    public readonly struct SerializerVariable
    {
        public SerializerVariable(TypeSyntax typeSyntax, MemberInfo originalMember, string uniqueName, EqualsValueClauseSyntax? initialValue)
        {
            Declaration = Statement.Declaration.Declare(uniqueName, typeSyntax);
            TypeSyntax = typeSyntax;
            OriginalMember = originalMember;
            UniqueName = uniqueName;
            InitialValue = initialValue;
        }

        public LocalDeclarationStatementSyntax Declaration { get; }
        public TypeSyntax TypeSyntax { get; }
        public MemberInfo OriginalMember { get; }
        public string UniqueName { get; }
        public EqualsValueClauseSyntax? InitialValue { get; }

        public EqualsValueClauseSyntax CreateDefaultValue()
        {
            return SyntaxFactory.EqualsValueClause(SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, SyntaxFactory.DefaultExpression(Declaration.Declaration.Type)));
        }

        public StatementSyntax GetAssignment()
        {
            var variables = Declaration.Declaration.Variables;
            if (variables.Count > 1)
            {
                throw new NotSupportedException("Multiple declaration and assignments in a single statement not supported.");
            }

            return Statement.Declaration.Assign(variables[0].Identifier.ToFullString(), InitialValue?.Value);
        }

        public LocalDeclarationStatementSyntax ToDeclarationAndAssignment()
        {
            var oldDeclaration = Declaration.Declaration;
            var oldVariables = oldDeclaration.Variables;
            if (oldVariables.Count > 1)
            {
                throw new NotSupportedException("Multiple declaration and assignments in a single statement not supported.");
            }

            SerializerVariable tmpThis = this;
            var newVariables =
                SyntaxFactory.SeparatedList(
                    oldVariables.Select(x =>
                                        {
                                            var newInitialValue = tmpThis.InitialValue;
                                            if (x.Initializer is not null && newInitialValue is not null)
                                                throw new NotSupportedException("Cannot assign already assigned variable.");
                                            return
                                                SyntaxFactory.VariableDeclarator(x.Identifier, x.ArgumentList,
                                                    newInitialValue ?? x.Initializer ?? tmpThis.CreateDefaultValue());
                                        }));
            var newDeclaration = SyntaxFactory.VariableDeclaration(oldDeclaration.Type, newVariables);
            return SyntaxFactory.LocalDeclarationStatement(Declaration.AttributeLists, Declaration.AwaitKeyword,
                Declaration.UsingKeyword, Declaration.Modifiers, newDeclaration, Declaration.SemicolonToken);
        }

        public SeparatedSyntaxList<VariableDeclaratorSyntax> ToAssignment()
        {
            var oldDeclaration = Declaration.Declaration;
            var oldVariables = oldDeclaration.Variables;
            if (oldVariables.Count > 1)
            {
                throw new NotSupportedException("Multiple declaration and assignments in a single statement not supported.");
            }

            SerializerVariable tmpThis = this;
            var newVariables =
                SyntaxFactory.SeparatedList(
                    oldVariables.Select(x =>
                    {
                        var newInitialValue = tmpThis.InitialValue;
                        if (x.Initializer is not null && newInitialValue is not null)
                            throw new NotSupportedException("Cannot assign already assigned variable.");
                        return
                            SyntaxFactory.VariableDeclarator(x.Identifier, x.ArgumentList,
                                newInitialValue ?? x.Initializer ?? tmpThis.CreateDefaultValue());
                    }));
            return newVariables;
        }

    }
}