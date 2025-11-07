using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using MottuVision.Data;
using MottuVision.Models;
using MottuVision.Dtos;
using MottuVision.Services;
using System.Data;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.MaxDepth = 32;
    });

// DB Oracle
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("OracleConnection")
             ?? throw new InvalidOperationException("ConnectionStrings:OracleConnection não configurada.");
    opt.UseOracle(cs);
});

// Serviços personalizados
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<MotoPrevisaoService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key não configurada");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mottu Vision API",
        Version = "v1",
        Description = @"
# API RESTful para Gerenciamento de Pátio de Motocicletas

## Sobre
API desenvolvida em .NET 8 Minimal API seguindo as melhores práticas REST e padrões HTTP.

## Características
- REST compliant - Verbos HTTP corretos (GET, POST, PUT, DELETE)
- Status Codes adequados - 200, 201, 204, 400, 404, 409, 500
- HATEOAS - Links de navegação em respostas paginadas
- Paginação - Suporte a paginação em todas as listagens
- Validações - Validação de dados e integridade referencial
- Content Negotiation - application/json
- JWT Authentication - Autenticação via Bearer token
- Health Checks - Monitoramento de saúde da API
- ML.NET - Previsões de Machine Learning

## Status Codes Utilizados
| Code | Significado | Uso |
|------|-------------|-----|
| 200 | OK | Sucesso em GET e PUT |
| 201 | Created | Recurso criado com sucesso (POST) |
| 204 | No Content | Sucesso em DELETE (sem corpo de resposta) |
| 400 | Bad Request | Dados inválidos ou violação de regras |
| 401 | Unauthorized | Não autenticado |
| 404 | Not Found | Recurso não encontrado |
| 409 | Conflict | Conflito (ex: duplicação de dados únicos) |
| 500 | Internal Server Error | Erro interno do servidor |",
        Contact = new OpenApiContact
        {
            Name = "Equipe MottuVision",
            Email = "contato@mottuvision.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
    
    // Configuração JWT no Swagger
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    opt.EnableAnnotations();
});

var app = builder.Build();

// Seeder/migração
using (var scope = app.Services.CreateScope())
{
    try
    {
        await MottuVision.Api.DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Erro ao popular dados iniciais");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mottu Vision API v1");
    c.RoutePrefix = "swagger";
});

// Middleware de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
}).WithTags("Health Checks")
  .WithOpenApi(op =>
  {
      op.Summary = "Health Check da API";
      op.Description = @"Verifica o status de saúde da API e suas dependências.
      
**Status possíveis:**
- `Healthy`: Todos os checks passaram
- `Degraded`: Alguns checks falharam mas o sistema ainda funciona
- `Unhealthy`: Checks críticos falharam";
      return op;
  });

app.MapGet("/", () => Results.Redirect("/swagger"));

// ---------- helpers ----------
static Link MakeLink(HttpContext ctx, string rel, string path, string method) =>
    new(rel, $"{ctx.Request.Scheme}://{ctx.Request.Host}{path}", method);

static PagedResult<T> ToPaged<T>(
    HttpContext ctx, IEnumerable<T> items, int page, int pageSize, long total, string basePath)
{
    var links = new List<Link> { MakeLink(ctx, "self", $"{basePath}?page={page}&pageSize={pageSize}", "GET") };
    var totalPages = (int)Math.Ceiling(total / (double)pageSize);
    if (page > 1) links.Add(MakeLink(ctx, "prev", $"{basePath}?page={page - 1}&pageSize={pageSize}", "GET"));
    if (page < totalPages) links.Add(MakeLink(ctx, "next", $"{basePath}?page={page + 1}&pageSize={pageSize}", "GET"));

    return new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = total, Links = links };
}

static async Task<decimal> NextIdAsync(AppDbContext db, string table)
{
    var connection = db.Database.GetDbConnection();

    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var cmd = connection.CreateCommand();
    cmd.CommandText = $"SELECT NVL(MAX(\"id\"), 0) + 1 FROM \"{table}\"";
    
    var result = await cmd.ExecuteScalarAsync();
    return result == DBNull.Value ? 1 : Convert.ToDecimal(result);
}

// ================== AUTENTICAÇÃO (JWT) ==================
var auth = app.MapGroup("/api/auth").WithTags("Autenticação");

