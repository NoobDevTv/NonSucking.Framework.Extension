using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Generators.Common;
using VaVare.Models.References;
using VaVare.Statements;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Drawing;
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(59)]
    internal static class MultiDimensionArraySerializer
    {
        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            var type = property.TypeSymbol;
            if (type is not IArrayTypeSymbol arrayTypeSymbol || arrayTypeSymbol.Rank < 2)
                return Continuation.NotExecuted;
            var rank = arrayTypeSymbol.Rank;

            var genericArgument = arrayTypeSymbol.ElementType;

            GeneratedSerializerCode preIterationStatements = new();

            var countNames = new string[rank];
            var randomIs = new string[rank];

            for (var i = 0; i < rank; i++)
            {
                var memberReference
                    = new MemberReference(
                        $"GetLength({i})");
                var varRef = new VariableReference(Helper.GetMemberAccessString(property), memberReference);
                _
                    = new ReferenceArgument(varRef);
                var lengthName = Helper.GetRandomNameFor("length");
                countNames[i] = lengthName;
                randomIs[i] = Helper.GetRandomNameFor("i");
                var countReference =
                    Statement.Declaration.DeclareAndAssign(lengthName, varRef);

                preIterationStatements.Statements.Add(countReference);
                preIterationStatements.Statements.Add(
                    Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write),
                            arguments: new[] {new VariableArgument(lengthName)})
                        .AsStatement());
            }

            var itemName = Helper.GetRandomNameFor("item", property.Name);
            var declareForLoopMember 
                = Statement.Declaration.ParseDeclareAndAssign(
                    itemName, 
                    $"{property.FullName}[{string.Join(",", randomIs)}]");
            
            var localStatements
                = NoosonGenerator.CreateStatementForSerializing(
                    new MemberInfo(genericArgument, genericArgument, itemName),
                    context,
                    writerName);
            localStatements.Statements.Insert(0, declareForLoopMember);

            var innerStatements = localStatements.MergeBlocksSeperated(statements);

            ForStatementSyntax? currentForLoop = null;
            for (var i = rank - 1; i >= 0; i--)
            {
                var countName = countNames[i];
                var randomI = randomIs[i];

                var body = currentForLoop is null
                    ? BodyGenerator.Create(innerStatements.ToArray())
                    : BodyGenerator.Create(currentForLoop);

                currentForLoop = Statement.Iteration.For(new VariableReference("0"), new VariableReference(countName),
                    randomI, body);
            }


            statements.MergeWith(preIterationStatements);
            statements.Statements.Add(currentForLoop!);

            return Continuation.Done;
        }

        private static ForEachStatementSyntax ForEach(string variableName, string varialeType, string enumerableName,
            BlockSyntax body)
            => SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName(varialeType),
                SyntaxFactory.Identifier(variableName),
                ReferenceGenerator.Create(new VariableReference(enumerableName)),
                body);


        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName,
            GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            var type = property.TypeSymbol;
            if (type is not IArrayTypeSymbol arrayTypeSymbol || arrayTypeSymbol.Rank < 2)
                return Continuation.NotExecuted;
            var rank = arrayTypeSymbol.Rank;

            var genericArgument = arrayTypeSymbol.ElementType;

            GeneratedSerializerCode preIterationStatements = new();

            var countNames = new string[rank];
            var randomIs = new string[rank];

            for (var i = 0; i < rank; i++)
            {
                ExpressionSyntax invocationExpression
                    = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();

                var lengthName = Helper.GetRandomNameFor("length");
                preIterationStatements.Statements.Add(
                    Statement
                        .Declaration
                        .DeclareAndAssign(lengthName, invocationExpression));

                countNames[i] = lengthName;
                randomIs[i] = Helper.GetRandomNameFor("i");
            }

            var arrName = property.CreateUniqueName();
            preIterationStatements
                .DeclareAndAssign(
                    property, 
                    arrName,
                    SyntaxFactory
                        .ParseTypeName($"{genericArgument.ToDisplayString()}[{new string(',', rank - 1)}]"),
                    SyntaxFactory
                        .ParseExpression($"new {genericArgument.ToDisplayString()}[{string.Join(",", countNames)}]"));

            var itemName = Helper.GetRandomNameFor("item", property.Name);

            var localStatements
                = NoosonGenerator.CreateStatementForDeserializing(
                    new MemberInfo(genericArgument, genericArgument, itemName),
                    context,
                    readerName);

            localStatements.Statements.Add(
                Statement
                    .Declaration
                    .Assign($"{arrName}[{string.Join(",", randomIs)}]", SyntaxFactory.IdentifierName(itemName)));
            
            var innerStatements = localStatements.ToMergedBlock();

            ForStatementSyntax? currentForLoop = null;
            for (var i = rank - 1; i >= 0; i--)
            {
                var countName = countNames[i];
                var randomI = randomIs[i];

                var body = currentForLoop is null
                    ? BodyGenerator.Create(innerStatements.ToArray())
                    : BodyGenerator.Create(currentForLoop);

                currentForLoop 
                    = Statement
                        .Iteration
                        .For(
                            new VariableReference("0"), 
                            new VariableReference(countName),
                            randomI, 
                            body);
            }

            statements.MergeWith(preIterationStatements);
            statements.Statements.Add(currentForLoop!);

            return Continuation.Done;
        }
    }
}