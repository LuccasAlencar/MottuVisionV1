using System.Collections.Generic;

namespace MottuVision.Models;
public class StatusGrupo
{
    public decimal Id { get; set; }
    public string Nome { get; set; } = default!;
    public ICollection<Status> Statuses { get; set; } = new List<Status>();
}