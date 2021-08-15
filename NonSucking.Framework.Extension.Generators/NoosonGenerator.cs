using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using NonSucking.Framework.Extension.Generators.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Testura.Code;
using Testura.Code.Builders;
using Testura.Code.Generators.Common;
using Testura.Code.Generators.Common.Arguments.ArgumentTypes;
using Testura.Code.Models.References;
using Testura.Code.Statements;

namespace NonSucking.Framework.Extension.Generators
{
    /*
     Unsere Attribute:
    1. Ignore
    2. Property name for ctor / Ctor Attribute for property name
    3. PrefferedCtor
    4. Custom Serialize/Deserialize
    
    Future:
    5. BinaryWriter / Span Switch/Off/On
     */


    [Generator]
    public class NoosonGenerator : ISourceGenerator
    {
        private const string writerName = "writer";
        private const string readerName = "reader";
        private readonly List<Diagnostic> diagnostics = new();
        private static readonly NoosonAttributeTemplate genSerializationAttribute = new();

        public NoosonGenerator()
        {

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            try
            {
                List<Template> templates
                = Assembly
                .GetAssembly(typeof(Template))
                .GetTypes()
                .Where(t => typeof(Template).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => (Template)Activator.CreateInstance(t))
                .ToList();


                context.RegisterForPostInitialization(i =>
                {
                    foreach (Template template in templates)
                    {
                        i.AddSource(template.Name, template.ToString());
                    }
                });

                context.RegisterForSyntaxNotifications(() => new SyntaxReceiver(genSerializationAttribute));
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                InternalExecute(context);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }

        }

        private void InternalExecute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            {
                return;
            }

            INamedTypeSymbol attributeSymbol
                = context
                    .Compilation
                    .GetTypeByMetadataName(genSerializationAttribute.FullName);


            foreach (VisitInfo classToAugment in receiver.ClassesToAugment)
            {
                StringBuilder builder = new StringBuilder();
                var methods
                    = GenerateSerializeAndDeserializeMethods(classToAugment);


                var sourceCode
                    = new ClassBuilder(classToAugment.TypeSymbol.Name, classToAugment.TypeSymbol.ContainingNamespace.ToDisplayString())
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .WithMethods(methods)
                    .Build();

                //rawSourceText += "You need a attribute for property XY, because it's a duplicate";
                string hintName
                    = $"{classToAugment.TypeSymbol.ToDisplayString()}";

                /*
                                            Schlachtplan
                0. IEnumerable => Not Supported (yet)
                5. Derserialize => Serialize Logik rückwärts aufrufen
                2. Ctor Analyzing => Get Only Props (Simples namematching von Parameter aufgrund von Namen), ReadOnlyProps ohne Ctor Parameter ignorieren
                3. Attributes => Überschreiben von Property Namen zu Ctor Parameter
                4. Custom Type Serializer/Deserializer => Falls etwas not supported wird, wie IReadOnlySet, IEnumerable
                1. Listen: IEnumerable => List, IReadOnlyCollection => ReadOnlyCollection, IReadOnlyDictionary => ReadOnlyDictionary
                6. Fehler/Warnings ausgeben
                7. CleanUp and Refactor
                 */

                using var workspace = new AdhocWorkspace();
                var options = workspace.Options;
                var formattedText = Formatter.Format(sourceCode, workspace, options).ToFullString();

                context.AddSource(hintName, formattedText);

            }

