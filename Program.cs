using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using MottuVision.Data;
using MottuVision.Models;
using MottuVision.Dtos;
using System.Data.Common;
using System.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// DB Oracle
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("OracleConnection")
             ?? throw new InvalidOperationException("ConnectionStrings:OracleConnection não configurada.");
    opt.UseOracle(cs);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mottu Vision API",
        Version = "v1",
        Description = "API RESTful (.NET 8 Minimal API) com boas práticas, paginação, HATEOAS e exemplos."
    });
});

var app = builder.Build();

//  seeder/migração
using (var scope = app.Services.CreateScope())
{
    
}

app.UseSwagger();
app.UseSwaggerUI();

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

    if (result == DBNull.Value)
    {
        return 1;
    }
    return Convert.ToDecimal(result);
}





// ================== USUÁRIOS ==================
var usuarios = app.MapGroup("/api/usuarios").WithTags("Usuários");

// GET /api/usuarios
usuarios.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = page < 1 ? 1 : page;
    pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

    var total = await db.Usuarios.LongCountAsync();
    var pageItems = await db.Usuarios
        .OrderBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize)
        .Select(u => new { u.Id, Usuario = u.NomeUsuario })
        .ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, pageItems, page, pageSize, total, "/api/usuarios"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista usuários (paginado)";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Lista paginada de usuários",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""
                {
                  "items":[{"id":1,"usuario":"admin"}],
                  "page":1,"pageSize":20,"totalCount":1,
                  "links":[{"rel":"self","href":"https://localhost/api/usuarios?page=1&pageSize=20","method":"GET"}]
                }
                """)
            }
        }
    };
    return op;
});

// GET /api/usuarios/{id}
usuarios.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound();
    return TypedResults.Ok(new { u.Id, Usuario = u.NomeUsuario });
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém usuário por ID";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Usuário encontrado",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id": 1, "usuario": "admin" }""")
            }
        }
    };
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

// POST /api/usuarios
usuarios.MapPost("/", async Task<IResult> (AppDbContext db, UsuarioCreateDto dto) =>
{
    if (await db.Usuarios.AnyAsync(x => x.NomeUsuario == dto.Usuario))
        return TypedResults.BadRequest("Usuário já existe.");

    var entity = new Usuario
    {
        Id = await NextIdAsync(db, "usuario"),
        NomeUsuario = dto.Usuario,
        SenhaHash = dto.Senha
    };
    db.Usuarios.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/usuarios/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria usuário";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "usuario":"novo_user", "senha":"Forte@123" }""")
            }
        }
    };
    op.Responses["201"] = new OpenApiResponse
    {
        Description = "Usuário criado",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id": 2, "nomeUsuario":"novo_user", "senhaHash":"<hash>" }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    return op;
});

// PUT /api/usuarios/{id}
usuarios.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, UsuarioUpdateDto dto) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound();

    if (await db.Usuarios.AnyAsync(x => x.NomeUsuario == dto.Usuario && x.Id != id))
        return TypedResults.BadRequest("Já existe outro usuário com esse nome.");

    u.NomeUsuario = dto.Usuario;
    u.SenhaHash = dto.Senha;
    await db.SaveChangesAsync();
    return TypedResults.Ok(new { u.Id, Usuario = u.NomeUsuario });
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza usuário";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "usuario":"admin2", "senha":"Nova@123" }""")
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Usuário atualizado",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id": 1, "usuario": "admin2" }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

// DELETE /api/usuarios/{id}
usuarios.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var u = await db.Usuarios.FindAsync(id);
    if (u is null) return TypedResults.NotFound();
    db.Usuarios.Remove(u);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove usuário";
    op.Responses["204"] = new OpenApiResponse { Description = "Usuário removido" };
    op.Responses["404"] = new OpenApiResponse { Description = "Usuário não encontrado" };
    return op;
});

// ================== ZONAS ==================
var zonas = app.MapGroup("/api/zonas").WithTags("Zonas");

// GET /api/zonas
zonas.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    var total = await db.Zonas.LongCountAsync();
    var items = await db.Zonas
        .OrderBy(z => z.Id).Skip((page - 1) * pageSize).Take(pageSize)
        .ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, items, page, pageSize, total, "/api/zonas"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista zonas (paginado)";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Lista paginada de zonas",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""
                { "items":[{"id":1,"nome":"Zona Leste","letra":"L"}], "page":1, "pageSize":20, "totalCount":1 }
                """)
            }
        }
    };
    return op;
});

// GET /api/zonas/{id}
zonas.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var z = await db.Zonas.FindAsync(id);
    if (z is null) return TypedResults.NotFound();
    return TypedResults.Ok(z);
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém zona por ID";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Zona encontrada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":1, "nome":"Zona Leste", "letra":"L" }""")
            }
        }
    };
    op.Responses["404"] = new OpenApiResponse { Description = "Zona não encontrada" };
    return op;
});

