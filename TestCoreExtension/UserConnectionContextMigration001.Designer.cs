using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

using NonSucking.Framework.Extension.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCoreExtension;


#nullable enable
[History]
public partial class UserConnectionContextMigration001 : IAutoMigrationTypeProvider
{
    public const string Id = $"2022_10_23-20_39_01-{nameof(DbContext2)}-InitialMigration";
    public IReadOnlyList<Type> GetEntityTypes()
    {
        return new[]
        {
            typeof(Example),
        };
    }

    [Table("Example")]
    private class Example : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

}
