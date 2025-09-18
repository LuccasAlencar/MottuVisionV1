using Microsoft.EntityFrameworkCore;
using MottuVision.Data;
using MottuVision.Models;

namespace MottuVision.Api;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            // Garantir que o banco existe
            await db.Database.EnsureCreatedAsync();
            
            Console.WriteLine("🔄 Verificando se precisa popular dados...");

            // Se já tem dados, não popula novamente
            if (await db.Usuarios.AnyAsync())
            {
                Console.WriteLine("✅ Dados já existem no banco");
                return;
            }

            Console.WriteLine("🔄 Populando dados iniciais...");

            // 1. Usuários
            var usuarios = new[]
            {
                new Usuario { Id = 1, NomeUsuario = "admin", SenhaHash = "admin@123" },
                new Usuario { Id = 2, NomeUsuario = "operador", SenhaHash = "123456" }
            };
            await db.Usuarios.AddRangeAsync(usuarios);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Usuários inseridos");

            // 2. Zonas
            var zonas = new[]
            {
                new Zona { Id = 1, Nome = "Norte", Letra = "N" },
                new Zona { Id = 2, Nome = "Sul", Letra = "S" }
            };
            await db.Zonas.AddRangeAsync(zonas);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Zonas inseridas");

            // 3. Pátios
            var patios = new[]
            {
                new Patio { Id = 1, Nome = "Pátio A" },
                new Patio { Id = 2, Nome = "Pátio B" }
            };
            await db.Patios.AddRangeAsync(patios);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Pátios inseridos");

            // 4. Status Grupos
            var statusGrupos = new[]
            {
                new StatusGrupo { Id = 1, Nome = "Operacional" },
                new StatusGrupo { Id = 2, Nome = "Exceção" }
            };
            await db.StatusGrupos.AddRangeAsync(statusGrupos);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Status grupos inseridos");

            // 5. Status
            var statuses = new[]
            {
                new Status { Id = 1, Nome = "OK", StatusGrupoId = 1 },
                new Status { Id = 2, Nome = "Manutenção", StatusGrupoId = 1 },
                new Status { Id = 3, Nome = "Sinistro", StatusGrupoId = 2 }
            };
            await db.Statuses.AddRangeAsync(statuses);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Status inseridos");

            // 6. Motos
            var motos = new[]
            {
                new Moto
                {
                    Id = 1,
                    Placa = "ABC1D23",
                    Chassi = "9BWZZZ377VT004251",
                    QrCode = "QR001",
                    DataEntrada = DateTime.Now,
                    PrevisaoEntrega = DateTime.Now.AddDays(1),
                    ZonaId = 1,
                    PatioId = 1,
                    StatusId = 1,
                    Observacoes = "Moto em perfeito estado"
                },
                new Moto
                {
                    Id = 2,
                    Placa = "EFG4H56",
                    Chassi = "9BWZZZ377VT004252",
                    QrCode = "QR002",
                    DataEntrada = DateTime.Now,
                    ZonaId = 2,
                    PatioId = 2,
                    StatusId = 2,
                    Observacoes = "Em manutenção preventiva"
                }
            };
            await db.Motos.AddRangeAsync(motos);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ Motos inseridas");

            Console.WriteLine("🎉 Dados populados com sucesso via Entity Framework!");
            
            // Verificar contagens
            var counts = new Dictionary<string, int>
            {
                ["Usuários"] = await db.Usuarios.CountAsync(),
                ["Zonas"] = await db.Zonas.CountAsync(),
                ["Pátios"] = await db.Patios.CountAsync(),
                ["Status Grupos"] = await db.StatusGrupos.CountAsync(),
                ["Status"] = await db.Statuses.CountAsync(),
                ["Motos"] = await db.Motos.CountAsync()
            };

            foreach (var (entity, count) in counts)
            {
                Console.WriteLine($"📊 {entity}: {count} registros");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao popular dados: {ex.Message}");
            Console.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}