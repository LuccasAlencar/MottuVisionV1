namespace MottuVision.Models;
public class Usuario
{
    public decimal Id { get; set; }
    public string NomeUsuario { get; set; } = default!; 
    public string SenhaHash { get; set; } = default!;  
}