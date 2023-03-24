using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using NonSucking.Framework.Serialization.Serializers;
using VaVare.Generators.Common.Arguments.ArgumentTypes;

using VaVare.Statements;

namespace NonSucking.Framework.Serialization
{
    [StaticSerializer(20)]
    internal static class SpecialTypeSerializer
    {
        internal static Continuation TrySerialize(ref MemberInfo property, NoosonGeneratorContext context, string writerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            var type = property.TypeSymbol;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    ValueArgument argument = Helper.GetValueArgumentFrom(property);

                    statements.Statements.Add(Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement());
                    return Continuation.Done;
                default:

                    return Continuation.NotExecuted;
            }
        }

        internal static Continuation TryDeserialize(ref MemberInfo property, NoosonGeneratorContext context, string readerName, GeneratedSerializerCode statements, ref SerializerMask includedSerializers)
        {
            var type = property.TypeSymbol;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, Helper.GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();
                    
                    statements.DeclareAndAssign(property, property.CreateUniqueName(), type, invocationExpression);

                    return Continuation.Done;
                default:

                    return Continuation.NotExecuted;
            }
        }
    }
}
