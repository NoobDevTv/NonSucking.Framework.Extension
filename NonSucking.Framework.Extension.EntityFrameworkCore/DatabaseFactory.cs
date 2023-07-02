using System.Reflection;
using System.Runtime.Loader;

namespace NonSucking.Framework.Extension.EntityFrameworkCore
{

    public static class DatabaseFactory
    {
        private static AssemblyLoadContext? assemblyLoadContext;

        public static List<IDatabaseConfigurator> DatabaseConfigurators { get; } = new();

        /// <summary>
        /// Needs to be called, so that the <see cref="DatabaseConfigurators"/> are filled
        /// </summary>
        /// <param name="source">Path to the dll which contains the <see cref="IDatabaseConfigurator"/> implementation(s)</param>
        public static void Initialize(string source)
        {
            if (DatabaseConfigurators.Any(x => source.Contains(x.Name, StringComparison.OrdinalIgnoreCase)))
                return;

            if (assemblyLoadContext is null)
            {
                assemblyLoadContext = AssemblyLoadContext.GetLoadContext(typeof(IDatabaseConfigurator).Assembly);
                assemblyLoadContext.Resolving += Default_Resolving;
            }
            var fullName = new FileInfo(source).FullName;
            var databasePlugin = assemblyLoadContext.LoadFromAssemblyPath(fullName);

            //Get All IDatabaseConfigurator
            DatabaseConfigurators
                .AddRange(databasePlugin
                .GetTypes()
                .Where(x => x.IsAssignableTo(typeof(IDatabaseConfigurator)) && x.GetConstructor(Array.Empty<Type>()) != null)
                .Select(x => (IDatabaseConfigurator)Activator.CreateInstance(x)));
        }

        private static Assembly? Default_Resolving(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name?.EndsWith("resources") ?? false)
                return null;

            var existing = context.Assemblies.FirstOrDefault(x => x.FullName == name.FullName);
            if (existing is not null)
                return existing;
            string assemblyPath = new FileInfo($"{name.Name}.dll").FullName;
            if (assemblyPath is not null)
                return context.LoadFromAssemblyPath(assemblyPath);
            return null;
        }
    }
}
