using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

using VaVare.Statements;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using System.Linq;
using System.IO;
using NonSucking.Framework.Serialization.Attributes;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using NonSucking.Framework.Serialization.Serializers;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(0)]
    internal static class CustomMethodCallSerializer
    {
        private static IMethodSymbol? GetMethod(NoosonGeneratorContext context, MemberInfo property, ITypeSymbol? customType, string methodName, bool isClassAttribute)
        {
            ITypeSymbol typeToCallOn;
            bool matchAdditionalParam = false;
            if (customType is not null)
            {
                typeToCallOn = customType;

                matchAdditionalParam = context.MethodType == MethodType.Serialize;
            }
            else if (isClassAttribute 
                     || (property.Symbol is not IPropertySymbol 
                         && property.Symbol is not IFieldSymbol))
            {
                typeToCallOn = property.TypeSymbol;
            }
            else if (property.Parent == Consts.ThisName)
            {
                typeToCallOn = property.Symbol.ContainingType;
            }
            else
            {
                typeToCallOn = property.Symbol.ContainingType;
            }
            return typeToCallOn.GetMembersWithBase<IMethodSymbol>(m => m.Name == methodName && m.Parameters.Length > 0 &&
                                                                            Helper.MatchReaderWriterParameter(context, m.Parameters.First()))
                .FirstOrDefault(m => (!matchAdditionalParam && m.Parameters.Length == 1) ||
                                     (matchAdditionalParam && m.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(property.TypeSymbol, m.Parameters[1].Type)));
        }
        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            var methodName = Consts.Deserialize;
            bool isClassAttribute = false;
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.Custom, out var propAttrData))
            {
                isClassAttribute = property.TypeSymbol.TryGetAttribute(AttributeTemplates.Custom, out propAttrData);
                if (!isClassAttribute)
                    return Continuation.NotExecuted;

            }

            if (propAttrData!.NamedArguments.IsEmpty)
            {
                context.AddDiagnostic(Diagnostics.CustomMethodParameterNeeded, property.Symbol, DiagnosticSeverity.Error);
                return Continuation.Done;
            }

            var customMethodName = propAttrData.NamedArguments.FirstOrDefault(x => x.Key == "DeserializeMethodName").Value.Value as string;
            if (!string.IsNullOrWhiteSpace(customMethodName))
            {
                methodName = customMethodName!;
            }

            var customType = propAttrData.NamedArguments.FirstOrDefault(x => x.Key == "DeserializeImplementationType").Value.Value as ITypeSymbol;

            var methodToCall = GetMethod(context, property, customType, methodName, isClassAttribute);
            if (methodToCall is null)
            {
                context.AddDiagnostic(Diagnostics.IncompatibleCustomSerializer.Format(context.ReaderTypeName ?? Consts.GenericParameterReaderInterfaceFull), propAttrData.ApplicationSyntaxReference?.GetLocation() ?? property.Symbol.GetLocation() ?? Location.None, DiagnosticSeverity.Warning);
                return Continuation.NotExecuted;
            }
            
            InvocationExpressionSyntax invocationExpression;

            if (customType is not null)
            {
                //Static call
                invocationExpression
                   = Statement
                   .Expression
                   .Invoke(customType.ToDisplayString(), methodName, arguments: new[] { new ValueArgument((object)readerName) })
                   .AsExpression();
            }
            else if (isClassAttribute || (property.Symbol is not IPropertySymbol && property.Symbol is not IFieldSymbol))
            {
                //Static on target type
                invocationExpression
                        = Statement
                        .Expression
                        .Invoke(property.TypeSymbol.ToDisplayString(), methodName, arguments: new[] { new ValueArgument((object)readerName) })
                        .AsExpression();
            }
            else
            {
                //Static on this
                invocationExpression
                        = Statement
                        .Expression
                        .Invoke(methodName, arguments: new[] { new ValueArgument((object)readerName) })
                        .AsExpression();
            }

            statements.DeclareAndAssign(property, property.CreateUniqueName(), property.TypeSymbol, invocationExpression);

            return Continuation.Done;
        }




        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            
            var methodName = Consts.Serialize;
            bool isClassAttribute = false;
            if (!property.Symbol.TryGetAttribute(AttributeTemplates.Custom, out var propAttrData))
            {
                isClassAttribute = property.TypeSymbol.TryGetAttribute(AttributeTemplates.Custom, out propAttrData);
                if (!isClassAttribute)
                    return Continuation.NotExecuted;

            }

            if (propAttrData!.NamedArguments.IsEmpty)
            {
                context.AddDiagnostic(Diagnostics.CustomMethodParameterNeeded, property.Symbol, DiagnosticSeverity.Error);
                return Continuation.Done;
            }

            var customMethodName = propAttrData.NamedArguments.FirstOrDefault(x => x.Key == "SerializeMethodName").Value.Value as string;
            if (!string.IsNullOrWhiteSpace(customMethodName))
            {
                methodName = customMethodName!;
            }
            StatementSyntax statement;
            var customType = propAttrData.NamedArguments.FirstOrDefault(x => x.Key == "SerializeImplementationType").Value.Value as ITypeSymbol;
            var methodToCall = GetMethod(context, property, customType, methodName, isClassAttribute);
            if (methodToCall is null)
            {
                context.AddDiagnostic(Diagnostics.IncompatibleCustomSerializer.Format(context.WriterTypeName ?? Consts.GenericParameterWriterInterfaceFull), propAttrData.ApplicationSyntaxReference?.GetLocation() ?? property.Symbol.GetLocation() ?? Location.None, DiagnosticSeverity.Warning);
                return Continuation.NotExecuted;
            }
            if (customType is not null)
            {
                //Static call
                statement
                   = Statement
                   .Expression
                   .Invoke(customType.ToDisplayString(), methodName, arguments: new[] { new ValueArgument((object)writerName), new ValueArgument((object)property.FullName) })
                   .AsStatement();
            }
            else if (isClassAttribute || (property.Symbol is not IPropertySymbol && property.Symbol is not IFieldSymbol))
            {
                //non Static
                statement
                        = Statement
                        .Expression
                        .Invoke(Helper.GetMemberAccessString(property), methodName, arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }
            else if (property.Parent == Consts.ThisName)
            {
                //Non Static on this
                statement
                        = Statement
                        .Expression
                        .Invoke(methodName, arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }
            else
            {
                //Non Static on this
                statement
                    = Statement
                        .Expression
                        .Invoke(property.Parent, methodName, arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }

            statements.Statements.Add(statement);

            return Continuation.Done;

        }
    }
}
