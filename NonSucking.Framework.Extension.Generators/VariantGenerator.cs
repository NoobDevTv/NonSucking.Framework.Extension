using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonSucking.Framework.Extension.Generators
{
    [Generator]
    public class VariantGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var generations = 20;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Reactive.Linq;");
            builder.AppendLine();
            builder.AppendLine("namespace NonSucking.Framework.Extension.SumTypes");
            builder.AppendLine("{");
            //for (int i = 2; i < generations; i++)
            //builder.AppendLine("public class Variant<T1>{}");
            for (var i = 2; i <= generations; i++)
            {
                IEnumerable<int> generics = Enumerable.Range(1, i);
                var typeParams = generics.Select(index => "T" + index).ToArray();
                var paramsList = string.Join(", ", typeParams);

                builder.AppendLine($"    public class Variant<{paramsList}> : IEquatable<Variant<{paramsList}>>");
                builder.AppendLine("    {");
                builder.AppendLine("        public Type ValueType => value.GetType();");
                builder.AppendLine("        private readonly object value;");
                builder.AppendLine("        private readonly int typeId;");
                builder.AppendLine();

                for (var index = 0; index < typeParams.Length; index++)
                {
                    builder.AppendLine($"        public Variant({typeParams[index]} value) => (typeId, this.value) = ({index}, value);");
                }

                builder.AppendLine();

                for (var index = 0; index < typeParams.Length; index++)
                {
                    builder.AppendLine($"        public bool Contains({typeParams[index]} value) " +
                        $"=> {index} == typeId && Equals(value, this.value);");
                }

                AddGetMethod(builder, typeParams);
                AddMapMethod(builder, typeParams);

                builder.AppendLine("public bool Is(Type type)");
                builder.AppendLine("    => type == ValueType;");

                builder.AppendLine($"public override bool Equals(object other) => other is Variant<{paramsList}> variant && Equals(variant);");
                builder.AppendLine($"public bool Equals(Variant<{paramsList}> other)");
                builder.AppendLine($"=> other != null");
                builder.AppendLine($"   && typeId == other.typeId");
                builder.AppendLine($"   && value.Equals(other.value);");

                builder.AppendLine($"public override int GetHashCode() => typeId + -1851467485 + (value?.GetHashCode() ?? 1);");

                builder.AppendLine($"public override string ToString() => $\"Variant<{{ValueType.Name}}>({{value}})\";");

                foreach (var t in typeParams)
                    builder.AppendLine($"public static implicit operator Variant<{paramsList}>({t} obj) => new Variant<{paramsList}>(obj);");

                builder.AppendLine($"public static bool operator ==(Variant<{paramsList}> left, Variant<{paramsList}> right) => left.Equals(right);");
                builder.AppendLine($"public static bool operator !=(Variant<{paramsList}> left, Variant<{paramsList}> right) => !(left == right);");

                builder.AppendLine("}");

                GenerateIEnumerableExtensions(builder, typeParams);
                GenerateIObservableExtensions(builder, typeParams);
            }

            builder.AppendLine("}");
            context.AddSource("variantGenerator", SourceText.From(builder.ToString(), Encoding.UTF8));

        }

        private void GenerateIEnumerableExtensions(StringBuilder builder, string[] typeParams)
        {
            var paramsList = string.Join(", ", typeParams);
            builder.AppendLine("public static partial class IEnumerableExtensions {");

            for (var i = 0; i < typeParams.Length; i++)
            {
                var parameter = typeParams[i];
                builder.AppendLine($"public static IEnumerable<TResult> Map<TResult, {paramsList}>" +
                    $"(this IEnumerable<Variant<{paramsList}>> enumerable, Func<{parameter}, TResult> selector){{");
                builder.AppendLine($"foreach(var item in enumerable)");
                builder.AppendLine("{");
                builder.AppendLine($"    if(item.TryGet(out {parameter} value))");
                builder.AppendLine("        yield return selector(value);");
                builder.AppendLine("}");
                builder.AppendLine("}");

                builder.AppendLine($"public static IEnumerable<Variant<{paramsList}>> Map<{paramsList}>" +
                    $"(this IEnumerable<Variant<{paramsList}>> enumerable, Action<{parameter}> action){{");
                builder.AppendLine($"foreach(var item in enumerable)");
                builder.AppendLine("{");
                builder.AppendLine($"    if(item.TryGet(out {parameter} value))");
                builder.AppendLine("         action(value);");

                builder.AppendLine("    yield return item;");
                builder.AppendLine("}");
                builder.AppendLine("}");
            }

            builder.AppendLine($"public static IEnumerable<TResult> Map<TResult, {paramsList}>(this IEnumerable<Variant<{paramsList}>> enumerable, " +
                $"{string.Join(",", typeParams.Select(x => $"Func<{x}, TResult> selector{x}"))}){{");
            builder.AppendLine($"foreach(var value in enumerable)");
            builder.AppendLine($"    yield return value.Map({string.Join(", ", typeParams.Select(x => $"selector{x}"))});");
            builder.AppendLine($"}}");

            builder.AppendLine($"public static IEnumerable<Variant<{paramsList}>> Map<{paramsList}>(this IEnumerable<Variant<{paramsList}>> enumerable, " +
                $"{string.Join(",", typeParams.Select(x => $"Action<{x}> action{x}"))}){{");
            builder.AppendLine($"foreach(var value in enumerable)");
            builder.AppendLine("{");
            builder.AppendLine($"    value.Map({string.Join(", ", typeParams.Select(x => $"action{x}"))});");

            builder.AppendLine("    yield return value;");
            builder.AppendLine("}");
            builder.AppendLine($"}}");

            builder.AppendLine("}");
        }

        private void GenerateIObservableExtensions(StringBuilder builder, string[] typeParams)
        {
            var paramsList = string.Join(", ", typeParams);
            builder.AppendLine("public static partial class IObservableExtensions {");

            for (var i = 0; i < typeParams.Length; i++)
            {
                var parameter = typeParams[i];
                builder.AppendLine($"public static IObservable<TResult> Map<TResult, {paramsList}>" +
                    $"(this IObservable<Variant<{paramsList}>> observable, Func<{parameter}, TResult> selector){{");
                builder.AppendLine($"   return observable.Where(t => t.TryGet(out {parameter} v)).Select(v => v.Map(selector));");
                builder.AppendLine("}");
            }

            builder.AppendLine($"public static IObservable<TResult> Map<TResult, {paramsList}>(this IObservable<Variant<{paramsList}>> observable, " +
                $"{string.Join(",", typeParams.Select(x => $"Func<{x}, TResult> selector{x}"))}){{");
            builder.AppendLine($"   return observable" +
                $".Select(v => v.Map({string.Join(", ", typeParams.Select(x => $"selector{x}"))}));");
            builder.AppendLine($"}}");

            builder.AppendLine($"public static IObservable<TResult> MapMany<TResult, {paramsList}>(this IObservable<Variant<{paramsList}>> observable, " +
                $"{string.Join(",", typeParams.Select(x => $"Func<IObservable<{x}>, IObservable<TResult>> selector{x}"))}){{");
            builder.AppendLine($"   return observable" +
                $".Publish(v => Observable.Merge({string.Join(", ", typeParams.Select(x => $"v.Map(({x} t) => t).Let(selector{x})"))}));");
            builder.AppendLine($"}}");

            builder.AppendLine("}");
        }

        private void AddMapMethod(StringBuilder builder, string[] typeParams)
        {
            for (var i = 0; i < typeParams.Length; i++)
            {
                var parameter = typeParams[i];
                builder.AppendLine($"public void Map(Action<{parameter}> action){{");
                builder.AppendLine($"if ({i} != typeId)");
                builder.AppendLine("    throw new TypeMismatchException();");
                builder.AppendLine($"action(({parameter})value);}}");

                builder.AppendLine($"public TReturn Map<TReturn>(Func<{parameter}, TReturn> selector){{");
                builder.AppendLine($"if ({i} != typeId)");
                builder.AppendLine("    throw new TypeMismatchException();");
                builder.AppendLine($"return selector(({parameter})value);}}");
            }

            builder.AppendLine($"public void Map({string.Join(",", typeParams.Select(x => $"Action<{x}> action{x}"))}){{");
            builder.AppendLine($"switch(typeId){{");

            for (var index = 0; index < typeParams.Length; index++)
                builder.AppendLine($"case {index}: action{typeParams[index]}(({typeParams[index]})value); break;");

            builder.AppendLine("default: throw new TypeMismatchException();");
            builder.AppendLine($"}}");
            builder.AppendLine($"}}");

            builder.AppendLine($"public TReturn Map<TReturn>({string.Join(",", typeParams.Select(x => $"Func<{x}, TReturn> selector{x}"))}){{");
            builder.AppendLine($"switch(typeId){{");

            for (var index = 0; index < typeParams.Length; index++)
                builder.AppendLine($"case {index}: return selector{typeParams[index]}(({typeParams[index]})value);");

            builder.AppendLine("default: throw new TypeMismatchException();");
            builder.AppendLine($"}}");
            builder.AppendLine($"}}");

        }

        private void AddGetMethod(StringBuilder builder, string[] typeParams)
        {

            for (var i = 0; i < typeParams.Length; i++)
            {
                var parameter = typeParams[i];
                builder.AppendLine($"        public void Get(out {parameter} value)");
                builder.AppendLine("        {");
                builder.AppendLine($"           if ({i} != typeId)");
                builder.AppendLine("                throw new TypeMismatchException();");
                builder.AppendLine($"           value = ({parameter})this.value;");
                builder.AppendLine("        }");

                builder.AppendLine($"        public bool TryGet(out {parameter} value)");
                builder.AppendLine("        {");
                builder.AppendLine($"           value = default;");
                builder.AppendLine($"           if ({i} != typeId)");
                builder.AppendLine("                return false;");
                builder.AppendLine($"           value = ({parameter})this.value;");
                builder.AppendLine($"           return true;");
                builder.AppendLine("        }");
            }

        }


        public void Initialize(GeneratorInitializationContext context) { }
    }
}