auth.MapPost("/login", async (AppDbContext db, JwtService jwtService, LoginRequestDto dto) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.NomeUsuario == dto.Usuario);
    
    if (usuario == null)
        return Results.Unauthorized();
    
    // Validar senha usando BCrypt
    if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
        return Results.Unauthorized();
    
    var token = jwtService.GenerateToken(usuario.NomeUsuario, usuario.Id.ToString());
    var response = new LoginResponseDto(token, usuario.NomeUsuario, DateTime.UtcNow.AddHours(2));
    
    return Results.Ok(response);
})
.WithName("Login")
.WithOpenApi(op =>
{
    op.Summary = "Login com JWT";
    op.Description = @"Autentica um usuário e retorna um token JWT.

**Como usar:**
1. Faça login com este endpoint
2. Copie o token retornado
3. Clique em 'Authorize' no Swagger
4. Cole o token (sem 'Bearer ')
5. Use as APIs protegidas

**Status Codes:**
- `200 OK`: Login bem-sucedido, retorna token JWT
- `401 Unauthorized`: Credenciais inválidas";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiObject
                {
                    ["usuario"] = new OpenApiString("admin"),
                    ["senha"] = new OpenApiString("admin@123")
                }
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse { Description = "Login bem-sucedido" };
    op.Responses["401"] = new OpenApiResponse { Description = "Credenciais inválidas" };
    return op;
})
.AllowAnonymous();

// ================== ML.NET - PREVISÃO ==================
var mlnet = app.MapGroup("/api/ml").WithTags("Machine Learning");

mlnet.MapPost("/previsao-permanencia", (MotoPrevisaoService mlService, PrevisaoRequestDto dto) =>
{
    try
    {
        var tempoHoras = mlService.PreverTempoPermanencia(
            dto.ZonaId,
            dto.PatioId,
            dto.StatusId,
            dto.DiaSemana
        );

        var tempoDias = (int)Math.Ceiling(tempoHoras / 24.0);
        
        var mensagem = tempoDias switch
        {
            <= 1 => "Moto deve sair em até 1 dia",
            <= 3 => "Moto deve permanecer de 2 a 3 dias",
            <= 7 => "Moto deve permanecer de 4 a 7 dias",
            _ => "Moto deve permanecer mais de 7 dias"
        };

        var response = new PrevisaoResponseDto(tempoHoras, tempoDias, mensagem, dto);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro na previsão: {ex.Message}");
    }
})
.WithName("PrevisaoPermanenciaMoto")
.WithOpenApi(op =>
{
    op.Summary = "Prevê tempo de permanência de moto no pátio (ML.NET)";
    op.Description = @"Usa Machine Learning (ML.NET) para prever quanto tempo uma moto ficará no pátio.

**Modelo:** FastTree Regression
**Entrada:** ZonaId, PatioId, StatusId, DiaSemana (0-6)
**Saída:** Tempo previsto em horas e dias

**Exemplo de uso:**
- ZonaId=1, PatioId=1, StatusId=1, DiaSemana=1 → ~24-30 horas
- ZonaId=2, PatioId=2, StatusId=2, DiaSemana=5 → ~96 horas

**Status Codes:**
- `200 OK`: Previsão realizada com sucesso
- `400 Bad Request`: Dados inválidos
- `500 Internal Server Error`: Erro no modelo ML";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiObject
                {
                    ["zonaId"] = new OpenApiInteger(1),
                    ["patioId"] = new OpenApiInteger(1),
                    ["statusId"] = new OpenApiInteger(1),
                    ["diaSemana"] = new OpenApiInteger(1)
                }
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse { Description = "Previsão realizada com sucesso" };
    op.Responses["500"] = new OpenApiResponse { Description = "Erro no modelo ML" };
    return op;
})
.AllowAnonymous(); // ou .RequireAuthorization() para endpoints protegidos

// ================== USUÁRIOS ==================
var usuarios = app.MapGroup("/api/usuarios").WithTags("Usuários");

usuarios.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await db.Usuarios.LongCountAsync();
    var usuarios = await db.Usuarios
        .OrderBy(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = usuarios.Select(u => new UsuarioResponseDto(u.Id, u.NomeUsuario, u.SenhaHash));
    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/usuarios"));
})
.WithName("GetUsuarios")
.WithOpenApi(op =>
{
    op.Summary = "Lista usuários paginado";
    op.Description = @"Retorna lista paginada de usuários com informações básicas.

**Status Codes:**
- `200 OK`: Lista retornada com sucesso (mesmo que vazia)
- `400 Bad Request`: Parâmetros de paginação inválidos";
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "page",
        In = ParameterLocation.Query,
        Description = "Número da página (mínimo 1)",
        Required = false,
        Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "pageSize",
        In = ParameterLocation.Query,
        Description = "Tamanho da página (1-100)",
        Required = false,
        Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(20) }
    });
    op.Responses["200"].Description = "Lista de usuários retornada com sucesso";
    return op;
});

