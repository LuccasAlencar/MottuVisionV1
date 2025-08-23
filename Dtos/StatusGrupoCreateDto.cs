using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;
public record StatusGrupoCreateDto(
    [property: Required(ErrorMessage = "Nome é obrigatório.")]
    [property: StringLength(200, ErrorMessage = "Nome pode ter no máximo 200 caracteres.")]
    string Nome
);