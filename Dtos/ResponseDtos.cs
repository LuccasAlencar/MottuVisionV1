using System.ComponentModel.DataAnnotations;

namespace MottuVision.Dtos;

// DTOs simplificados para evitar ciclos de referência
public record ZonaResponseDto(
    decimal Id,
    string Nome,
    string Letra
);

public record PatioResponseDto(
    decimal Id,
    string Nome
);

public record StatusGrupoResponseDto(
    decimal Id,
    string Nome
);

public record StatusResponseDto(
    decimal Id,
    string Nome,
    decimal StatusGrupoId,
    StatusGrupoResponseDto? StatusGrupo = null
);

public record MotoResponseDto(
    decimal Id,
    string Placa,
    string Chassi,
    string? QrCode,
    DateTime DataEntrada,
    DateTime? PrevisaoEntrega,
    string? Fotos,
    decimal ZonaId,
    decimal PatioId,
    decimal StatusId,
    string? Observacoes,
    ZonaResponseDto? Zona = null,
    PatioResponseDto? Patio = null,
    StatusResponseDto? Status = null
);

public record UsuarioResponseDto(
    decimal Id,
    string Usuario,
    string SenhaHash
);

// Para Status Grupo com seus Status (sem ciclo)
public record StatusGrupoWithStatusDto(
    decimal Id,
    string Nome,
    IEnumerable<StatusSimpleDto> Statuses
);

public record StatusSimpleDto(
    decimal Id,
    string Nome,
    decimal StatusGrupoId
);