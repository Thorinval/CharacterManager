using System.ComponentModel.DataAnnotations;
using CharacterManager.Server.Models.Enums;

namespace CharacterManager.Server.Models;

public class TeamPowerTimelineRecord
{
    [Key]
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TeamPowerTimelineType Type { get; set; }
    public int TotalPower { get; set; }
    public DateTime DateInsertion { get; set; } = DateTime.Now;
}
