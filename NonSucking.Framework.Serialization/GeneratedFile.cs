using System.Collections.Generic;

namespace NonSucking.Framework.Serialization
{
    public record GeneratedFile(string? Namespace, string Name, List<GeneratedType> GeneratedTypes, HashSet<string> Usings)
    {

    }

}
