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
                new Usuario { Id = 1, NomeUsuario = "admin", SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin@123") },
                new Usuario { Id = 2, NomeUsuario = "operador", SenhaHash = BCrypt.Net.BCrypt.HashPassword("123456") },
                new Usuario { Id = 3, NomeUsuario = "supervisor", SenhaHash = BCrypt.Net.BCrypt.HashPassword("super@2024") }
            };
            await db.Usuarios.AddRangeAsync(usuarios);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 3 Usuários inseridos");

            // 2. Zonas
            var zonas = new[]
            {
                new Zona { Id = 1, Nome = "Zona Norte", Letra = "N" },
                new Zona { Id = 2, Nome = "Zona Sul", Letra = "S" },
                new Zona { Id = 3, Nome = "Zona Leste", Letra = "L" },
                new Zona { Id = 4, Nome = "Zona Oeste", Letra = "O" },
                new Zona { Id = 5, Nome = "Zona Central", Letra = "C" }
            };
            await db.Zonas.AddRangeAsync(zonas);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 5 Zonas inseridas");

            // 3. Pátios
            var patios = new[]
            {
                new Patio { Id = 1, Nome = "Pátio Principal SP" },
                new Patio { Id = 2, Nome = "Pátio Guarulhos" },
                new Patio { Id = 3, Nome = "Pátio ABC" },
                new Patio { Id = 4, Nome = "Pátio Osasco" },
                new Patio { Id = 5, Nome = "Pátio Vila Madalena" }
            };
            await db.Patios.AddRangeAsync(patios);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 5 Pátios inseridos");

            // 4. Status Grupos
            var statusGrupos = new[]
            {
                new StatusGrupo { Id = 1, Nome = "Operacional" },
                new StatusGrupo { Id = 2, Nome = "Manutenção" },
                new StatusGrupo { Id = 3, Nome = "Exceção" }
            };
            await db.StatusGrupos.AddRangeAsync(statusGrupos);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 3 Status grupos inseridos");

            // 5. Status
            var statuses = new[]
            {
                // Status Operacionais
                new Status { Id = 1, Nome = "Disponível", StatusGrupoId = 1 },
                new Status { Id = 2, Nome = "Em Uso", StatusGrupoId = 1 },
                new Status { Id = 3, Nome = "Reservado", StatusGrupoId = 1 },
                
                // Status de Manutenção
                new Status { Id = 4, Nome = "Manutenção Preventiva", StatusGrupoId = 2 },
                new Status { Id = 5, Nome = "Manutenção Corretiva", StatusGrupoId = 2 },
                new Status { Id = 6, Nome = "Aguardando Peças", StatusGrupoId = 2 },
                
                // Status de Exceção
                new Status { Id = 7, Nome = "Sinistro", StatusGrupoId = 3 },
                new Status { Id = 8, Nome = "Furtado", StatusGrupoId = 3 },
                new Status { Id = 9, Nome = "Baixado", StatusGrupoId = 3 }
            };
            await db.Statuses.AddRangeAsync(statuses);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 9 Status inseridos");

            // 6. Motos 
            var motos = new[]
            {
                // Motos Disponíveis
                new Moto
                {
                    Id = 1,
                    Placa = "ABC1D23",
                    Chassi = "9BWZZZ377VT004251",
                    QrCode = "QR001",
                    DataEntrada = DateTime.Now.AddDays(-30),
                    PrevisaoEntrega = DateTime.Now.AddDays(1),
                    ZonaId = 1,
                    PatioId = 1,
                    StatusId = 1, 
                    Observacoes = "Moto em perfeito estado - Honda CG 160"
                },
                new Moto
                {
                    Id = 2,
                    Placa = "EFG4H56",
                    Chassi = "9BWZZZ377VT004252",
                    QrCode = "QR002",
                    DataEntrada = DateTime.Now.AddDays(-25),
                    ZonaId = 2,
                    PatioId = 2,
                    StatusId = 1, 
                    Observacoes = "Yamaha Factor 125 - Revisão em dia"
                },
                new Moto
                {
                    Id = 3,
                    Placa = "IJK7L89",
                    Chassi = "9BWZZZ377VT004253",
                    QrCode = "QR003",
                    DataEntrada = DateTime.Now.AddDays(-20),
                    ZonaId = 3,
                    PatioId = 1,
                    StatusId = 2, 
                    Observacoes = "Honda CG 160 Start - Entregador: João Silva"
                },
                
                // Motos em Manutenção
                new Moto
                {
                    Id = 4,
                    Placa = "MNO0P12",
                    Chassi = "9BWZZZ377VT004254",
                    QrCode = "QR004",
                    DataEntrada = DateTime.Now.AddDays(-15),
                    PrevisaoEntrega = DateTime.Now.AddDays(3),
                    ZonaId = 1,
                    PatioId = 3,
                    StatusId = 4, 
                    Observacoes = "Troca de óleo e filtros agendada",
                    Fotos = "manutencao_001.jpg,manutencao_002.jpg"
                },
                new Moto
                {
                    Id = 5,
                    Placa = "QRS3T45",
                    Chassi = "9BWZZZ377VT004255",
                    QrCode = "QR005",
                    DataEntrada = DateTime.Now.AddDays(-10),
                    ZonaId = 4,
                    PatioId = 4,
                    StatusId = 5, 
                    Observacoes = "Problema no motor - aguardando diagnóstico"
                },
                
                // Motos Reservadas
                new Moto
                {
                    Id = 6,
                    Placa = "UVW6X78",
                    Chassi = "9BWZZZ377VT004256",
                    QrCode = "QR006",
                    DataEntrada = DateTime.Now.AddDays(-5),
                    PrevisaoEntrega = DateTime.Now.AddDays(7),
                    ZonaId = 5,
                    PatioId = 5,
                    StatusId = 3, 
                    Observacoes = "Reservado para entregador premium - Maria Santos"
                },
                
                // Casos de Exceção
                new Moto
                {
                    Id = 7,
                    Placa = "YZA9B01",
                    Chassi = "9BWZZZ377VT004257",
                    QrCode = "QR007",
                    DataEntrada = DateTime.Now.AddDays(-45),
                    ZonaId = 2,
                    PatioId = 2,
                    StatusId = 7, 
                    Observacoes = "Acidente na Av. Paulista - aguardando perícia",
                    Fotos = "sinistro_001.jpg,sinistro_002.jpg,sinistro_003.jpg"
                },
                new Moto
                {
                    Id = 8,
                    Placa = "CDE2F34",
                    Chassi = "9BWZZZ377VT004258",
                    QrCode = "QR008",
                    DataEntrada = DateTime.Now.AddDays(-60),
                    ZonaId = 3,
                    PatioId = 1,
                    StatusId = 6, 
                    Observacoes = "Yamaha Factor - aguardando kit de embreagem"
                },
                
                new Moto
                {
                    Id = 9,
                    Placa = "GHI5J67",
                    Chassi = "9BWZZZ377VT004259",
                    QrCode = "QR009",
                    DataEntrada = DateTime.Now.AddDays(-12),
                    ZonaId = 1,
                    PatioId = 3,
                    StatusId = 1,
                    Observacoes = "Honda CG 160 Titan - Km: 25.000"
                },
                new Moto
                {
                    Id = 10,
                    Placa = "KLM8N90",
                    Chassi = "9BWZZZ377VT004260",
                    QrCode = "QR010",
                    DataEntrada = DateTime.Now.AddDays(-8),
                    ZonaId = 4,
                    PatioId = 4,
                    StatusId = 2, 
                    Observacoes = "Yamaha XTZ 150 - Entregador: Carlos Oliveira"
                },
                new Moto
                {
                    Id = 11,
                    Placa = "PQR1S23",
                    Chassi = "9BWZZZ377VT004261",
                    QrCode = "QR011",
                    DataEntrada = DateTime.Now.AddDays(-18),
                    ZonaId = 5,
                    PatioId = 5,
                    StatusId = 1, 
                    Observacoes = "Honda Bros 160 - Moto nova, baixa quilometragem"
                },
                new Moto
                {
                    Id = 12,
                    Placa = "TUV4W56",
                    Chassi = "9BWZZZ377VT004262",
                    QrCode = "QR012",
                    DataEntrada = DateTime.Now.AddDays(-22),
                    ZonaId = 2,
                    PatioId = 2,
                    StatusId = 4, 
                    Observacoes = "Honda CG 160 - Revisão dos 10.000 km"
                },
                new Moto
                {
                    Id = 13,
                    Placa = "XYZ7A89",
                    Chassi = "9BWZZZ377VT004263",
                    QrCode = "QR013",
                    DataEntrada = DateTime.Now.AddDays(-35),
                    ZonaId = 3,
                    PatioId = 1,
                    StatusId = 3, 
                    Observacoes = "Yamaha Factor 125 - Reservado para expansão Zona Leste"
                },
                new Moto
                {
                    Id = 14,
                    Placa = "BCD0E12",
                    Chassi = "9BWZZZ377VT004264",
                    QrCode = "QR014",
                    DataEntrada = DateTime.Now.AddDays(-7),
                    ZonaId = 1,
                    PatioId = 3,
                    StatusId = 1, 
                    Observacoes = "Honda CG 160 Start - Excelente estado"
                },
                new Moto
                {
                    Id = 15,
                    Placa = "FGH3I45",
                    Chassi = "9BWZZZ377VT004265",
                    QrCode = "QR015",
                    DataEntrada = DateTime.Now.AddDays(-14),
                    ZonaId = 4,
                    PatioId = 4,
                    StatusId = 5,
                    Observacoes = "Yamaha XTZ 150 - Problema na transmissão"
                }
            };
            await db.Motos.AddRangeAsync(motos);
            await db.SaveChangesAsync();
            Console.WriteLine("✅ 15 Motos inseridas");

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

            Console.WriteLine("\n📊 RESUMO DOS DADOS INSERIDOS:");
            foreach (var (entity, count) in counts)
            {
                Console.WriteLine($"   • {entity}: {count} registros");
            }

            // Estatísticas das motos por status
            var motosStats = await db.Motos
                .Include(m => m.Status)
                .GroupBy(m => m.Status.Nome)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            Console.WriteLine("\n🏍️ DISTRIBUIÇÃO DAS MOTOS POR STATUS:");
            foreach (var stat in motosStats.OrderByDescending(s => s.Count))
            {
                Console.WriteLine($"   • {stat.Status}: {stat.Count} motos");
            }

            Console.WriteLine("\n✅ Sistema pronto para uso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao popular dados: {ex.Message}");
            Console.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}