            foreach (Diagnostic diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static BaseMethodDeclarationSyntax[] GenerateSerializeAndDeserializeMethods(VisitInfo visitInfo)
            => new[] {
                GenerateSerializeMethod(visitInfo, writerName),
                GenerateDeserializeMethod(visitInfo, readerName)
            };

        private static BaseMethodDeclarationSyntax GenerateSerializeMethod(VisitInfo visitInfo, string writerName)
        {
            var parameter = SyntaxFactory.ParseParameterList($"BinaryWriter {writerName}");
            var body
                = CreateBlock(visitInfo, MethodType.Serialize);

            return new MethodBuilder("Serialize")
                .WithModifiers(Modifiers.Public)
                .WithReturnType(typeof(void))
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        private static BaseMethodDeclarationSyntax GenerateDeserializeMethod(VisitInfo visitInfo, string readerName)
        {
            var parameter = SyntaxFactory.ParseParameterList($"BinaryReader {readerName}");
            var body
                = CreateBlock(visitInfo, MethodType.Deserialize);

            return new MethodBuilder("Deserialize")
                .WithModifiers(Modifiers.Public, Modifiers.Static)
                .WithReturnType(typeof(void)) //TODO: typename Testura overload hinzufügen type as string
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        private static BlockSyntax CreateBlock(VisitInfo visitInfo, MethodType methodType)
        {
            var statements
                = visitInfo
                .Properties
                .SelectMany(propertieTypeGroup => GenerateStatementsForProps(propertieTypeGroup, methodType))
                .Where(statement => statement is not null)
                .ToArray();

            return BodyGenerator
                .Create(statements);
        }

        private static IEnumerable<StatementSyntax> GenerateStatementsForProps(TypeGroupInfo typeGroupInfo, MethodType methodType, string parentName = null)
        {
            NoosonIgnoreAttributeTemplate ignore = new NoosonIgnoreAttributeTemplate();

            foreach (PropertyInfo property in typeGroupInfo.Properties)
            {
                ITypeSymbol propertyType = property.PropertySymbol.Type;

                string propertyName = property.PropertySymbol.Name;
                int index = propertyName.IndexOf('.');


                if (index >= 0)
                {
                    continue;
                }

                if (property.PropertySymbol.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == ignore.FullName))
                {
                    continue;
                }

                yield return methodType switch
                {
                    MethodType.Serialize => CreateStatementForSerializing(property, writerName),
                    MethodType.Deserialize => CreateStatementForDeserializing(property, readerName),
                };

            }

        }



        //Proudly stolen from https://github.com/mknejp/dotvariant/blob/c59599a079637e38c3471a13b6a0443e4e607058/src/dotVariant.Generator/Diagnose.cs#L234
        private static Diagnostic MakeDiagnostic(string id, string title, string message, Location location, DiagnosticSeverity severity, string helpLinkurl = null, params string[] customTags)
        {
            return Diagnostic.Create(
                   new DiagnosticDescriptor(
                       $"{nameof(NoosonGenerator)}.{id}",
                       title,
                       message,
                       nameof(NoosonGenerator),
                       severity,
                       true,
                       helpLinkUri: helpLinkurl,
                       customTags: customTags),
                   location);
        }

        private static StatementSyntax CreateStatementForSerializing(PropertyInfo property, string writerName)
        {
            StatementSyntax statement;
            var success
                        = TrySpecialTypeWriterCall(property, writerName, out statement)
                           || TryEnumWriterCall(property, writerName, out statement)
                           || TrySerializeWriterCall(property, writerName, out statement)
                           || TrySerializeList(property, writerName, out statement)
                           //|| TryGeneratePublicPropsLines(builder, writerName, true)
                           ;

            return statement;
        }

        private static StatementSyntax CreateStatementForDeserializing(PropertyInfo property, string readerName)
        {
            StatementSyntax statement;
            var success
                        = TrySpecialTypeReaderCall(property, readerName, out statement)
                           || TryEnumReaderCall(property, readerName, out statement)
                           || TryDeserializeReaderCall(property, readerName, out statement)
                           || TryDeserializeList(property, readerName, out statement)
                           //|| TryGeneratePublicPropsLines(builder, readerName, false)
                           ;

            return statement;
        }

        #region Serialize

        private static bool TrySpecialTypeWriterCall(PropertyInfo property, string writerName, out StatementSyntax statement)
        {
            var type = property.PropertySymbol.Type;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { new ValueArgument((object)property.PropertySymbol.Name) })
                        .AsStatement();
                    return true;
                default:
                    statement = null;
                    return false;
            }
        }