usuarios.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound(new { message = "Usuário não encontrado" });
    return TypedResults.Ok(new UsuarioResponseDto(u.Id, u.NomeUsuario, u.SenhaHash));
})
.WithName("GetUsuarioById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém usuário por ID";
    op.Description = @"Retorna um usuário específico pelo ID.

**Status Codes:**
- `200 OK`: Usuário encontrado e retornado
- `404 Not Found`: Usuário não existe com o ID fornecido";
    op.Responses["200"].Description = "Usuário encontrado com sucesso";
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

usuarios.MapPost("/", async Task<IResult> (AppDbContext db, UsuarioCreateDto dto) =>
{
    if (await db.Usuarios.AnyAsync(x => x.NomeUsuario == dto.Usuario))
        return TypedResults.BadRequest(new { message = "Usuário já existe." });

    var entity = new Usuario
    {
        Id = await NextIdAsync(db, "usuario"),
        NomeUsuario = dto.Usuario,
        SenhaHash = dto.Senha // Em produção, hash a senha adequadamente
    };
    
    db.Usuarios.Add(entity);
    await db.SaveChangesAsync();
    
    var result = new UsuarioResponseDto(entity.Id, entity.NomeUsuario, entity.SenhaHash);
    return TypedResults.Created($"/api/usuarios/{entity.Id}", result);
})
.WithName("CreateUsuario")
.WithOpenApi(op =>
{
    op.Summary = "Cria usuário";
    op.Description = @"Cria um novo usuário no sistema.

**Status Codes:**
- `201 Created`: Usuário criado com sucesso. Retorna Location header com URI do recurso
- `400 Bad Request`: Dados inválidos ou usuário já existe
- `409 Conflict`: Conflito - nome de usuário já está em uso";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(UsuarioCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["usuario"] = new OpenApiString("admin"),
                    ["senha"] = new OpenApiString("admin@123")
                }
            }
        }
    };
    op.Responses["201"] = new OpenApiResponse { Description = "Usuário criado com sucesso" };
    op.Responses["400"] = new OpenApiResponse { Description = "Dados inválidos ou usuário já existe" };
    return op;
});

usuarios.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, UsuarioUpdateDto dto) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound(new { message = "Usuário não encontrado" });

    if (await db.Usuarios.AnyAsync(x => x.NomeUsuario == dto.Usuario && x.Id != id))
        return TypedResults.BadRequest(new { message = "Já existe outro usuário com esse nome." });

    u.NomeUsuario = dto.Usuario;
    u.SenhaHash = dto.Senha; 
    await db.SaveChangesAsync();
    
    return TypedResults.Ok(new UsuarioResponseDto(u.Id, u.NomeUsuario, u.SenhaHash));
})
.WithName("UpdateUsuario")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza usuário";
    op.Description = @"Atualiza um usuário existente.

**Status Codes:**
- `200 OK`: Usuário atualizado com sucesso
- `400 Bad Request`: Dados inválidos ou nome de usuário já está em uso por outro usuário
- `404 Not Found`: Usuário não encontrado";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(UsuarioUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["usuario"] = new OpenApiString("admin_updated"),
                    ["senha"] = new OpenApiString("nova_senha@123")
                }
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse { Description = "Usuário atualizado com sucesso" };
    op.Responses["400"] = new OpenApiResponse { Description = "Dados inválidos" };
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

usuarios.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound(new { message = "Usuário não encontrado" });
    
    db.Usuarios.Remove(u);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeleteUsuario")
.WithOpenApi(op =>
{
    op.Summary = "Remove usuário";
    op.Description = @"Remove um usuário do sistema.

**Status Codes:**
- `204 No Content`: Usuário removido com sucesso (sem corpo de resposta)
- `404 Not Found`: Usuário não encontrado";
    op.Responses["204"] = new OpenApiResponse { Description = "Usuário removido com sucesso" };
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

// ================== ZONAS ==================
var zonas = app.MapGroup("/api/zonas").WithTags("Zonas");

zonas.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await db.Zonas.LongCountAsync();
    var zonas = await db.Zonas
        .OrderBy(z => z.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = zonas.Select(z => new ZonaResponseDto(z.Id, z.Nome, z.Letra));
    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/zonas"));
})
.WithName("GetZonas")
.WithOpenApi(op =>
{
    op.Summary = "Lista zonas paginado";
    op.Description = "Retorna lista paginada de zonas";
    return op;
});

zonas.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var z = await db.Zonas.FindAsync(id);
    if (z is null) return TypedResults.NotFound(new { message = "Zona não encontrada" });
    return TypedResults.Ok(new ZonaResponseDto(z.Id, z.Nome, z.Letra));
})
.WithName("GetZonaById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém zona por ID";
    op.Description = "Retorna uma zona específica pelo ID";
    return op;
});

zonas.MapPost("/", async Task<IResult> (AppDbContext db, ZonaCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Letra) || dto.Letra.Length != 1)
        return TypedResults.BadRequest(new { message = "Nome obrigatório e Letra deve ter exatamente 1 caractere." });

    var entity = new Zona 
    { 
        Id = await NextIdAsync(db, "zona"), 
        Nome = dto.Nome.Trim(), 
        Letra = dto.Letra.Trim().ToUpper()
    };
    
    db.Zonas.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/zonas/{entity.Id}", new ZonaResponseDto(entity.Id, entity.Nome, entity.Letra));
})
.WithName("CreateZona")
.WithOpenApi(op =>
{
    op.Summary = "Cria zona";
    op.Description = "Cria uma nova zona no sistema";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(ZonaCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Zona Norte"),
                    ["letra"] = new OpenApiString("N")
                }
            }
        }
    };
    return op;
});

