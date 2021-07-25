using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonSucking.Framework.Extension.Generators
{
    internal class CodeBuilder : Builder
    {
        public GeneratorExecutionContext ExecutionContext { get; }

        private readonly List<TypeGroupInfo> typePropertiesGroups;

        public CodeBuilder(GeneratorExecutionContext executionContext, List<TypeGroupInfo> typePropertiesGroups) : base()
        {
            ExecutionContext = executionContext;
            this.typePropertiesGroups = typePropertiesGroups;
        }

        public MethodBuilder GetMethodBuilder(GenerationLogic logic, string accessModifier, string returnType, string name, bool isStatic = false, params (string type, string name)[] parameters)
            => new(this, logic, accessModifier, returnType, name, isStatic, parameters);

        internal class MethodBuilder : Builder<CodeBuilder>
        {
            public IReadOnlyList<TypeGroupInfo> TypeGroupInfos => parent.typePropertiesGroups;

            public GenerationLogic Logic { get; }

            private readonly string accessModifier;
            private readonly string returnType;
            private readonly string name;
            private readonly bool isStatic;
            private readonly (string type, string name)[] parameters;

            public MethodBuilder(CodeBuilder builder, GenerationLogic logic, string accessModifier, string returnType, string name, bool isStatic = false, params (string type, string name)[] parameters) : base(builder)
            {
                Logic = logic;
                this.accessModifier = accessModifier;
                this.returnType = returnType;
                this.name = name;
                this.isStatic = isStatic;
                this.parameters = parameters;
            }

            public MethodBuilder Open()
            {
                string paramsString
                 = string
                 .Join(", ", parameters.Select(param => $"{param.type} {param.name}"));

                stringBuilder
                    .AppendLine($"{accessModifier}{(isStatic ? " static" : "")} {returnType} {name}({paramsString})")
                    .AppendLine("{");
                return this;
            }

            public LoopBuilder ForEach(string variable, string collection)
            => new(this, this.stringBuilder, $"foreach(var {variable} in {collection})");


            public LoopBuilder For(string startValue, string condition, string operation)
                => new(this, this.stringBuilder, $"for({startValue}; {condition}; {operation})");

            public InstanceCallBuilder GetProperty(PropertyInfo typeGroupInfo, string memberName = null)
                => new(this, typeGroupInfo.PropertySymbol.Type, memberName ?? typeGroupInfo.PropertySymbol.Name);

            public InstanceCallBuilder GetInstance(ITypeSymbol typeSymbol, string memberName)
                => new(this, typeSymbol, memberName);

            public CodeBuilder Close()
            {
                stringBuilder.AppendLine("}");
                return AppendToParent();
            }

        }

        internal class InstanceCallBuilder : Builder<MethodBuilder>
        {
            public ITypeSymbol TypeInformation { get; }
            public string InstanceName { get; }

            private string lowerCaseName;

            public InstanceCallBuilder(MethodBuilder method, ITypeSymbol typeInfo, string instanceName) : base(method)
            {
                InstanceName = instanceName;
                TypeInformation = typeInfo;
                lowerCaseName = InstanceName.Substring(0, 1).ToLower() + InstanceName.Substring(1);
            }

            public InstanceCallBuilder AssignVariable()
            {
                stringBuilder.AppendLine($"{lowerCaseName} = ");
                return this;
            }
            public InstanceCallBuilder CreateVariable()
            {
                stringBuilder.AppendLine($"{TypeInformation.Name} {lowerCaseName};");
                return this;
            }

            public InstanceCallBuilder CastToOwnType()
            {
                stringBuilder.Append($"({TypeInformation.Name})");
                return this;
            }

            public MethodBuilder MethodCall(string instance, string method, params string[] arguments)
            {
                stringBuilder.AppendLine($"{instance}.{method}({string.Join(",", arguments)});");
                return AppendToParent();
            }

            //public InstanceCallBuilder MemberCall(string memberName)
            //{
            //    stringBuilder.Append($"{InstanceName}.");
            //}

            public MethodBuilder AppendCall()
               => AppendToParent();

            public MethodBuilder PassPropertyInMethodCall(string instance, string method, string castingType = null)
            {
                var casting = castingType == null ? "" : $"({castingType})";
                stringBuilder.AppendLine($"{instance}.{method}({casting}{InstanceName});");
                return AppendToParent();
            }

            public MethodBuilder CallMethodOnProperty(string method, params string[] arguments)
            {
                stringBuilder.AppendLine($"{InstanceName}.{method}({string.Join(",", arguments)});");
                return AppendToParent();
            }



        }

        internal class LoopBuilder : Builder<MethodBuilder>
        {

            public LoopBuilder(MethodBuilder method, StringBuilder builder, string initalCall) : base(method, builder)
            {
                stringBuilder.AppendLine(initalCall);
            }

            public LoopBuilder Open()
            {
                stringBuilder.AppendLine("{");
                return this;
            }

            public MethodBuilder Close()
            {
                stringBuilder.AppendLine("}");
                return parent;
            }
            public override MethodBuilder AppendToParent()
            {
                return parent;
            }
        }

        public enum GenerationLogic
        {
            Serialize,
            Deserialize
        }

    }

    internal abstract class Builder<T> : Builder where T : Builder
    {
        protected readonly T parent;

        public Builder(T parent)
        {
            this.parent = parent;
        }
        public Builder(T parent, StringBuilder builder) : base(builder)
        {
            this.parent = parent;
        }

        public virtual T AppendToParent()
        {
            parent.AppendToThis(ToString());
            return parent;
        }
    }

    internal abstract class Builder
    {
        protected readonly StringBuilder stringBuilder;

        public Builder()
        {
            stringBuilder = new StringBuilder();
        }

        protected Builder(StringBuilder builder)
        {
            stringBuilder = builder;
        }

        internal void AppendToThis(string value)
        {
            stringBuilder.Append(value);
        }

        public override string ToString()
            => stringBuilder.ToString();
    }
}