// POST /api/zonas
zonas.MapPost("/", async Task<IResult> (AppDbContext db, ZonaCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || dto.Letra?.Length != 1)
        return TypedResults.BadRequest("Nome obrigatório e Letra deve ter 1 caractere.");

    var entity = new Zona { Id = await NextIdAsync(db, "zona"), Nome = dto.Nome, Letra = dto.Letra! };
    db.Zonas.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/zonas/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria zona";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "nome":"Zona Norte", "letra":"N" }""")
            }
        }
    };
    op.Responses["201"] = new OpenApiResponse
    {
        Description = "Zona criada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":2, "nome":"Zona Norte", "letra":"N" }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    return op;
});

// PUT /api/zonas/{id}
zonas.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, ZonaUpdateDto dto) =>
{
    var z = await db.Zonas.FindAsync(id);
    if (z is null) return TypedResults.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Nome) || dto.Letra?.Length != 1)
        return TypedResults.BadRequest("Nome obrigatório e Letra deve ter 1 caractere.");

    z.Nome = dto.Nome;
    z.Letra = dto.Letra!;
    await db.SaveChangesAsync();
    return TypedResults.Ok(z);
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza zona";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "nome":"Zona Leste Atualizada", "letra":"L" }""")
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Zona atualizada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":1, "nome":"Zona Leste Atualizada", "letra":"L" }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    op.Responses["404"] = new OpenApiResponse { Description = "Zona não encontrada" };
    return op;
});

// DELETE /api/zonas/{id}
zonas.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var z = await db.Zonas.FindAsync(id);
    if (z is null) return TypedResults.NotFound();
    db.Zonas.Remove(z);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove zona";
    op.Responses["204"] = new OpenApiResponse { Description = "Zona removida" };
    op.Responses["404"] = new OpenApiResponse { Description = "Zona não encontrada" };
    return op;
});

// ================== MOTOS ==================
var motos = app.MapGroup("/api/motos").WithTags("Motos");

// GET /api/motos
motos.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20, string? placa = null) =>
{
    var query = db.Motos.Include(m => m.Zona).Include(m => m.Patio).Include(m => m.Status).AsQueryable();
    if (!string.IsNullOrWhiteSpace(placa))
        query = query.Where(m => m.Placa.ToLower().Contains(placa.ToLower()));

    var total = await query.LongCountAsync();
    var items = await query.OrderBy(m => m.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, items, page, pageSize, total, "/api/motos"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista motos (paginado, filtro por placa)";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Lista paginada de motos",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""
                { "items":[{"id":1,"placa":"ABC1D23","chassi":"9BW...","qrCode":"QR001","zonaId":1,"patioId":1,"statusId":1}], "page":1,"pageSize":20,"totalCount":1 }
                """)
            }
        }
    };
    return op;
});

// GET /api/motos/{id}
motos.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var m = await db.Motos.Include(x => x.Zona).Include(x => x.Patio).Include(x => x.Status)
                          .FirstOrDefaultAsync(x => x.Id == id);
    if (m is null) return TypedResults.NotFound();
    return TypedResults.Ok(m);
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém moto por ID";
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Moto encontrada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":1, "placa":"ABC1D23", "chassi":"9BW...", "qrCode":"QR001", "zonaId":1, "patioId":1, "statusId":1 }""")
            }
        }
    };
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

// POST /api/motos
motos.MapPost("/", async Task<IResult> (AppDbContext db, MotoCreateDto dto) =>
{
    if (!await db.Zonas.AnyAsync(z => z.Id == dto.ZonaId))   return TypedResults.BadRequest("ZonaId inválido.");
    if (!await db.Patios.AnyAsync(p => p.Id == dto.PatioId))  return TypedResults.BadRequest("PatioId inválido.");
    if (!await db.Statuses.AnyAsync(s => s.Id == dto.StatusId)) return TypedResults.BadRequest("StatusId inválido.");
    if (await db.Motos.AnyAsync(m => m.Placa == dto.Placa))   return TypedResults.BadRequest("Placa já cadastrada.");
    if (await db.Motos.AnyAsync(m => m.Chassi == dto.Chassi)) return TypedResults.BadRequest("Chassi já cadastrado.");

    var entity = new Moto
    {
        Id = await NextIdAsync(db, "moto"),
        Placa = dto.Placa, Chassi = dto.Chassi, QrCode = dto.QrCode,
        ZonaId = dto.ZonaId, PatioId = dto.PatioId, StatusId = dto.StatusId
    };
    db.Motos.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/motos/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria moto";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "placa":"JKL7M89","chassi":"9BWZZZ377VT004253","qrCode":"QR003","zonaId":1,"patioId":1,"statusId":1 }""")
            }
        }
    };
    op.Responses["201"] = new OpenApiResponse
    {
        Description = "Moto criada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":3, "placa":"JKL7M89", "chassi":"9BWZZZ377VT004253", "qrCode":"QR003", "zonaId":1, "patioId":1, "statusId":1 }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    return op;
});

