using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;
public record StatusCreateDto(
    [property: Required(ErrorMessage = "Nome é obrigatório.")]
    [property: StringLength(200, ErrorMessage = "Nome pode ter no máximo 200 caracteres.")]
    string Nome,

    [property: Required(ErrorMessage = "StatusGrupoId é obrigatório.")]
    decimal StatusGrupoId
);