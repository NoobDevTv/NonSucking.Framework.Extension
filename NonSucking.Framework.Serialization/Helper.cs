using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.IO;
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

        internal static IEnumerable<MemberInfo> GetMembersWithBase(ITypeSymbol? symbol, int maxRecursion = int.MaxValue, int currentIteration = 0)
        {
            if(currentIteration++ > maxRecursion)
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

            return string.Equals(identifier, parameterName, StringComparison.OrdinalIgnoreCase);
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
            if (string.IsNullOrEmpty(memberInfo.Parent))
            {
                return Cast(memberInfo.Name, castTo?.ToDisplayString());
            }
            else
            {
                return Cast($"{memberInfo.Parent}.{memberInfo.Name}", castTo?.ToDisplayString());
            }
        }

        internal static string Cast(string value, string? castType = null)
        {
            if (!string.IsNullOrWhiteSpace(castType))
                return $"(({castType}){value})";

            return value;
        }
    }
}
