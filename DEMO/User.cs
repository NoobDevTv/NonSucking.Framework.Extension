using AutoNotify;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DEMO
{
    //[GenSerialization, JsonConverter(typeof(string))]
    //[Pure]
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [AutoNotify]
        private int seat;

        [AutoNotify]
        private int process;

        public IReadOnlyList<int> Rights => rights;

        private readonly List<int> rights;
    }

    //[Pure, JsonConverter(typeof(string))]
    //public class User2
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }

    //    public IReadOnlyList<int> Rights => rights;

    //    private readonly List<int> rights;
    //}
}
