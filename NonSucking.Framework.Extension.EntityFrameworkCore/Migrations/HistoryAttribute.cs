namespace NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
public class HistoryAttribute : Attribute
{
    public string Version { get;  }

	public HistoryAttribute(string version)
	{
		Version = version;
	}
}
