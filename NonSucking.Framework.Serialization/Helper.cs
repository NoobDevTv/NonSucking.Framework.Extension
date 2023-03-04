using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
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
        internal const string localVariableSuffix = "__";
        internal const string doubleLocalVariableSuffix = localVariableSuffix + localVariableSuffix;
        internal static int uniqueNumber = 8;
        static Helper()
        {
            //endsWithOurSuffixAndGuid = new Regex($"{localVariableSuffix}[a-f0-9]{{32}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            endsWithOurSuffixAndGuid = new Regex($"{localVariableSuffix}[a-zA-Z]{{1,6}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        internal static ISymbol? GetFirstMemberWithBase(ITypeSymbol? symbol, Func<ISymbol, bool> predicate, int maxRecursion = int.MaxValue, int currentIteration = 0)
        {
            if (symbol is null)
                return null;
            if (currentIteration++ > maxRecursion)
                return null;

            foreach (var member in symbol.GetMembers())
            {
                if (predicate(member))
                    return member;
            }

            return GetFirstMemberWithBase(symbol.BaseType, predicate, maxRecursion, currentIteration);
        }
        internal static IEnumerable<MemberInfo> GetMembersWithBase(ITypeSymbol? symbol, int maxRecursion = int.MaxValue, int currentIteration = 0)
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
                    yield return new MemberInfo(propSymbol.Type, member, member.Name, NoosonGenerator.ReturnValueBaseName);
                }
                else if (member is IFieldSymbol fieldSymbol && fieldSymbol.TryGetAttribute(AttributeTemplates.Include, out _))
                {
                    yield return new MemberInfo(fieldSymbol.Type, member, member.Name, NoosonGenerator.ReturnValueBaseName);
                }
            }
            foreach (var item in GetMembersWithBase(symbol.BaseType, maxRecursion, currentIteration))
            {
                yield return item;
            }
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
            var index = identifier.IndexOf(localVariableSuffix);
            if (index > -1)
                identifier = identifier.Remove(index);
            
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


            return $"{variableName}{localVariableSuffix}{name}{localVariableSuffix}{IntToString(Interlocked.Increment(ref uniqueNumber))}";
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
            out bool generateDefaultWriter, out INamedTypeSymbol?[] directReaders, out INamedTypeSymbol?[] directWriters)
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
                attributeData.ConstructorArguments.Length > 2 && !attributeData.ConstructorArguments[2].Values.IsDefaultOrEmpty
                ? attributeData.ConstructorArguments[2].Values.Select(x => (INamedTypeSymbol?)x.Value).ToArray()
                : Array.Empty<INamedTypeSymbol>();
            directWriters =
                attributeData.ConstructorArguments.Length > 3 && !attributeData.ConstructorArguments[3].Values.IsDefaultOrEmpty
                ? attributeData.ConstructorArguments[3].Values.Select(x => (INamedTypeSymbol?)x.Value).ToArray()
                : Array.Empty<INamedTypeSymbol>();
        }


        internal static bool CheckSignature(NoosonGeneratorContext context, IMethodSymbol m, string? typeName)
        {

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

        internal static string ToSummaryName(this ITypeSymbol symbol)
        {
            return symbol.ToDisplayString()
                .Replace('<', '{')
                .Replace('>', '}');
        }
    }
}