zonas.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, ZonaUpdateDto dto) =>
{
    var z = await db.Zonas.FindAsync(id);
    if (z is null) return TypedResults.NotFound(new { message = "Zona não encontrada" });
    
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Letra) || dto.Letra.Length != 1)
        return TypedResults.BadRequest(new { message = "Nome obrigatório e Letra deve ter exatamente 1 caractere." });

    z.Nome = dto.Nome.Trim();
    z.Letra = dto.Letra.Trim().ToUpper();
    await db.SaveChangesAsync();
    return TypedResults.Ok(new ZonaResponseDto(z.Id, z.Nome, z.Letra));
})
.WithName("UpdateZona")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza zona";
    op.Description = "Atualiza uma zona existente";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(ZonaUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Zona Sul"),
                    ["letra"] = new OpenApiString("S")
                }
            }
        }
    };
    return op;
});

zonas.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var z = await db.Zonas.Include(x => x.Motos).FirstOrDefaultAsync(x => x.Id == id);
    if (z is null) return TypedResults.NotFound(new { message = "Zona não encontrada" });
    
    if (z.Motos.Any())
        return TypedResults.BadRequest(new { message = "Não é possível remover zona com motos associadas." });
    
    db.Zonas.Remove(z);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeleteZona")
.WithOpenApi(op =>
{
    op.Summary = "Remove zona";
    op.Description = "Remove uma zona do sistema (apenas se não tiver motos associadas)";
    return op;
});

// ================== PÁTIOS ==================
var patios = app.MapGroup("/api/patios").WithTags("Pátios");

patios.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await db.Patios.LongCountAsync();
    var patios = await db.Patios
        .OrderBy(p => p.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = patios.Select(p => new PatioResponseDto(p.Id, p.Nome));
    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/patios"));
})
.WithName("GetPatios")
.WithOpenApi(op =>
{
    op.Summary = "Lista pátios paginado";
    op.Description = "Retorna lista paginada de pátios";
    return op;
});

patios.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var p = await db.Patios.FindAsync(id);
    if (p is null) return TypedResults.NotFound(new { message = "Pátio não encontrado" });
    return TypedResults.Ok(new PatioResponseDto(p.Id, p.Nome));
})
.WithName("GetPatioById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém pátio por ID";
    op.Description = "Retorna um pátio específico pelo ID";
    return op;
});

patios.MapPost("/", async Task<IResult> (AppDbContext db, PatioCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });

    var entity = new Patio 
    { 
        Id = await NextIdAsync(db, "patio"), 
        Nome = dto.Nome.Trim()
    };
    
    db.Patios.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/patios/{entity.Id}", new PatioResponseDto(entity.Id, entity.Nome));
})
.WithName("CreatePatio")
.WithOpenApi(op =>
{
    op.Summary = "Cria pátio";
    op.Description = "Cria um novo pátio no sistema";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(PatioCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Pátio Central SP")
                }
            }
        }
    };
    return op;
});

patios.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, PatioUpdateDto dto) =>
{
    var p = await db.Patios.FindAsync(id);
    if (p is null) return TypedResults.NotFound(new { message = "Pátio não encontrado" });
    
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });

    p.Nome = dto.Nome.Trim();
    await db.SaveChangesAsync();
    return TypedResults.Ok(new PatioResponseDto(p.Id, p.Nome));
})
.WithName("UpdatePatio")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza pátio";
    op.Description = "Atualiza um pátio existente";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(PatioUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Pátio Guarulhos Atualizado")
                }
            }
        }
    };
    return op;
});

patios.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var p = await db.Patios.Include(x => x.Motos).FirstOrDefaultAsync(x => x.Id == id);
    if (p is null) return TypedResults.NotFound(new { message = "Pátio não encontrado" });
    
    if (p.Motos.Any()) 
        return TypedResults.BadRequest(new { message = "Não é possível remover pátio com motos associadas." });
    
    db.Patios.Remove(p);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeletePatio")
.WithOpenApi(op =>
{
    op.Summary = "Remove pátio";
    op.Description = "Remove um pátio do sistema (apenas se não tiver motos associadas)";
    return op;
});

// ================== STATUS GRUPOS ==================
var statusGrupos = app.MapGroup("/api/statusgrupos").WithTags("Status Grupos");

statusGrupos.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await db.StatusGrupos.LongCountAsync();
    var statusGrupos = await db.StatusGrupos
        .OrderBy(sg => sg.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = statusGrupos.Select(sg => new StatusGrupoResponseDto(sg.Id, sg.Nome));
    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/statusgrupos"));
})
.WithName("GetStatusGrupos")
.WithOpenApi(op =>
{
    op.Summary = "Lista status grupos paginado";
    op.Description = "Retorna lista paginada de grupos de status";
    return op;
});