        private static bool TryEnumWriterCall(PropertyInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;

            var type = property.PropertySymbol.Type;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                object valueCall
                    = $"({typeSymbol.EnumUnderlyingType}){property.PropertySymbol.Name}";

                statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { new ValueArgument(valueCall) })
                        .AsStatement();
                return true;
            }
            else
            {
                return false;
            }

        }

        private static bool TrySerializeList(PropertyInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.PropertySymbol.Type;
            bool isIEnumerable
                = type
                .AllInterfaces
                .Any(x => x.Name == typeof(IEnumerable).Name);

            if (!isIEnumerable)
            {
                return false;
            }

            bool isIEnumerableInterfaceSelf = type.Name == nameof(IEnumerable);
            if (isIEnumerableInterfaceSelf)
            {
                //Diagnostic Error for not supported type
                return true;
            }

            ITypeSymbol genericArgument;
            if (type is INamedTypeSymbol nts)
            {
                genericArgument = nts.TypeArguments[0];
            }
            else if (type is IArrayTypeSymbol ats)
            {
                genericArgument = ats.ElementType;
            }
            else
            {
                throw new NotSupportedException();
            }

            const string itemName = "item";

            //TryGeneratePublicPropsLines(builder, writerName, true);
            //TODO


            //MethodBuilder mb = builder.AppendToParent();
            //LoopBuilder loop = mb
            //    .ForEach(itemName, builder.InstanceName)
            //    .Open();


            //PropertyInfo[] props = genericArgument
            //    .GetMembers()
            //    .OfType<IPropertySymbol>()
            //        .Select(x => new PropertyInfo(null, x))
            //        .ToArray();


            //StatementSyntax[] bodySyntax;
            //if (props.Length > 0)
            //{
            //    GenerateLineForProps(mb, new TypeGroupInfo(null, default, props), itemName);
            //}
            //else //List<string>, List<int>
            //{
            //    InstanceCallBuilder instanceBuilder = mb.GetInstance(genericArgument, itemName);
            //    TrySerializing(instanceBuilder, writerName);
            //}

            //var loopBlock
            //     = BodyGenerator
            //     .Create(bodySyntax);

            //statement
            //    = Statement
            //    .Iteration
            //    .ForEach(itemName, typeof(void), property.PropertySymbol.Name, loopBlock, useVar: true);

            return true;
        }

        private static bool TrySerializeWriterCall(PropertyInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.PropertySymbol.Type;

            IEnumerable<IMethodSymbol> member
                = type
                    .GetMembers("Serialize")
                    .OfType<IMethodSymbol>();

            bool shouldBeGenerated
                = type
                .GetAttributes()
                .Any(x => x.ToString() == genSerializationAttribute.FullName);

            bool isUsable
                = shouldBeGenerated || member
                .Any(m =>
                    m.Parameters.Length == 1
                    && m.Parameters[0].ToDisplayString() == typeof(BinaryWriter).FullName
                );

            if (isUsable)
            {
                statement
                        = Statement
                        .Expression
                        .Invoke(property.PropertySymbol.Name, "Serialize", arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }

            return isUsable;
        }

        #endregion

        #region Deserialize

        private static bool TrySpecialTypeReaderCall(PropertyInfo property, string readerName, out StatementSyntax statement)
        {
            var type = property.PropertySymbol.Type;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    string memberName = property.PropertySymbol.Name;

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();

                    statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign(memberName, typeof(void), invocationExpression);

                    return true;
                default:
                    statement = null;
                    return false;
            }
        }
        private static bool TryEnumReaderCall(PropertyInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.PropertySymbol.Type;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                SpecialType specialType = typeSymbol.EnumUnderlyingType.SpecialType;
                string typeName = typeSymbol.Name;

                ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, GetReadMethodCallFrom(specialType))
                        .AsExpression();

                var typeSyntax
                    = SyntaxFactory
                    .ParseTypeName(typeSymbol.Name);

                invocationExpression
                    = SyntaxFactory
                    .CastExpression(typeSyntax, invocationExpression);

                statement
                    = Statement
                    .Declaration
                    .DeclareAndAssign(typeName, typeof(void), invocationExpression);

                return true;
            }
            else
            {
                return false;
            }

        }

        private static string GetReadMethodCallFrom(SpecialType specialType)
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

        private static bool TryDeserializeReaderCall(PropertyInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.PropertySymbol.Type;

            IEnumerable<IMethodSymbol> member
                = type
                .GetMembers("Deserialize")
                .OfType<IMethodSymbol>();

            bool shouldBeGenerated
                = type
                .GetAttributes()
                .Any(x => x.ToString() == genSerializationAttribute.FullName);

            bool isUsable
                = shouldBeGenerated
                    || member
                        .Any(m =>
                            m.Parameters.Length == 1
                            && m.Parameters[0].ToDisplayString() == typeof(BinaryReader).FullName
                            && m.IsStatic
                        );

            if (isUsable)
            {
                var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(type.Name, "Deserialize")
                        .AsExpression();

                statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign(property.PropertySymbol.Name, typeof(void), invocationExpression);
            }

            return isUsable;
        }

        private static bool TryDeserializeList(PropertyInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.PropertySymbol.Type;

            bool isEnumerable
                = type
                .AllInterfaces
                .Any(x => x.Name == typeof(IEnumerable).Name);

            if (!isEnumerable)
            {
                return false;
            }

            bool isIEnumerableSelf = type.Name == nameof(IEnumerable);
            if (isIEnumerableSelf)
            {
                //Diagnostic Error for not supported type
                return true;
            }

            ITypeSymbol genericArgument;
            if (type is INamedTypeSymbol nts)
            {
                genericArgument = nts.TypeArguments[0];
            }
            else if (type is IArrayTypeSymbol ats)
            {
                genericArgument = ats.ElementType;
            }
            else
            {
                throw new NotSupportedException();
            }

            const string itemName = "item";
            //TODO

            //TryGeneratePublicPropsLines(builder, writerName, false);
            //MethodBuilder mb = builder.AppendToParent();
            //LoopBuilder loop = mb
            //    .For("int i = 0", "i < count", "i++")
            //    .Open();

            PropertyInfo[] props = genericArgument
                .GetMembers()
                .OfType<IPropertySymbol>()
                    .Select(x => new PropertyInfo(null, x))
                    .ToArray();

            StatementSyntax[] statements = Array.Empty<StatementSyntax>();

            if (props.Length > 0)
            {
                statements
                    = GenerateStatementsForProps(new TypeGroupInfo(null, default, props), MethodType.Deserialize, itemName)
                    .ToArray();
            }
            else //List<string>, List<int>
            {
                //InstanceCallBuilder instanceBuilder = mb.GetInstance(genericArgument, itemName);
                //CreateStatementForDeserializing(props[0], readerName);
            }

            var start = new VariableReference("0");
            var end = new VariableReference("count");

            statement
                = Statement
                .Iteration
                .For(start, end, "i", BodyGenerator.Create(statements));

            return true;
        }



        #endregion

        //private static bool TryGeneratePublicPropsLines(InstanceCallBuilder builder, bool serialize)
        //{
        //    if (builder.TypeInformation.TypeKind == TypeKind.Interface)
        //    {
        //        var success = true;
        //        //message.Test = asd;
        //        //message.ABC = 123;


        //        //typeNameSymbol.Name == nameof(IEnumerable);

        //        foreach (var interfaceType in builder.TypeInformation.AllInterfaces)
        //        {

        //            success = PropertiesForSingleType(builder, interfaceType, builder.InstanceName, serialize);
        //            //diagnostics.Add(MakeDiagnostic(
        //            //    "🤷", 
        //            //    "Unexpected error during generation", 
        //            //    $"Property {memberName} needs a name property, because it is a duplicate", 
        //            //    typeNameSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(), 
        //            //    DiagnosticSeverity.Error,
        //            //    "https://lmgtfy.app/?q=Property+c%23&iie=1"));

        //            if (!success)
        //                return success;
        //        }

        //        return success;
        //    }
        //    else
        //    {
        //        return PropertiesForSingleType(builder, typeNameSymbol, memberName, serialize);
        //    }
        //}

        //private bool PropertiesForSingleType(InstanceCallBuilder builder, bool serialize)
        //{
        //    var props = builder.TypeInformation
        //                    .GetMembers()
        //                    .OfType<IPropertySymbol>()
        //                    .Where(property => property.Name != "this[]");

        //    return GenerateLineForProps(builder,
        //        new TypeGroupInfo(
        //            null,
        //            new SymbolInfo(),
        //            props
        //                .Select(x => new PropertyInfo(null, x))
        //                .ToArray()
        //        ),
        //        serialize,
        //        builder.InstanceName
        //    );
        //}

        public enum MethodType
        {
            Serialize,
            Deserialize
        }


    }
}
