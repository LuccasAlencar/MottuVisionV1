using System.Collections.Generic;

namespace MottuVision.Models;
public class Status
{
    public decimal Id { get; set; }
    public string Nome { get; set; } = default!;
    public decimal StatusGrupoId { get; set; }
    public StatusGrupo StatusGrupo { get; set; } = default!;
    public ICollection<Moto> Motos { get; set; } = new List<Moto>();
}