statusGrupos.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var sg = await db.StatusGrupos
        .Include(x => x.Statuses)
        .FirstOrDefaultAsync(x => x.Id == id);
    
    if (sg is null) return TypedResults.NotFound(new { message = "Status grupo não encontrado" });
    
    var result = new StatusGrupoWithStatusDto(
        sg.Id, 
        sg.Nome,
        sg.Statuses.Select(s => new StatusSimpleDto(s.Id, s.Nome, s.StatusGrupoId))
    );
    
    return TypedResults.Ok(result);
})
.WithName("GetStatusGrupoById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém status grupo por ID";
    op.Description = "Retorna um status grupo específico pelo ID com seus status relacionados";
    return op;
});

statusGrupos.MapPost("/", async Task<IResult> (AppDbContext db, StatusGrupoCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });

    var entity = new StatusGrupo 
    { 
        Id = await NextIdAsync(db, "status_grupo"), 
        Nome = dto.Nome.Trim()
    };
    
    db.StatusGrupos.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/statusgrupos/{entity.Id}", new StatusGrupoResponseDto(entity.Id, entity.Nome));
})
.WithName("CreateStatusGrupo")
.WithOpenApi(op =>
{
    op.Summary = "Cria status grupo";
    op.Description = "Cria um novo grupo de status no sistema";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(StatusGrupoCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Operacional")
                }
            }
        }
    };
    return op;
});

statusGrupos.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, StatusGrupoUpdateDto dto) =>
{
    var sg = await db.StatusGrupos.FindAsync(id);
    if (sg is null) return TypedResults.NotFound(new { message = "Status grupo não encontrado" });
    
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });

    sg.Nome = dto.Nome.Trim();
    await db.SaveChangesAsync();
    return TypedResults.Ok(new StatusGrupoResponseDto(sg.Id, sg.Nome));
})
.WithName("UpdateStatusGrupo")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza status grupo";
    op.Description = "Atualiza um grupo de status existente";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(StatusGrupoUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Manutenção")
                }
            }
        }
    };
    return op;
});

statusGrupos.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var sg = await db.StatusGrupos.Include(x => x.Statuses).FirstOrDefaultAsync(x => x.Id == id);
    if (sg is null) return TypedResults.NotFound(new { message = "Status grupo não encontrado" });
    
    if (sg.Statuses.Any()) 
        return TypedResults.BadRequest(new { message = "Não é possível remover status grupo que contém status." });
    
    db.StatusGrupos.Remove(sg);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeleteStatusGrupo")
.WithOpenApi(op =>
{
    op.Summary = "Remove status grupo";
    op.Description = "Remove um status grupo do sistema (apenas se não tiver status associados)";
    return op;
});

// ================== STATUS ==================
var statuses = app.MapGroup("/api/statuses").WithTags("Status");

statuses.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await db.Statuses.LongCountAsync();
    var statuses = await db.Statuses
        .Include(s => s.StatusGrupo)
        .OrderBy(s => s.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = statuses.Select(s => new StatusResponseDto(
        s.Id, 
        s.Nome, 
        s.StatusGrupoId,
        new StatusGrupoResponseDto(s.StatusGrupo.Id, s.StatusGrupo.Nome)
    ));

    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/statuses"));
})
.WithName("GetStatuses")
.WithOpenApi(op =>
{
    op.Summary = "Lista status paginado";
    op.Description = "Retorna lista paginada de status com seus grupos";
    return op;
});

statuses.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var s = await db.Statuses
        .Include(x => x.StatusGrupo)
        .FirstOrDefaultAsync(x => x.Id == id);
    
    if (s is null) return TypedResults.NotFound(new { message = "Status não encontrado" });
    
    var result = new StatusResponseDto(
        s.Id, 
        s.Nome, 
        s.StatusGrupoId,
        new StatusGrupoResponseDto(s.StatusGrupo.Id, s.StatusGrupo.Nome)
    );
    
    return TypedResults.Ok(result);
})
.WithName("GetStatusById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém status por ID";
    op.Description = "Retorna um status específico pelo ID com seu grupo relacionado";
    return op;
});

statuses.MapPost("/", async Task<IResult> (AppDbContext db, StatusCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });
    
    if (!await db.StatusGrupos.AnyAsync(x => x.Id == dto.StatusGrupoId)) 
        return TypedResults.BadRequest(new { message = "StatusGrupoId inválido." });

    var entity = new Status
    {
        Id = await NextIdAsync(db, "status"),
        Nome = dto.Nome.Trim(),
        StatusGrupoId = dto.StatusGrupoId
    };
    
    db.Statuses.Add(entity);
    await db.SaveChangesAsync();
    
    var statusGrupo = await db.StatusGrupos.FindAsync(dto.StatusGrupoId);
    var result = new StatusResponseDto(
        entity.Id, 
        entity.Nome, 
        entity.StatusGrupoId,
        new StatusGrupoResponseDto(statusGrupo!.Id, statusGrupo.Nome)
    );
    
    return TypedResults.Created($"/api/statuses/{entity.Id}", result);
})
.WithName("CreateStatus")
.WithOpenApi(op =>
{
    op.Summary = "Cria status";
    op.Description = "Cria um novo status no sistema";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(StatusCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Disponível"),
                    ["statusGrupoId"] = new OpenApiInteger(1)
                }
            }
        }
    };
    return op;
});

