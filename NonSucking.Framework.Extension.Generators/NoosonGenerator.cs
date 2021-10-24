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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using VaVare;
using VaVare.Builders;
using VaVare.Generators.Common;
using VaVare.Generators.Common.Arguments.ArgumentTypes;
using VaVare.Models.References;
using VaVare.Statements;

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
        private enum DeclareOrAndAssign
        {
            DeclareOnly,
            DeclareAndAssign
        }

        private const string writerName = "writer";
        private const string readerName = "reader";
        private const string localVariableSuffix = "___";
        //private const string localVariableSuffix = "___NossonLocalVariable___";
        private Random rand = new Random();
        private readonly List<Diagnostic> diagnostics = new();
        private static readonly NoosonAttributeTemplate genSerializationAttribute = new();
        private static readonly Regex endsWithOurSuffixAndGuid;
        private static readonly string returnValue;

        static NoosonGenerator()
        {
            endsWithOurSuffixAndGuid = new Regex($"{localVariableSuffix}[a-f0-9]{{32}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            returnValue = GetRandomNameFor("returnValue");
        }

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
            //Debugger.Launch();

            try
            {
                InternalExecute(context);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                Debug.Fail(ex.ToString());
                throw;
            }

        }

        private static GeneratorExecutionContext cont;

        private void InternalExecute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            {
                return;
            }
            cont = context;


            foreach (VisitInfo classToAugment in receiver.ClassesToAugment)
            {
                StringBuilder builder = new StringBuilder();
                var methods
                    = GenerateSerializeAndDeserializeMethods(classToAugment);

                var sourceCode
                    = new ClassBuilder(classToAugment.TypeSymbol.Name, classToAugment.TypeSymbol.ContainingNamespace.ToDisplayString())
                    .WithUsings()
                    .WithModifiers(Modifiers.Public, Modifiers.Partial)
                    .WithMethods(methods)
                    .Build();

                //rawSourceText += "You need a attribute for property XY, because it's a duplicate";
                string hintName
                    = $"{classToAugment.TypeSymbol.ToDisplayString()}";

                /*
                                            Schlachtplan
                0. ✓ IEnumerable => Not Supported (yet)
                5. ✓ Derserialize => Serialize Logik rückwärts aufrufen
                7. ✓ CleanUp and Refactor
                2. ✓ Ctor Analyzing => Get Only Props (Simples namematching von Parameter aufgrund von Namen), ReadOnlyProps ohne Ctor Parameter ignorieren
                3. Attributes => Überschreiben von Property Namen zu Ctor Parameter
                4. Custom Type Serializer/Deserializer => Falls etwas not supported wird, wie IReadOnlySet, IEnumerable
                8. Support for Dictionaries
                1. Listen: IEnumerable => List, IReadOnlyCollection => ReadOnlyCollection, IReadOnlyDictionary => ReadOnlyDictionary
                6. Fehler/Warnings ausgeben

                *: ✓ Serialize bzw. Deserialize auf List Items aufrufen wenn möglich
                *: ✓ Randomize der count variable beim Deserialize
                *: ✓ Serialize Member access
                *: ✓ Randomize var item name
                *: ✓ List filter indexer
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
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryWriter {writerName}");
            var body
                = CreateBlock(visitInfo, MethodType.Serialize);

            return new MethodBuilder("Serialize")
                .WithModifiers(Modifiers.Public)
                //.WithReturnType(null)
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        private static BaseMethodDeclarationSyntax GenerateDeserializeMethod(VisitInfo visitInfo, string readerName)
        {
            var parameter = SyntaxFactory.ParseParameterList($"System.IO.BinaryReader {readerName}");
            var body
                = CreateBlock(visitInfo, MethodType.Deserialize);

            return new MethodBuilder("Deserialize")
                .WithModifiers(Modifiers.Public, Modifiers.Static)
                .WithReturnType(SyntaxFactory.ParseTypeName(visitInfo.TypeSymbol.Name))
                .WithParameters(parameter.Parameters.ToArray())
                .WithBody(body)
                .Build();
        }

        private static BlockSyntax CreateBlock(VisitInfo visitInfo, MethodType methodType)
        {
            var statements
                = visitInfo
                .Properties
                .SelectMany(propertieTypeGroup => GenerateStatementsForProps(propertieTypeGroup.Properties, methodType))
                .Where(statement => statement is not null)
                .ToList();

            if (methodType == MethodType.Deserialize)
            {
                var ret = CallCtorAndSetProps(visitInfo.TypeSymbol, statements, returnValue, DeclareOrAndAssign.DeclareAndAssign);
                statements.Add(ret);

                var returnStatement
                    = SyntaxFactory
                    .ParseStatement($"return {returnValue};");

                statements.Add(returnStatement);
            }
            return BodyGenerator.Create(statements.ToArray());
        }

        private static StatementSyntax CallCtorAndSetProps(INamedTypeSymbol typeSymbol, ICollection<StatementSyntax> statements, string instanceName, DeclareOrAndAssign declareAndAssign)
        {
            var constructors
                = typeSymbol
                .Constructors
                .OrderByDescending(constructor => constructor.Parameters.Length)
                .ToList();

            var localDeclarations
                = statements
                .OfType<LocalDeclarationStatementSyntax>()
                .Concat(
                    statements
                    .OfType<BlockSyntax>()
                    .SelectMany(x => x.Statements.OfType<LocalDeclarationStatementSyntax>())
                 )
                .SelectMany(declaration => declaration.Declaration.Variables)
                .Select(variable => variable.Identifier.Text)
                .Where(text => text.StartsWith("@"))
                .Select(text => text.Substring(1))
                .ToList();

            var currentType
                = SyntaxFactory
                .ParseTypeName(typeSymbol.ToDisplayString());


            if (typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.IsAbstract)
            {
                return Statement
                 .Declaration
                 .Assign(instanceName, SyntaxFactory.ParseTypeName(" default"));
            }

            var ctorCallStatement
                = GetStatementForCtorCall(constructors, localDeclarations, currentType, instanceName, declareAndAssign, out var ctorArguments);

            var propertyAssignments
                = AssignMissingSetterProperties(typeSymbol, localDeclarations, ctorArguments, instanceName);

            return GetBlockWithoutBraces(new StatementSyntax[] { ctorCallStatement, propertyAssignments });
        }


        private static StatementSyntax AssignMissingSetterProperties(ITypeSymbol typeSymbol, List<string> localDeclarations, List<string> ctorArguments, string variableName)
        {

            //TODO Set Public props which have a set method via !IPropertySymbol.IsReadOnly
            localDeclarations
                = localDeclarations
                .Where(declaration =>
                    !ctorArguments.Any(argument => MatchIdentifierWithPropName(argument, declaration))
                )
                .ToList();

            var properties
                = typeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property =>
                    !property.IsReadOnly
                    && property.SetMethod is not null
                    && !ctorArguments.Any(argument => MatchIdentifierWithPropName(argument, property.Name))
                );

            var blockStatements = new List<StatementSyntax>();

            foreach (var property in properties)
            {
                var variableReference
                     = new VariableReference(variableName, new MemberReference(property.Name));

                var declaration
                    = localDeclarations
                    .FirstOrDefault(declaration => MatchIdentifierWithPropName(declaration, property.Name));

                if (declaration is null)
                {
                    continue;
                }

                VariableReference declarationReference;

                if (property.Type.TypeKind == TypeKind.Array)
                {
                    var method = new MethodReference("ToArray");
                    declarationReference = new MemberReference(declaration, method);
                }
                else
                {
                    declarationReference = new VariableReference(declaration);
                }

                var statement
                    = Statement
                    .Declaration
                    .Assign(variableReference, declarationReference);

                blockStatements.Add(statement);
            }


            return GetBlockWithoutBraces(blockStatements);

        }

        private static StatementSyntax GetStatementForCtorCall(List<IMethodSymbol> constructors, List<string> localDeclarations, TypeSyntax currentType, string instanceName, DeclareOrAndAssign declareAndAssign, out List<string> ctorArguments)
        {
            ctorArguments = new List<string>();
            if (constructors.Count == 0)
            {
                var arguments = SyntaxFactory.ParseArgumentList("()");

                return DeclareAssignCtor(currentType, instanceName, declareAndAssign, arguments);
            }

            foreach (var constructor in constructors)
            {
                bool constructorMatch = true;

                foreach (var parameter in constructor.Parameters)
                {


                    var matchedDeclaration
                        = localDeclarations
                        .FirstOrDefault(identifier => MatchIdentifierWithPropName(identifier, parameter.Name));

                    if (string.IsNullOrWhiteSpace(matchedDeclaration))
                    {
                        constructorMatch = false;
                        break;
                    }

                    ctorArguments.Add(matchedDeclaration);
                }


                if (constructorMatch)
                {
                    var ctorArgumentsString
                        = string.Join(",", ctorArguments);

                    var arguments
                        = SyntaxFactory
                        .ParseArgumentList($"({ctorArgumentsString})");

                    //var semanticModel = cont.Compilation.GetSemanticModel(currentType.SyntaxTree);

                    //var symb = semanticModel.GetSymbolInfo(currentType);

                    //var text = currentType.GetText();
                    return DeclareAssignCtor(currentType, instanceName, declareAndAssign, arguments);

                }
                else
                {
                    ctorArguments.Clear();
                }
            }

            //TODO diagnostic
            throw new NotSupportedException();
        }

        private static StatementSyntax DeclareAssignCtor(TypeSyntax currentType, string instanceName, DeclareOrAndAssign declareAndAssign, ArgumentListSyntax arguments)
        {
            if (declareAndAssign == DeclareOrAndAssign.DeclareAndAssign)
            {
                return Statement
                    .Declaration
                    .DeclareAndAssign(instanceName, currentType, arguments);
            }
            else if (declareAndAssign == DeclareOrAndAssign.DeclareOnly)
            {
                return Statement
                    .Declaration
                    .Assign(instanceName, currentType, arguments);
            }
            else
            {
                //TODO: Error
                return null;
            }
        }

        private static IEnumerable<StatementSyntax> GenerateStatementsForProps(ICollection<MemberInfo> properties, MethodType methodType)
        {
            NoosonIgnoreAttributeTemplate ignore = new NoosonIgnoreAttributeTemplate();

            foreach (MemberInfo property in properties)
            {
                ITypeSymbol propertyType = property.TypeSymbol;

                string propertyName = property.Name;
                //int index = propertyName.IndexOf('.');

                //if (index >= 0)
                //{
                //    continue;
                //}

                if (property.Symbol.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == ignore.FullName))
                {
                    continue;
                }

                yield return methodType switch
                {
                    MethodType.Serialize => CreateStatementForSerializing(property, writerName),
                    MethodType.Deserialize => CreateStatementForDeserializing(property, readerName),
                    _ => throw new NotSupportedException($"{methodType} is not supported by Property generation")
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

        private static StatementSyntax CreateStatementForSerializing(MemberInfo property, string writerName)
        {
            StatementSyntax statement;
            var success
                        = TrySpecialTypeWriterCall(property, writerName, out statement)
                           || TryEnumWriterCall(property, writerName, out statement)
                           || TrySerializeWriterCall(property, writerName, out statement)
                           || TrySerializeList(property, writerName, out statement)
                           || TrySerializePublicProps(property, writerName, out statement)
                           ;

            return statement;
        }

        private static StatementSyntax CreateStatementForDeserializing(MemberInfo property, string readerName)
        {
            StatementSyntax statement;
            var success
                        = TrySpecialTypeReaderCall(property, readerName, out statement)
                           || TryEnumReaderCall(property, readerName, out statement)
                           || TryDeserializeReaderCall(property, readerName, out statement)
                           || TryDeserializeList(property, readerName, out statement)
                           || TryDeserializePublicProps(property, readerName, out statement)
                           ;

            return statement;
        }



        #region Serialize

        private static bool TrySpecialTypeWriterCall(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            var type = property.TypeSymbol;
            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    ValueArgument argument = GetValueArgumentFrom(property);

                    statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement();
                    return true;
                default:
                    statement = null;
                    return false;
            }
        }

       

        private static bool TryEnumWriterCall(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;

            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                ValueArgument argument = GetValueArgumentFrom(property, typeSymbol.EnumUnderlyingType);

                statement
                        = Statement
                        .Expression
                        .Invoke(writerName, "Write", arguments: new[] { argument })
                        .AsStatement();
                return true;
            }
            else
            {
                return false;
            }

        }

        private static bool TrySerializeList(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;
            bool isIEnumerable
                = type
                .AllInterfaces
                .Any(x => x.Name == typeof(IEnumerable).Name);

            if (!isIEnumerable)
            {
                return false;
            }

            bool isIEnumerableInterfaceSelf
                = type.Name == nameof(IEnumerable);

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

            string itemName = GetRandomNameFor("item");

            //TryGeneratePublicPropsLines(builder, writerName, true);
            //TODO

            StatementSyntax[] statements = Array.Empty<StatementSyntax>();


            if ((int)genericArgument.SpecialType is >= 7 and <= 20) //List<string>, List<int>
            {
                statements
                    = new[]{
                        CreateStatementForSerializing(
                            new MemberInfo(genericArgument, genericArgument, itemName),
                            writerName
                        )
                    };
            }
            else
            {
                var genericInfo = new[] { new MemberInfo(genericArgument, genericArgument, itemName) };
                statements
                    = GenerateStatementsForProps(genericInfo, MethodType.Serialize)
                    .ToArray();
            }


            var memberReference
                = new MemberReference(
                type.TypeKind == TypeKind.Array
                    ? "Length"
                    : "Count");
            var countRefernce = new ReferenceArgument(new VariableReference(GetMemberAccessString(property), memberReference));


            var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(writerName, nameof(BinaryWriter.Write), arguments: new[] { countRefernce })
                        .AsStatement();


            var iterationStatement
                = Statement
                .Iteration
                 .ForEach(itemName, typeof(void), GetMemberAccessString(property), BodyGenerator.Create(statements), useVar: true);


            statement = GetBlockWithoutBraces(new StatementSyntax[] { invocationExpression, iterationStatement });


            return true;
        }

        private static bool TrySerializeWriterCall(MemberInfo property, string writerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

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
                        .Invoke(GetMemberAccessString(property), "Serialize", arguments: new[] { new ValueArgument((object)writerName) })
                        .AsStatement();
            }

            return isUsable;
        }

        private static bool TrySerializePublicProps(MemberInfo memberInfo, string readerName, out StatementSyntax statement)
        {
            var props
                = memberInfo
                .TypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.Name != "this[]");

            var statements
                = GenerateStatementsForProps(
                    props
                        .Select(x => new MemberInfo(x.Type, x, x.Name, memberInfo.Name))
                        .ToArray(),
                    MethodType.Serialize

                );
            statement = GetBlockWithoutBraces(statements);
            return true;

        }

        #endregion

        #region Deserialize

        private static bool TrySpecialTypeReaderCall(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            var type = property.TypeSymbol;

            switch ((int)type.SpecialType)
            {
                case >= 7 and <= 20:
                    string memberName = $"@{GetRandomNameFor(property.Name)}";

                    var invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, GetReadMethodCallFrom(type.SpecialType))
                        .AsExpression();

                    statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign(memberName, invocationExpression);

                    return true;
                default:
                    statement = null;
                    return false;
            }
        }
        private static bool TryEnumReaderCall(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

            if (type.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            if (type is INamedTypeSymbol typeSymbol)
            {
                SpecialType specialType = typeSymbol.EnumUnderlyingType.SpecialType;
                string localName = $"@{GetRandomNameFor(property.Name)}";

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
                    .DeclareAndAssign(localName, invocationExpression);

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

        private static bool TryDeserializeReaderCall(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

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
                        .Invoke(type.ToString(), "Deserialize", arguments: new[] { new ValueArgument((object)readerName) })
                        .AsExpression();

                statement
                        = Statement
                        .Declaration
                        .DeclareAndAssign($"@{GetRandomNameFor(property.Name)}", invocationExpression);
            }

            return isUsable;
        }

        private static bool TryDeserializeList(MemberInfo property, string readerName, out StatementSyntax statement)
        {
            statement = null;
            var type = property.TypeSymbol;

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

            var randomForThisScope = GetRandomNameFor("");

            List<StatementSyntax> statements = new();
            var listVariableName = $"{genericArgument.Name}{localVariableSuffix}{randomForThisScope}";

            if ((int)genericArgument.SpecialType is >= 7 and <= 20) //List<string>, List<int>
            {
                statements.Add(CreateStatementForDeserializing(
                            new MemberInfo(genericArgument, genericArgument, genericArgument.Name),
                            readerName
                        ));
                var localDeclerationSyntax = statements[0] as LocalDeclarationStatementSyntax;
                listVariableName = localDeclerationSyntax.Declaration.Variables.First().Identifier.ToFullString();
            }
            else
            {
                var genericInfo = new[] { new MemberInfo(genericArgument, genericArgument, listVariableName) };
                var gsfp = GenerateStatementsForProps(genericInfo, MethodType.Deserialize);
                statements.AddRange(gsfp);
            }

            var listName = $"@{GetRandomNameFor(property.Name)}";

            var addStatement
                = Statement
                .Expression
                .Invoke(listName, $"Add", arguments: new[] { new ValueArgument((object)listVariableName) })
                .AsStatement();

            statements.Add(addStatement);

            var start = new VariableReference("0");
            var end = new VariableReference(GetRandomNameFor("count" + property.Name));


            ExpressionSyntax invocationExpression
                        = Statement
                        .Expression
                        .Invoke(readerName, nameof(BinaryReader.ReadInt32))
                        .AsExpression();

            var countStatement
                = Statement
                .Declaration
                .DeclareAndAssign(end.Name, invocationExpression);

            ExpressionSyntax ctorInvocationExpression
                = Statement
                .Expression
                .Invoke($"new System.Collections.Generic.List<{genericArgument}>", arguments: new[] { new ValueArgument((object)end.Name) })
                .AsExpression();

            var listStatement
                = Statement
                .Declaration
                .DeclareAndAssign(listName, ctorInvocationExpression);

            var iterationStatement
                = Statement
                .Iteration
                .For(start, end, GetRandomNameFor("i"), BodyGenerator.Create(statements.ToArray()));

            statement
                = GetBlockWithoutBraces(new StatementSyntax[] { countStatement, listStatement, iterationStatement });

            return true;
        }

        private static bool TryDeserializePublicProps(MemberInfo memberInfo, string readerName, out StatementSyntax statement)
        {
            var props
                = memberInfo
                .TypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.Name != "this[]");
            string randomForThisScope = GetRandomNameFor("");
            var statements
                = GenerateStatementsForProps(
                    props
                        .Select(x => new MemberInfo(x.Type, x, $"{x.Name}"))
                        .ToArray(),
                    MethodType.Deserialize
                ).ToList();

            string memberName = $"@{GetRandomNameFor(memberInfo.Name)}";

            var declaration
                = Statement
                .Declaration
                .Declare(memberName, SyntaxFactory.ParseTypeName(memberInfo.TypeSymbol.ToDisplayString()));


            var ctorSyntax = CallCtorAndSetProps((INamedTypeSymbol)memberInfo.TypeSymbol, statements.ToArray(), memberName, DeclareOrAndAssign.DeclareOnly);
            statements.Add(ctorSyntax);
            statement = GetBlockWithoutBraces(new StatementSyntax[] { declaration, SyntaxFactory.Block(SyntaxFactory.List(statements)), });
            return true;
        }

        #endregion

        static SyntaxToken openEmpty = SyntaxFactory.Token(default, SyntaxKind.OpenBraceToken, "", "", default);
        static SyntaxToken closeEmpty = SyntaxFactory.Token(default, SyntaxKind.CloseBraceToken, "", "", default);
        private static StatementSyntax GetBlockWithoutBraces(IEnumerable<StatementSyntax> statements)
        {
            StatementSyntax statement;

            statement
                = SyntaxFactory.Block(openEmpty, SyntaxFactory.List(statements.ToArray()), closeEmpty);
            return statement;
        }
        public enum MethodType
        {
            Serialize,
            Deserialize
        }

        private static bool MatchIdentifierWithPropName(string identifier, string parameterName)
        {
            var index = identifier.IndexOf(localVariableSuffix);
            if (index > -1)
                identifier = identifier.Remove(index);

            return string.Equals(identifier, parameterName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRandomNameFor(string variableName)
        {
            if (endsWithOurSuffixAndGuid.IsMatch(variableName))
                return variableName;

            return $"{variableName}{localVariableSuffix}{Guid.NewGuid().ToString("N")}";
        }

        private static ValueArgument GetValueArgumentFrom(MemberInfo memberInfo, ITypeSymbol castTo = null)
        {
            object referenceValue = GetMemberAccessString(memberInfo, castTo);
            return new ValueArgument(referenceValue);
        }

        private static string GetMemberAccessString(MemberInfo memberInfo, ITypeSymbol castTo = null)
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

        private static string Cast(string value, string castType = null)
        {
            if (!string.IsNullOrWhiteSpace(castType))
                return $"({castType}){value}";

            return value;
        }
    }

}
