namespace NonSucking.Framework.Serialization;

public static class Diagnostics
{
    public static readonly DiagnosticInfo General = new(
        0001,
        "",
        "{0}"
    );
    public static readonly DiagnosticInfo InstanceCreationImpossible = new(
        0006,
        "",
        "No instance could be created with the constructors in this type. Add a custom ctor call, property mapping or a ctor with matching arguments."
    );
    public static readonly DiagnosticInfo WriteOnlyPropertyUnsupported = new(
        0007,
        "",
        "Properties that are write only are not supported. Implemented a custom serializer method or ignore this property."
    );

    public static readonly DiagnosticInfo CustomMethodParameterNeeded = new(
        0010,
        "",
        $"You must at least provide one argument for {AttributeTemplates.Custom.Name}. Otherwise this value won't be deserialized!"
    );

    public static readonly DiagnosticInfo UnhandledException = new(
        0011,
        "",
        "Error occured while trying to generate serializer code for '{0}' type: {1}\n{2}"
    );

    public static readonly DiagnosticInfo MissingDependenciesForGeneration = new(
        0012,
        "",
        "Missing dependencies for generation of serializer code for '{0}'. Amount: {1}, {2}, {3}"
    );

    public static readonly DiagnosticInfo SerializerIncompatibility = new(
        0013,
        "",
        "{0} can not be serialized because of serializer incompatibility."
    );
    
    public static readonly DiagnosticInfo BaseWillBeShadowed = new(
        0014,
        "",
  "Base Serialize is neither virtual nor abstract and therefore a shadow serialize will be implemented, which might not be wanted. Please consult your doctor or apothecary."
    );

    public static readonly DiagnosticInfo TypeNotSupported = new(
        0015,
        "",
        "The type {0} is currently not supported, no deserialization code will be generated!."
    );

    public static readonly DiagnosticInfo RecursionOnTypeDetected = new(
        0020,
        "",
        "The call stack has reached it's limit, check for recursion on type {0}."
    );

    public static readonly DiagnosticInfo SingletonImplementationRequired = new(
        0021,
        "",
        "Singleton property or field 'Instance' required for type converters."
    );

    public static readonly DiagnosticInfo NoValidConverter = new(
        0022,
        "",
        "No valid converter that can convert {0} '{1}'"
    );

    public static readonly DiagnosticInfo ConverterConvertToNeeded = new(
        0022,
        "",
        "Type to convert to can not be determined, 'ConvertTo' needs to be set explicitely."
    );

    public static readonly DiagnosticInfo IncompatibleCustomSerializer = new(
        0327,
        "",
        "Custom method call is not compatible with serializer of type '{0}'"
    );
    
    public record DiagnosticInfo(int Id, string Title, string FormatString)
    {
        public DiagnosticInfo Format(params object[] args)
        {
            return this with { FormatString = string.Format(FormatString, args) };
        }
    }
}