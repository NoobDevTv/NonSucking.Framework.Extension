﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Serialization
{
    internal static class Helper
    {
        private static readonly Regex endsWithOurSuffixAndGuid;
        internal static int uniqueNumber = 8;

        static Helper()
        {
            //endsWithOurSuffixAndGuid = new Regex($"{localVariableSuffix}[a-f0-9]{{32}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            endsWithOurSuffixAndGuid = new Regex($"{Consts.LocalVariableSuffix}[a-zA-Z]{{1,6}}",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        internal static void Reset()
        {
            uniqueNumber = 8;
        }

        internal static (GeneratedMethod?, ITypeSymbol?) GetFirstMemberWithBase(NoosonGeneratorContext ngc,
            ITypeSymbol? baseType, Func<GeneratedMethod, bool> predicate,
            int maxRecursion = int.MaxValue,
            int currentIteration = 0)
        {
            if (baseType is null)
                return (null, null);
            var gc = ngc.GlobalContext;
            NoosonGenerator.GenerateForTypeSymbol(ngc.GeneratorContext, ngc.GlobalContext, baseType, ngc.UseAdvancedTypes);
            if (currentIteration++ > maxRecursion)
                return (null, null);
            if (gc.TryResolve(baseType, out var generatedFile))
            {
                foreach (var generatedType in generatedFile.GeneratedTypes)
                    foreach (var m in generatedType.Methods)
                        if (predicate(m))
                            return (m, baseType);
            }

            return GetFirstMemberWithBase(ngc, baseType.BaseType, predicate, maxRecursion, currentIteration);
        }

        internal static T? GetFirstMemberWithBase<T>(ITypeSymbol? symbol,
            Func<T, bool> predicate,
            NoosonGeneratorContext ngc,
            int maxRecursion = int.MaxValue,
            int currentIteration = 0)
            where T : class, ISymbol
        {
            if (symbol is null)
                return null;
            NoosonGenerator.GenerateForTypeSymbol(ngc.GeneratorContext, ngc.GlobalContext, symbol, ngc.UseAdvancedTypes);
            if (currentIteration++ > maxRecursion)
                return null;

            foreach (var member in symbol.GetMembers())
            {
                if (member is T t && predicate(t))
                    return t;
            }

            return GetFirstMemberWithBase(symbol.BaseType, predicate, ngc, maxRecursion,
                currentIteration);
        }

        internal static IEnumerable<T> GetMembersWithBase<T>(this ITypeSymbol? symbol, Func<T, bool> predicate, int maxRecursion = int.MaxValue, int currentIteration = 0) where T : ISymbol
        {
            if (currentIteration++ > maxRecursion)
                yield break;

            if (symbol is null)
                yield break;
            var isRecord = symbol.IsRecord;
            foreach (var member in symbol.GetMembers())
            {
                if (member is not T t)
                    continue;

                if (member.TryGetAttribute(AttributeTemplates.Ignore, out _))
                    continue;

                if (!predicate(t))
                    continue;

                yield return t;
            }

            foreach (var item in GetMembersWithBase(symbol.BaseType, predicate, maxRecursion, currentIteration))
            {
                yield return item;
            }
        }

        internal static IEnumerable<(MemberInfo memberInfo, int depth)> GetMembersWithBase(ITypeSymbol? symbol,
            int maxRecursion = int.MaxValue, int currentIteration = 0)
        {
            if (currentIteration++ > maxRecursion)
                yield break;

            if (symbol is null)
                yield break;
            var isRecord = symbol.IsRecord;
            foreach (var member in symbol.GetMembers())
            {
                if (member.TryGetAttribute(AttributeTemplates.Ignore, out _))
                    continue;

                if (member is IPropertySymbol propSymbol)
                {
                    // Exclude CompilerGenerated EqualityContract from serialization process
                    if (isRecord && propSymbol.Name == "EqualityContract")
                    {
                        continue;
                    }

                    yield return (
                        new MemberInfo(propSymbol.Type, member, member.Name, NoosonGenerator.ReturnValueBaseName),
                        currentIteration);
                }
                else if (member is IFieldSymbol fieldSymbol &&
                         fieldSymbol.TryGetAttribute(AttributeTemplates.Include, out _))
                {
                    yield return (
                        new MemberInfo(fieldSymbol.Type, member, member.Name, NoosonGenerator.ReturnValueBaseName),
                        currentIteration);
                }
            }

            foreach (var item in GetMembersWithBase(symbol.BaseType, maxRecursion, currentIteration))
            {
                yield return item;
            }
        }

        internal static bool HasBase(this ITypeSymbol? Implementation, ITypeSymbol PossibleBase)
        {
            if (Implementation is null)
                return false;
            if (SymbolEqualityComparer.Default.Equals(Implementation, PossibleBase))
                return true;
            return HasBase(Implementation.BaseType, PossibleBase);
        }

        internal static string GetReadMethodCallFrom(SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.System_Boolean => nameof(BinaryReader.ReadBoolean),
                SpecialType.System_Char => nameof(BinaryReader.ReadChar),
                SpecialType.System_SByte => nameof(BinaryReader.ReadSByte),
                SpecialType.System_Byte => nameof(BinaryReader.ReadByte),
                SpecialType.System_Int16 => nameof(BinaryReader.ReadInt16),
                SpecialType.System_UInt16 => nameof(BinaryReader.ReadUInt16),
                SpecialType.System_Int32 => nameof(BinaryReader.ReadInt32),
                SpecialType.System_UInt32 => nameof(BinaryReader.ReadUInt32),
                SpecialType.System_Int64 => nameof(BinaryReader.ReadInt64),
                SpecialType.System_UInt64 => nameof(BinaryReader.ReadUInt64),
                SpecialType.System_Decimal => nameof(BinaryReader.ReadDecimal),
                SpecialType.System_Single => nameof(BinaryReader.ReadSingle),
                SpecialType.System_Double => nameof(BinaryReader.ReadDouble),
                SpecialType.System_String => nameof(BinaryReader.ReadString),
                _ => throw new NotSupportedException(),
            };
        }

        internal static bool MatchIdentifierWithPropName(string identifier, string parameterName)
        {
            var index = identifier.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
            if (index > -1)
                identifier = identifier.Remove(index);
            index = parameterName.IndexOf(Consts.LocalVariableSuffix, StringComparison.Ordinal);
            if (index > -1)
                parameterName = parameterName.Remove(index);

            return char.ToLowerInvariant(identifier[0]) == char.ToLowerInvariant(parameterName[0])
                   && string.Equals(identifier.Substring(1), parameterName.Substring(1));
        }


        const string allChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const uint allCharsLength = 52;

        private static string IntToString(int i)
        {
            var unsigned = (uint)i;
            var sb = new StringBuilder();
            while (unsigned > 0)
            {
                uint begin = unsigned % allCharsLength;
                _ = sb.Insert(0, allChars[(int)begin]);
                unsigned /= allCharsLength;
            }

            return sb.ToString();
        }

        internal static string GetRandomNameFor(string variableName, string name = "")
        {
            if (endsWithOurSuffixAndGuid.IsMatch(variableName))
                return variableName;


            return
                $"{variableName}{Consts.LocalVariableSuffix}{name}{Consts.LocalVariableSuffix}{IntToString(Interlocked.Increment(ref uniqueNumber))}";
        }

        internal static ValueArgument GetValueArgumentFrom(MemberInfo memberInfo, ITypeSymbol? castTo = null)
        {
            object referenceValue = GetMemberAccessString(memberInfo, castTo);
            return new ValueArgument(referenceValue);
        }

        internal static string GetMemberAccessString(MemberInfo memberInfo, ITypeSymbol? castTo = null)
        {
            return Cast(memberInfo.FullName, castTo?.ToDisplayString());
        }

        internal static string Cast(string value, string? castType = null)
        {
            if (!string.IsNullOrWhiteSpace(castType))
                return $"(({castType}){value})";

            return value;
        }

        internal static void GetGenAttributeData(AttributeData attributeData, out bool generateDefaultReader,
            out bool generateDefaultWriter, out INamedTypeSymbol?[] directReaders,
            out INamedTypeSymbol?[] directWriters)
        {
            bool GetGenerateDefault(int i, bool defaultValue)
            {
                if (attributeData!.ConstructorArguments.Length <= i)
                    return defaultValue;
                var val = (int)(attributeData.ConstructorArguments[i].Value!);
                return val switch
                {
                    -1 => defaultValue,
                    0 => false,
                    1 => true,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            generateDefaultReader = GetGenerateDefault(0, true);
            generateDefaultWriter = GetGenerateDefault(1, true);
            directReaders =
                attributeData.ConstructorArguments.Length > 2 &&
                !attributeData.ConstructorArguments[2].Values.IsDefaultOrEmpty
                    ? attributeData.ConstructorArguments[2].Values.Select(x => (INamedTypeSymbol?)x.Value).ToArray()
                    : Array.Empty<INamedTypeSymbol>();
            directWriters =
                attributeData.ConstructorArguments.Length > 3 &&
                !attributeData.ConstructorArguments[3].Values.IsDefaultOrEmpty
                    ? attributeData.ConstructorArguments[3].Values.Select(x => (INamedTypeSymbol?)x.Value).ToArray()
                    : Array.Empty<INamedTypeSymbol>();
        }


        internal static bool CheckSignature(NoosonGeneratorContext context, IMethodSymbol m, string? typeName,
            bool checkForStatic = true)
        {
            if (!checkForStatic && m.IsStatic)
                return false;

            if (context.WriterTypeName is not null
                && (!m.IsStatic || m.Parameters.Length != 2)
                && (m.IsStatic || m.Parameters.Length != 1))
                return false;

            if (context.ReaderTypeName is not null
                && (!m.IsStatic || m.Parameters.Length != 1))
                return false;

            if (!(context.ReaderTypeName is null && context.WriterTypeName is null))
            {
                var binaryTypeName = m.Parameters.Last().Type.ToDisplayString();
                return binaryTypeName == context.WriterTypeName || binaryTypeName == context.ReaderTypeName;
            }

            if (m.Parameters.Last().Type.TypeKind != TypeKind.TypeParameter || !m.IsGenericMethod)
                return false;

            var typeParameter = m.TypeParameters.FirstOrDefault(x => x.Name == m.Parameters.Last().Type.Name);

            return typeParameter != null && typeParameter.ConstraintTypes.Any(x => x.Name == typeName);
        }

        internal static List<ArgumentSyntax> GetArgumentsFromGenMethod(string readerName,
            MemberInfo memberInfo,
            List<string> declerationNames,
            IEnumerable<string> parameters)
        {
            List<ArgumentSyntax> arguments = new()
                        {SyntaxFactory.Argument(SyntaxFactory.IdentifierName(readerName))};

            foreach (var item in parameters)
            {
                string variableName = Helper.GetRandomNameFor(item, memberInfo.Name);
                declerationNames.Add(variableName);
                arguments.Add(SyntaxFactory
                    .Argument(null,
                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                        SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName(
                                SyntaxFactory.Identifier(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.VarKeyword,
                                    "var",
                                    "var",
                                    SyntaxFactory.TriviaList())),
                            SyntaxFactory.SingleVariableDesignation(
                                SyntaxFactory.Identifier(variableName)))));
            }

            return arguments;
        }

        internal static void ConvertToStatement(
                GeneratedSerializerCode statements,
                string typeName,
                string methodName,
                List<ArgumentSyntax> arguments)
        {

            MemberAccessExpressionSyntax memberAccess = SyntaxFactory
                .MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(typeName),
                    SyntaxFactory.IdentifierName(methodName));

            statements.Statements.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(memberAccess,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(arguments)))));
        }

        internal static string ToSummaryName(this ISymbol symbol)
        {
            string displayStr = symbol switch
            {
                IPropertySymbol ps => ps.OriginalDefinition.ToDisplayString(),
                IFieldSymbol fs => fs.OriginalDefinition.ToDisplayString(),
                IMethodSymbol ms => ms.OriginalDefinition.ToDisplayString(),
                ITypeSymbol ts => ts.OriginalDefinition.ToDisplayString(),
                _ => symbol.ToDisplayString()
            };

            return displayStr.ToSummaryName();
        }
        internal static string ToSummaryName(this string displayStr)
        {
            return displayStr
                .Replace('<', '{')
                .Replace('>', '}');
        }
        internal static bool SameAssembly(this ISymbol symbol, GlobalContext gc)
        {
            return SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, gc.Compilation.Assembly);
        }
        internal static bool Equality(this ISymbol symbol1, ISymbol symbol2)
        {
            return SymbolEqualityComparer.Default.Equals(symbol1, symbol2);
        }

        internal static bool ForAll<T, T2>(this IEnumerable<T> list, IList<T2> second, Func<T, T2, bool> check)
        {
            int index = 0;
            foreach (var item in list)
            {
                if (second.Count > index)
                {
                    if (!check(item, second[index]))
                        return false;
                }
                else
                    return false;
                index++;
            }

            return second.Count == index;
        }
    }
}
