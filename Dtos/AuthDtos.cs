namespace MottuVision.Dtos;

public record LoginRequestDto(string Usuario, string Senha);

public record LoginResponseDto(string Token, string Usuario, DateTime ExpiresAt);
