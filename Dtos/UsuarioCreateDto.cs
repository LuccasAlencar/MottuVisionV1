using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;

public record UsuarioCreateDto(
    [property: Required, StringLength(50, MinimumLength = 3)] string Usuario,
    [property: Required, StringLength(255, MinimumLength = 4)] string Senha
);