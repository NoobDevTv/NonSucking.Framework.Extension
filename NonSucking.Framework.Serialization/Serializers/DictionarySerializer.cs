using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models.References;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    internal static class DictionarySerializer
    {

        private static bool CheckMetaDataName(ISymbol symbol)
        {
            return symbol.MetadataName == typeof(IReadOnlyDictionary<,>).Name
                || symbol.MetadataName == typeof(IDictionary<,>).Name;
        }

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName,List<StatementSyntax> statements)
        {
            
            ITypeSymbol type = property.TypeSymbol;


            bool isDictionary
                = CheckMetaDataName(type)
                || type
                    .AllInterfaces
                    .Any(x =>
                        x.MetadataName == typeof(IReadOnlyDictionary<,>).Name
                        || x.MetadataName == typeof(IDictionary<,>).Name
                    );


            if (!isDictionary)
            {
                return false;
            }

            string itemName = Helper.GetRandomNameFor("item", property.Name);

            List<StatementSyntax> localStatements = new();

            ITypeSymbol keyGenericArgument;
            ITypeSymbol valueGenericArgument;

            if (type is INamedTypeSymbol nts)
            {
                keyGenericArgument
                    = nts
                    .TypeArguments[0];

                valueGenericArgument
                    = nts
                    .TypeArguments[1];

                MemberInfo[] genericInfos
                    = new[] {
                        new MemberInfo(keyGenericArgument, keyGenericArgument, "Key", itemName) ,
                        new MemberInfo(valueGenericArgument, valueGenericArgument, "Value", itemName)
                    };


                var generatedStatements
                    = NoosonGenerator
                    .GenerateStatementsForProps(genericInfos, context, MethodType.Serialize);
                foreach (var item in generatedStatements)
                {

                    localStatements.AddRange(item);
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            MemberReference memberReference
                = new MemberReference(
                type.TypeKind == TypeKind.Array
                    ? "Length"
                    : "Count");
            ReferenceArgument countReference = new ReferenceArgument(new VariableReference(Helper.GetMemberAccessString(property), memberReference));


            ExpressionStatementSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write), arguments: new[] { countReference })
                        .AsStatement();


            ForEachStatementSyntax iterationStatement
                = Statement
                .Iteration
                .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property), BodyGenerator.Create(localStatements.ToArray()), useVar: true);


            statements.Add(invocationExpression);
            statements.Add(iterationStatement);


            return true;
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName,List<StatementSyntax> statements)
        {
            
            ITypeSymbol type = property.TypeSymbol;

            bool isDictionary
               = CheckMetaDataName(type)
               || type
                   .AllInterfaces
                   .Any(x =>
                       x.MetadataName == typeof(IReadOnlyDictionary<,>).Name
                       || x.MetadataName == typeof(IDictionary<,>).Name
                   );


            if (!isDictionary)
            {
                return false;
            }

            ITypeSymbol keyGenericArgument;
            ITypeSymbol valueGenericArgument;

            string keyVariableName = Helper.GetRandomNameFor("key", property.Name);
            string valueVariableName = Helper.GetRandomNameFor("value", property.Name);
            List<StatementSyntax> localStatements = new();

            if (type is INamedTypeSymbol nts)
            {
                keyGenericArgument
                    = nts
                    .TypeArguments[0];

                valueGenericArgument
                    = nts
                    .TypeArguments[1];

                MemberInfo[] genericInfos
                    = new[] {
                        new MemberInfo(keyGenericArgument, keyGenericArgument, keyVariableName),
                        new MemberInfo(valueGenericArgument, valueGenericArgument, valueVariableName)
                    };

                var statementsForProps
                    = NoosonGenerator
                    .GenerateStatementsForProps(genericInfos, context, MethodType.Deserialize);
                foreach (var item in statementsForProps)
                {
                    localStatements.AddRange(item);
                }
            }
            else
            {
                throw new NotSupportedException();
            }


            string listName = $"{Helper.GetRandomNameFor(property.Name, property.Parent)}";

            ExpressionStatementSyntax addStatement
                = Statement
                .Expression
                .Invoke(
                    listName,
                    $"Add",
                    arguments: new[] { new ValueArgument((object)keyVariableName), new ValueArgument((object)valueVariableName) }
                )
                .AsStatement();

            localStatements.Add(addStatement);

            VariableReference start = new VariableReference("0");
            VariableReference end = new VariableReference(Helper.GetRandomNameFor("count" , property.Name));


            ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();

            LocalDeclarationStatementSyntax countStatement
                = Statement
                .Declaration
                .DeclareAndAssign(end.Name, invocationExpression);

            ExpressionSyntax ctorInvocationExpression
                = Statement
                .Expression
                .Invoke($"new System.Collections.Generic.Dictionary<{keyGenericArgument.ToDisplayString()}, {valueGenericArgument.ToDisplayString()}>", arguments: new[] { new ValueArgument((object)end.Name) })
                .AsExpression();

            LocalDeclarationStatementSyntax listStatement
                = Statement
                .Declaration
                .DeclareAndAssign(listName, ctorInvocationExpression);

            ForStatementSyntax iterationStatement
                = Statement
                .Iteration
                .For(start, end, Helper.GetRandomNameFor("i", ""), BodyGenerator.Create(localStatements.ToArray()));

            statements.Add(countStatement);
            statements.Add(listStatement);
            statements.Add(iterationStatement);

            return true;
        }
    }
}