// PUT /api/motos/{id}
motos.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, MotoUpdateDto dto) =>
{
    var m = await db.Motos.FindAsync(id);
    if (m is null) return TypedResults.NotFound();

    if (!await db.Zonas.AnyAsync(z => z.Id == dto.ZonaId))     return TypedResults.BadRequest("ZonaId inválido.");
    if (!await db.Patios.AnyAsync(p => p.Id == dto.PatioId))    return TypedResults.BadRequest("PatioId inválido.");
    if (!await db.Statuses.AnyAsync(s => s.Id == dto.StatusId)) return TypedResults.BadRequest("StatusId inválido.");
    if (await db.Motos.AnyAsync(x => x.Placa == dto.Placa && x.Id != id))  return TypedResults.BadRequest("Placa já cadastrada.");
    if (await db.Motos.AnyAsync(x => x.Chassi == dto.Chassi && x.Id != id)) return TypedResults.BadRequest("Chassi já cadastrado.");

    m.Placa = dto.Placa; m.Chassi = dto.Chassi; m.QrCode = dto.QrCode;
    m.ZonaId = dto.ZonaId; m.PatioId = dto.PatioId; m.StatusId = dto.StatusId;
    await db.SaveChangesAsync();
    return TypedResults.Ok(m);
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza moto";
    op.RequestBody = new OpenApiRequestBody
    {
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "placa":"ABC1D23","chassi":"9BW...","qrCode":"QR010","zonaId":2,"patioId":1,"statusId":2 }""")
            }
        }
    };
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Moto atualizada",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""{ "id":1, "placa":"ABC1D23", "chassi":"9BW...", "qrCode":"QR010", "zonaId":2, "patioId":1, "statusId":2 }""")
            }
        }
    };
    op.Responses["400"] = new OpenApiResponse { Description = "Erro de validação" };
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

// DELETE /api/motos/{id}
motos.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var m = await db.Motos.FindAsync(id);
    if (m is null) return TypedResults.NotFound();
    db.Motos.Remove(m);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove moto";
    op.Responses["204"] = new OpenApiResponse { Description = "Moto removida" };
    op.Responses["404"] = new OpenApiResponse { Description = "Moto não encontrada" };
    return op;
});

// ================== PATIOS ==================
var patios = app.MapGroup("/api/patios").WithTags("Patios");

// GET /api/patios
patios.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = page < 1 ? 1 : page;
    pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

    var total = await db.Patios.LongCountAsync();
    var items = await db.Patios.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, items, page, pageSize, total, "/api/patios"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista pátios (paginado)";
    return op;
});

// GET /api/patios/{id}
patios.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var p = await db.Patios.FindAsync(id);
    if (p is null) return TypedResults.NotFound();
    return TypedResults.Ok(p);
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém pátio por ID";
    return op;
});

// POST /api/patios
patios.MapPost("/", async Task<IResult> (AppDbContext db, PatioCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    var entity = new Patio { Id = await NextIdAsync(db, "patio"), Nome = dto.Nome };
    db.Patios.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/patios/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria pátio";
    return op;
});

// PUT /api/patios/{id}
patios.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, PatioUpdateDto dto) =>
{
    var p = await db.Patios.FindAsync(id);
    if (p is null) return TypedResults.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    p.Nome = dto.Nome;
    await db.SaveChangesAsync();
    return TypedResults.Ok(p);
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza pátio";
    return op;
});

// DELETE /api/patios/{id}
patios.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var p = await db.Patios.Include(x => x.Motos).FirstOrDefaultAsync(x => x.Id == id);
    if (p is null) return TypedResults.NotFound();
    if (p.Motos.Any()) return TypedResults.BadRequest("Não é possível remover pátio com motos associadas.");
    db.Patios.Remove(p);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove pátio";
    return op;
});

// ================== STATUS GRUPOS ==================
var statusGrupos = app.MapGroup("/api/statusgrupos").WithTags("StatusGrupo");

// GET /api/statusgrupos
statusGrupos.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = page < 1 ? 1 : page;
    pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

    var total = await db.StatusGrupos.LongCountAsync();
    var items = await db.StatusGrupos.OrderBy(sg => sg.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, items, page, pageSize, total, "/api/statusgrupos"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista status grupos (paginado)";
    return op;
});

