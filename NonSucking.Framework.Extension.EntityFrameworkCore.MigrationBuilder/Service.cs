using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace NonSucking.Framework.Extension.EntityFrameworkCore.MigrationBuilder;
internal static class Service
{
    public static void Start(FileInfo sourceFile, DirectoryInfo targetDirectory)
    {
        var context = new AssemblyLoadContext("sourceContext");

        using var stream = sourceFile.OpenRead();

        context.LoadFromStream(stream);

        var types 
            = context
                .Assemblies
                .SelectMany(x => x.GetTypes())
                .Where(type => !type.IsAbstract && !type.IsInterface && type.IsAssignableTo(typeof(IEntity)))
                .Where(type => type.GetCustomAttribute<HistoryAttribute>() is null);

        //Todo: create migration class and designer file
        //requires implementation of IAutoMigrationTypeProvider
        //Add: const Id field of patter Number_name example: 1_Initialized
        foreach (var type in types)
        {
            //add types to designer file put History attribute on it
            //example: [History(Migration.Id)]
            foreach (var property in type.GetProperties())
            {

            }
        }
    }
}