statuses.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, StatusUpdateDto dto) =>
{
    var s = await db.Statuses.FindAsync(id);
    if (s is null) return TypedResults.NotFound(new { message = "Status não encontrado" });
    
    if (string.IsNullOrWhiteSpace(dto.Nome)) 
        return TypedResults.BadRequest(new { message = "Nome é obrigatório." });
    
    if (!await db.StatusGrupos.AnyAsync(x => x.Id == dto.StatusGrupoId)) 
        return TypedResults.BadRequest(new { message = "StatusGrupoId inválido." });

    s.Nome = dto.Nome.Trim();
    s.StatusGrupoId = dto.StatusGrupoId;
    await db.SaveChangesAsync();
    
    var statusGrupo = await db.StatusGrupos.FindAsync(dto.StatusGrupoId);
    var result = new StatusResponseDto(
        s.Id, 
        s.Nome, 
        s.StatusGrupoId,
        new StatusGrupoResponseDto(statusGrupo!.Id, statusGrupo.Nome)
    );
    
    return TypedResults.Ok(result);
})
.WithName("UpdateStatus")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza status";
    op.Description = "Atualiza um status existente";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(StatusUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["nome"] = new OpenApiString("Em Manutenção"),
                    ["statusGrupoId"] = new OpenApiInteger(2)
                }
            }
        }
    };
    return op;
});

statuses.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var s = await db.Statuses.Include(x => x.Motos).FirstOrDefaultAsync(x => x.Id == id);
    if (s is null) return TypedResults.NotFound(new { message = "Status não encontrado" });
    
    if (s.Motos.Any()) 
        return TypedResults.BadRequest(new { message = "Não é possível remover status com motos associadas." });
    
    db.Statuses.Remove(s);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeleteStatus")
.WithOpenApi(op =>
{
    op.Summary = "Remove status";
    op.Description = "Remove um status do sistema (apenas se não tiver motos associadas)";
    return op;
});

// ================== MOTOS ==================
var motos = app.MapGroup("/api/motos").WithTags("Motos");

motos.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20, string? placa = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = db.Motos.AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(placa))
        query = query.Where(m => m.Placa.ToLower().Contains(placa.ToLower()));

    var total = await query.LongCountAsync();
    var motos = await query
        .Include(m => m.Zona)
        .Include(m => m.Patio)
        .Include(m => m.Status)
            .ThenInclude(s => s.StatusGrupo)
        .OrderBy(m => m.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var result = motos.Select(m => new MotoResponseDto(
        m.Id,
        m.Placa,
        m.Chassi,
        m.QrCode,
        m.DataEntrada,
        m.PrevisaoEntrega,
        m.Fotos,
        m.ZonaId,
        m.PatioId,
        m.StatusId,
        m.Observacoes,
        new ZonaResponseDto(m.Zona.Id, m.Zona.Nome, m.Zona.Letra),
        new PatioResponseDto(m.Patio.Id, m.Patio.Nome),
        new StatusResponseDto(
            m.Status.Id, 
            m.Status.Nome, 
            m.Status.StatusGrupoId,
            new StatusGrupoResponseDto(m.Status.StatusGrupo.Id, m.Status.StatusGrupo.Nome)
        )
    ));

    return TypedResults.Ok(ToPaged(ctx, result, page, pageSize, total, "/api/motos"));
})
.WithName("GetMotos")
.WithOpenApi(op =>
{
    op.Summary = "Lista motos paginado com filtro";
    op.Description = @"Retorna lista paginada de motos com opção de filtro por placa.

**Status Codes:**
- `200 OK`: Lista retornada com sucesso (mesmo que vazia)
- `400 Bad Request`: Parâmetros inválidos";
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "placa",
        In = ParameterLocation.Query,
        Description = "Filtro por placa (busca parcial)",
        Required = false,
        Schema = new OpenApiSchema { Type = "string" }
    });
    op.Responses["200"].Description = "Lista de motos retornada com sucesso";
    return op;
});

