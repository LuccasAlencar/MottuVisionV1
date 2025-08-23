using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;

public record MotoCreateDto(
    [property: Required, StringLength(10)] string Placa,
    [property: Required, StringLength(20)] string Chassi,
    string? QrCode,
    [property: Required] DateTime DataEntrada,
    DateTime? PrevisaoEntrega,
    string? Fotos,
    [property: Required] decimal ZonaId,
    [property: Required] decimal PatioId,
    [property: Required] decimal StatusId,
    string? Observacoes
);