// GET /api/statusgrupos/{id}
statusGrupos.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var sg = await db.StatusGrupos.Include(x => x.Statuses).FirstOrDefaultAsync(x => x.Id == id);
    if (sg is null) return TypedResults.NotFound();
    return TypedResults.Ok(sg);
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém status grupo por ID";
    return op;
});

// POST /api/statusgrupos
statusGrupos.MapPost("/", async Task<IResult> (AppDbContext db, StatusGrupoCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    var entity = new StatusGrupo { Id = await NextIdAsync(db, "status_grupo"), Nome = dto.Nome };
    db.StatusGrupos.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/statusgrupos/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria status grupo";
    return op;
});

// PUT /api/statusgrupos/{id}
statusGrupos.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, StatusGrupoUpdateDto dto) =>
{
    var sg = await db.StatusGrupos.FindAsync(id);
    if (sg is null) return TypedResults.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    sg.Nome = dto.Nome;
    await db.SaveChangesAsync();
    return TypedResults.Ok(sg);
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza status grupo";
    return op;
});

// DELETE /api/statusgrupos/{id}
statusGrupos.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var sg = await db.StatusGrupos.Include(x => x.Statuses).FirstOrDefaultAsync(x => x.Id == id);
    if (sg is null) return TypedResults.NotFound();
    if (sg.Statuses.Any()) return TypedResults.BadRequest("Não é possível remover StatusGrupo que contém Statuses.");
    db.StatusGrupos.Remove(sg);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove status grupo";
    return op;
});

// ================== STATUS ==================
var statuses = app.MapGroup("/api/statuses").WithTags("Status");

// GET /api/statuses
statuses.MapGet("/", async (HttpContext ctx, AppDbContext db, int page = 1, int pageSize = 20) =>
{
    page = page < 1 ? 1 : page;
    pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

    var query = db.Statuses.Include(s => s.StatusGrupo).AsQueryable();
    var total = await query.LongCountAsync();
    var items = await query.OrderBy(s => s.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return TypedResults.Ok(ToPaged(ctx, items, page, pageSize, total, "/api/statuses"));
})
.WithOpenApi(op =>
{
    op.Summary = "Lista status (paginado)";
    return op;
});

// GET /api/statuses/{id}
statuses.MapGet("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var s = await db.Statuses.Include(x => x.StatusGrupo).FirstOrDefaultAsync(x => x.Id == id);
    if (s is null) return TypedResults.NotFound();
    return TypedResults.Ok(s);
})
.WithOpenApi(op =>
{
    op.Summary = "Obtém status por ID";
    return op;
});

// POST /api/statuses
statuses.MapPost("/", async Task<IResult> (AppDbContext db, StatusCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    if (!await db.StatusGrupos.AnyAsync(x => x.Id == dto.StatusGrupoId)) return TypedResults.BadRequest("StatusGrupoId inválido.");

    var entity = new Status
    {
        Id = await NextIdAsync(db, "status"),
        Nome = dto.Nome,
        StatusGrupoId = dto.StatusGrupoId
    };
    db.Statuses.Add(entity);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/statuses/{entity.Id}", entity);
})
.WithOpenApi(op =>
{
    op.Summary = "Cria status";
    return op;
});

// PUT /api/statuses/{id}
statuses.MapPut("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id, StatusUpdateDto dto) =>
{
    var s = await db.Statuses.FindAsync(id);
    if (s is null) return TypedResults.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Nome)) return TypedResults.BadRequest("Nome obrigatório.");
    if (!await db.StatusGrupos.AnyAsync(x => x.Id == dto.StatusGrupoId)) return TypedResults.BadRequest("StatusGrupoId inválido.");

    s.Nome = dto.Nome;
    s.StatusGrupoId = dto.StatusGrupoId;
    await db.SaveChangesAsync();
    return TypedResults.Ok(s);
})
.WithOpenApi(op =>
{
    op.Summary = "Atualiza status";
    return op;
});

// DELETE /api/statuses/{id}
statuses.MapDelete("/{id:decimal}", async Task<IResult> (AppDbContext db, decimal id) =>
{
    var s = await db.Statuses.Include(x => x.Motos).FirstOrDefaultAsync(x => x.Id == id);
    if (s is null) return TypedResults.NotFound();
    if (s.Motos.Any()) return TypedResults.BadRequest("Não é possível remover Status com motos associadas.");
    db.Statuses.Remove(s);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
})
.WithOpenApi(op =>
{
    op.Summary = "Remove status";
    return op;
});




await app.RunAsync();