motos.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var m = await db.Motos
        .Include(x => x.Zona)
        .Include(x => x.Patio)
        .Include(x => x.Status)
            .ThenInclude(s => s.StatusGrupo)
        .FirstOrDefaultAsync(x => x.Id == id);
    
    if (m is null) return TypedResults.NotFound(new { message = "Moto não encontrada" });
    
    var result = new MotoResponseDto(
        m.Id,
        m.Placa,
        m.Chassi,
        m.QrCode,
        m.DataEntrada,
        m.PrevisaoEntrega,
        m.Fotos,
        m.ZonaId,
        m.PatioId,
        m.StatusId,
        m.Observacoes,
        new ZonaResponseDto(m.Zona.Id, m.Zona.Nome, m.Zona.Letra),
        new PatioResponseDto(m.Patio.Id, m.Patio.Nome),
        new StatusResponseDto(
            m.Status.Id, 
            m.Status.Nome, 
            m.Status.StatusGrupoId,
            new StatusGrupoResponseDto(m.Status.StatusGrupo.Id, m.Status.StatusGrupo.Nome)
        )
    );
    
    return TypedResults.Ok(result);
})
.WithName("GetMotoById")
.WithOpenApi(op =>
{
    op.Summary = "Obtém moto por ID";
    op.Description = @"Retorna uma moto específica pelo ID com todos os relacionamentos (Zona, Pátio, Status).

**Status Codes:**
- `200 OK`: Moto encontrada e retornada
- `404 Not Found`: Moto não existe com o ID fornecido";
    op.Responses["200"].Description = "Moto encontrada com sucesso";
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

motos.MapPost("/", async Task<IResult> (AppDbContext db, MotoCreateDto dto) =>
{
   
    if (!await db.Zonas.AnyAsync(z => z.Id == dto.ZonaId))   
        return TypedResults.BadRequest(new { message = "ZonaId inválido." });
    
    if (!await db.Patios.AnyAsync(p => p.Id == dto.PatioId))  
        return TypedResults.BadRequest(new { message = "PatioId inválido." });
    
    if (!await db.Statuses.AnyAsync(s => s.Id == dto.StatusId)) 
        return TypedResults.BadRequest(new { message = "StatusId inválido." });
    
    if (await db.Motos.AnyAsync(m => m.Placa == dto.Placa))   
        return TypedResults.BadRequest(new { message = "Placa já cadastrada." });
    
    if (await db.Motos.AnyAsync(m => m.Chassi == dto.Chassi)) 
        return TypedResults.BadRequest(new { message = "Chassi já cadastrado." });

    var entity = new Moto
    {
        Id = await NextIdAsync(db, "moto"),
        Placa = dto.Placa.Trim().ToUpper(), 
        Chassi = dto.Chassi.Trim().ToUpper(), 
        QrCode = dto.QrCode?.Trim(),
        DataEntrada = dto.DataEntrada,
        PrevisaoEntrega = dto.PrevisaoEntrega,
        Fotos = dto.Fotos?.Trim(),
        ZonaId = dto.ZonaId, 
        PatioId = dto.PatioId, 
        StatusId = dto.StatusId,
        Observacoes = dto.Observacoes?.Trim()
    };
    
    db.Motos.Add(entity);
    await db.SaveChangesAsync();
    
    var zona = await db.Zonas.FindAsync(dto.ZonaId);
    var patio = await db.Patios.FindAsync(dto.PatioId);
    var status = await db.Statuses.Include(s => s.StatusGrupo).FirstAsync(s => s.Id == dto.StatusId);
    
    var result = new MotoResponseDto(
        entity.Id,
        entity.Placa,
        entity.Chassi,
        entity.QrCode,
        entity.DataEntrada,
        entity.PrevisaoEntrega,
        entity.Fotos,
        entity.ZonaId,
        entity.PatioId,
        entity.StatusId,
        entity.Observacoes,
        new ZonaResponseDto(zona!.Id, zona.Nome, zona.Letra),
        new PatioResponseDto(patio!.Id, patio.Nome),
        new StatusResponseDto(
            status.Id, 
            status.Nome, 
            status.StatusGrupoId,
            new StatusGrupoResponseDto(status.StatusGrupo.Id, status.StatusGrupo.Nome)
        )
    );
    
    return TypedResults.Created($"/api/motos/{entity.Id}", result);
})
.WithName("CreateMoto")
.WithOpenApi(op =>
{
    op.Summary = "Cria moto";
    op.Description = @"Cria uma nova moto no sistema com validações de integridade.

**Validações:**
- Placa e Chassi devem ser únicos
- ZonaId, PatioId e StatusId devem existir

**Status Codes:**
- `201 Created`: Moto criada com sucesso. Retorna Location header
- `400 Bad Request`: Dados inválidos, placa/chassi duplicados ou IDs não encontrados";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(MotoCreateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["placa"] = new OpenApiString("ABC1D23"),
                    ["chassi"] = new OpenApiString("9BWZZZ377VT004251"),
                    ["qrCode"] = new OpenApiString("QR001"),
                    ["dataEntrada"] = new OpenApiString("2024-11-20T10:30:00"),
                    ["previsaoEntrega"] = new OpenApiString("2024-11-25T18:00:00"),
                    ["fotos"] = new OpenApiString("foto1.jpg,foto2.jpg"),
                    ["zonaId"] = new OpenApiInteger(1),
                    ["patioId"] = new OpenApiInteger(1),
                    ["statusId"] = new OpenApiInteger(1),
                    ["observacoes"] = new OpenApiString("Honda CG 160 - Excelente estado")
                }
            }
        }
    };
    op.Responses["201"] = new OpenApiResponse { Description = "Moto criada com sucesso" };
    op.Responses["400"] = new OpenApiResponse { Description = "Dados inválidos ou duplicados" };
    return op;
});

