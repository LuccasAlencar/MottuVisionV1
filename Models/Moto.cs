using System.Collections.Generic;

namespace MottuVision.Models;
public class Moto
{
    public decimal Id { get; set; }
    public string Placa { get; set; } = default!;
    public string Chassi { get; set; } = default!;
    public string? QrCode { get; set; }
    public DateTime DataEntrada { get; set; }
    public DateTime? PrevisaoEntrega { get; set; }
    public string? Fotos { get; set; }
    public decimal ZonaId { get; set; }
    public decimal PatioId { get; set; }
    public decimal StatusId { get; set; }
    public string? Observacoes { get; set; }
    
    // Propriedades de navegação
    public Zona Zona { get; set; } = default!;
    public Patio Patio { get; set; } = default!;
    public Status Status { get; set; } = default!;
}