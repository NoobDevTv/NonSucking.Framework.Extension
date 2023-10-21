namespace NonSucking.Framework.Extension.EntityFrameworkCore.Migrations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
public class HistoryAttribute : Attribute
{
    /// <summary>
    /// The version to migrate to
    /// </summary>
    public string Version { get; }

	/// <summary>
	/// Creates a custom migration config
	/// </summary>
	public HistoryAttribute()
	{
	}
}