motos.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, MotoUpdateDto dto) =>
{
    var m = await db.Motos.FindAsync(id);
    if (m is null) return TypedResults.NotFound(new { message = "Moto não encontrada" });
    
    if (!await db.Zonas.AnyAsync(z => z.Id == dto.ZonaId))     
        return TypedResults.BadRequest(new { message = "ZonaId inválido." });
    
    if (!await db.Patios.AnyAsync(p => p.Id == dto.PatioId))    
        return TypedResults.BadRequest(new { message = "PatioId inválido." });
    
    if (!await db.Statuses.AnyAsync(s => s.Id == dto.StatusId)) 
        return TypedResults.BadRequest(new { message = "StatusId inválido." });
    
    if (await db.Motos.AnyAsync(x => x.Placa == dto.Placa && x.Id != id))  
        return TypedResults.BadRequest(new { message = "Placa já cadastrada." });
    
    if (await db.Motos.AnyAsync(x => x.Chassi == dto.Chassi && x.Id != id)) 
        return TypedResults.BadRequest(new { message = "Chassi já cadastrado." });
    
    m.Placa = dto.Placa.Trim().ToUpper();
    m.Chassi = dto.Chassi.Trim().ToUpper();
    m.QrCode = dto.QrCode?.Trim();
    m.DataEntrada = dto.DataEntrada;
    m.PrevisaoEntrega = dto.PrevisaoEntrega;
    m.Fotos = dto.Fotos?.Trim();
    m.ZonaId = dto.ZonaId;
    m.PatioId = dto.PatioId;
    m.StatusId = dto.StatusId;
    m.Observacoes = dto.Observacoes?.Trim();
    
    await db.SaveChangesAsync();
    
    var zona = await db.Zonas.FindAsync(dto.ZonaId);
    var patio = await db.Patios.FindAsync(dto.PatioId);
    var status = await db.Statuses.Include(s => s.StatusGrupo).FirstAsync(s => s.Id == dto.StatusId);
    
    var result = new MotoResponseDto(
        m.Id,
        m.Placa,
        m.Chassi,
        m.QrCode,
        m.DataEntrada,
        m.PrevisaoEntrega,
        m.Fotos,
        m.ZonaId,
        m.PatioId,
        m.StatusId,
        m.Observacoes,
        new ZonaResponseDto(zona!.Id, zona.Nome, zona.Letra),
        new PatioResponseDto(patio!.Id, patio.Nome),
        new StatusResponseDto(
            status.Id, 
            status.Nome, 
            status.StatusGrupoId,
            new StatusGrupoResponseDto(status.StatusGrupo.Id, status.StatusGrupo.Nome)
        )
    );
    
    return TypedResults.Ok(result);
})
.WithName("UpdateMoto")
.WithOpenApi(op =>
{
    op.Summary = "Atualiza moto";
    op.Description = @"Atualiza uma moto existente com validações de integridade.

**Validações:**
- Placa e Chassi devem ser únicos (exceto para a própria moto)
- ZonaId, PatioId e StatusId devem existir

**Status Codes:**
- `200 OK`: Moto atualizada com sucesso
- `400 Bad Request`: Dados inválidos, placa/chassi duplicados ou IDs não encontrados
- `404 Not Found`: Moto não encontrada";
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(MotoUpdateDto)
                    }
                },
                Example = new OpenApiObject
                {
                    ["placa"] = new OpenApiString("XYZ9W88"),
                    ["chassi"] = new OpenApiString("9BWZZZ377VT009999"),
                    ["qrCode"] = new OpenApiString("QR999"),
                    ["dataEntrada"] = new OpenApiString("2024-11-20T10:30:00"),
                    ["previsaoEntrega"] = new OpenApiString("2024-11-30T15:00:00"),
                    ["fotos"] = new OpenApiString("foto_atualizada.jpg"),
                    ["zonaId"] = new OpenApiInteger(2),
                    ["patioId"] = new OpenApiInteger(2),
                    ["statusId"] = new OpenApiInteger(2),
                    ["observacoes"] = new OpenApiString("Yamaha Factor 125 - Transferida para zona sul")
                }
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse { Description = "Moto atualizada com sucesso" };
    op.Responses["400"] = new OpenApiResponse { Description = "Dados inválidos ou duplicados" };
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

motos.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var m = await db.Motos.FindAsync(id);
    if (m is null) return TypedResults.NotFound(new { message = "Moto não encontrada" });
    
    db.Motos.Remove(m);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithName("DeleteMoto")
.WithOpenApi(op =>
{
    op.Summary = "Remove moto";
    op.Description = @"Remove uma moto do sistema.

**Status Codes:**
- `204 No Content`: Moto removida com sucesso (sem corpo de resposta)
- `404 Not Found`: Moto não encontrada";
    op.Responses["204"] = new OpenApiResponse { Description = "Moto removida com sucesso" };
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

await app.RunAsync();