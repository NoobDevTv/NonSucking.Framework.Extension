using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using VaVare.Generators.Common.Arguments.ArgumentTypes;

namespace NonSucking.Framework.Serialization
{
    internal static class Helper
    {
        private static readonly Regex endsWithOurSuffixAndGuid;
        internal const string localVariableSuffix = "___";

        static Helper()
        {
            endsWithOurSuffixAndGuid = new Regex($"{localVariableSuffix}[a-f0-9]{{32}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        internal static IEnumerable<MemberInfo> GetMembersWithBase(ITypeSymbol symbol)
        {
            if (symbol is null)
                yield break;
            foreach (var member in symbol.GetMembers())
            {
                if (member is IPropertySymbol propSymbol)
                {
                    yield return new MemberInfo(propSymbol.Type, member, member.Name);
                }
                else if (member is IFieldSymbol fieldSymbol && fieldSymbol.TryGetAttribute(AttributeTemplates.Include, out _))
                {
                    yield return new MemberInfo(fieldSymbol.Type, member, member.Name);
                }
            }
            foreach (var item in GetMembersWithBase(symbol.BaseType))
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

        internal static string GetRandomNameFor(string variableName)
        {
            if (endsWithOurSuffixAndGuid.IsMatch(variableName))
                return variableName;

            return $"{variableName}{localVariableSuffix}{Guid.NewGuid().ToString("N")}";
        }

        internal static ValueArgument GetValueArgumentFrom(MemberInfo memberInfo, ITypeSymbol castTo = null)
        {
            object referenceValue = GetMemberAccessString(memberInfo, castTo);
            return new ValueArgument(referenceValue);
        }

        internal static string GetMemberAccessString(MemberInfo memberInfo, ITypeSymbol castTo = null)
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

        internal static string Cast(string value, string castType = null)
        {
            if (!string.IsNullOrWhiteSpace(castType))
                return $"({castType}){value}";

            return value;
        }
    }
}
