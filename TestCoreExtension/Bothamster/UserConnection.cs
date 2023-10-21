using System.ComponentModel.DataAnnotations.Schema;

namespace TestCoreExtension.Bothamster;

[Table("UserConnections")]
public class UserConnection : IdEntity<int>
{
    public string ConnectionCode { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool Connected { get; set; }

    public int PlattformUserId { get; set; }

    [ForeignKey(nameof(PlattformUserId))]
    public virtual PlattformUser? PlattformUser { get; set; }

    public UserConnection Clone()
    {
        return new()
        {
            Connected = Connected,
            ConnectionCode = ConnectionCode,
            ValidUntil = ValidUntil,
            PlattformUserId = PlattformUserId,
            PlattformUser = PlattformUser?.Clone()
        };
    }
}
