using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models.References;
using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    //[StaticSerializer(50)]
    internal static class DictionarySerializer
    {

        private static bool CheckMetaDataName(ISymbol symbol)
        {
            return symbol.MetadataName == typeof(IReadOnlyDictionary<,>).Name
                || symbol.MetadataName == typeof(IDictionary<,>).Name;
        }

        internal static bool TrySerialize(MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements)
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

            GeneratedSerializerCode localStatements = new();

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
                    localStatements.MergeWith(item);
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
                .ForEach(itemName, typeof(void), Helper.GetMemberAccessString(property), BodyGenerator.Create(localStatements.ToMergedBlock().ToArray()), useVar: true);


            statements.Statements.Add(invocationExpression);
            statements.Statements.Add(iterationStatement);


            return true;
        }

        internal static bool TryDeserialize(MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements)
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
            GeneratedSerializerCode localStatements = new();

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
                    localStatements.MergeWith(item);
                }
            }
            else
            {
                throw new NotSupportedException();
            }


            string listName = property.CreateUniqueName();

            ExpressionStatementSyntax addStatement
                = Statement
                .Expression
                .Invoke(
                    listName,
                    $"Add",
                    arguments: new[] { new ValueArgument((object)keyVariableName), new ValueArgument((object)valueVariableName) }
                )
                .AsStatement();

            localStatements.Statements.Add(addStatement);

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

            var dictTypeName =
                $"System.Collections.Generic.Dictionary<{keyGenericArgument.ToDisplayString()}, {valueGenericArgument.ToDisplayString()}>";
            ExpressionSyntax ctorInvocationExpression
                = Statement
                .Expression
                .Invoke($"new {dictTypeName}", arguments: new[] { new ValueArgument((object)end.Name) })
                .AsExpression();


            ForStatementSyntax iterationStatement
                = Statement
                .Iteration
                .For(start, end, Helper.GetRandomNameFor("i", ""), BodyGenerator.Create(localStatements.ToMergedBlock().ToArray()));

            statements.Statements.Add(countStatement);
            statements.DeclareAndAssign(property, listName, SyntaxFactory.ParseTypeName(dictTypeName), ctorInvocationExpression);
            statements.Statements.Add(iterationStatement);

            return true;
        }
    }
}
