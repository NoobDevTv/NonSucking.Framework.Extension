// See https://aka.ms/new-console-template for more information
using NonSucking.Framework.Extension.EntityFrameworkCore;

Console.WriteLine("Hello, World!");


public class Example : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; }
}