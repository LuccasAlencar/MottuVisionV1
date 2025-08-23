using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;

public record ZonaUpdateDto(
    [property: Required, StringLength(50)] string Nome,
    [property: Required, StringLength(1, MinimumLength = 1)] string Letra
);