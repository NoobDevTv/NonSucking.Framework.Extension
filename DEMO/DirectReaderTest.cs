using NonSucking.Framework.Serialization;

namespace DEMO;

[Nooson(DirectReaders = new[] {typeof(DirectReader)})]
public partial class DirectReaderDefaultTest
{
    public int A { get; set; }
}
[Nooson(GenerateDefaultReader = NoosonAttribute.GenerateDefault.No, DirectReaders = new[] {typeof(DirectReader)})]
public partial class DirectReaderWithoutDefaultTest
{
    public int A { get; set; }
}

[Nooson(GenerateDefaultReader = NoosonAttribute.GenerateDefault.No, GenerateDefaultWriter = NoosonAttribute.GenerateDefault.No, DirectReaders = new[] {typeof(DirectReader)})]
public partial class DirectReaderWithoutAnyDefaultTest
{
    public int A { get; set; }
}