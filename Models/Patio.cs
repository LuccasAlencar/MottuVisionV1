using System.Collections.Generic;

namespace MottuVision.Models;
public class Patio
{
    public decimal Id { get; set; }
    public string Nome { get; set; } = default!;
    public ICollection<Moto> Motos { get; set; } = new List<Moto>();
}