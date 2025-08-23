using Microsoft.EntityFrameworkCore;
using MottuVision.Models;

namespace MottuVision.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Patio> Patios => Set<Patio>();
    public DbSet<StatusGrupo> StatusGrupos => Set<StatusGrupo>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Moto> Motos => Set<Moto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // usuario
        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("usuario");
            b.HasKey(x => x.Id).HasName("usuario_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.NomeUsuario).HasColumnName("usuario").HasMaxLength(50).IsRequired();
            b.Property(x => x.SenhaHash).HasColumnName("senha").HasMaxLength(255).IsRequired();
            b.HasIndex(x => x.NomeUsuario).IsUnique().HasDatabaseName("usuario_usuario_uk");
        });

        // zona
        modelBuilder.Entity<Zona>(b =>
        {
            b.ToTable("zona");
            b.HasKey(x => x.Id).HasName("zona_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            b.Property(x => x.Letra).HasColumnName("letra").HasMaxLength(1).IsRequired();
        });

        // patio
        modelBuilder.Entity<Patio>(b =>
        {
            b.ToTable("patio");
            b.HasKey(x => x.Id).HasName("patio_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
        });

        // status_grupo
        modelBuilder.Entity<StatusGrupo>(b =>
        {
            b.ToTable("status_grupo");
            b.HasKey(x => x.Id).HasName("status_grupo_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
        });

        // status
        modelBuilder.Entity<Status>(b =>
        {
            b.ToTable("status");
            b.HasKey(x => x.Id).HasName("status_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            b.Property(x => x.StatusGrupoId).HasColumnName("status_grupo_id").IsRequired();
            b.HasOne(x => x.StatusGrupo)
             .WithMany(g => g.Statuses)
             .HasForeignKey(x => x.StatusGrupoId)
             .HasConstraintName("status_fk");
        });

        // moto
        modelBuilder.Entity<Moto>(b =>
        {
            b.ToTable("moto");
            b.HasKey(x => x.Id).HasName("moto_pk");
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Placa).HasColumnName("placa").HasMaxLength(10).IsRequired();
            b.Property(x => x.Chassi).HasColumnName("chassi").HasMaxLength(20).IsRequired();
            b.Property(x => x.QrCode).HasColumnName("qr_code").HasMaxLength(255);
            b.Property(x => x.DataEntrada).HasColumnName("data_entrada").IsRequired();
            b.Property(x => x.PrevisaoEntrega).HasColumnName("previsao_entrega");
            b.Property(x => x.Fotos).HasColumnName("fotos").HasMaxLength(255);
            b.Property(x => x.ZonaId).HasColumnName("zona_id").IsRequired();
            b.Property(x => x.PatioId).HasColumnName("patio_id").IsRequired();
            b.Property(x => x.StatusId).HasColumnName("status_id").IsRequired();
            b.Property(x => x.Observacoes).HasColumnName("observacoes");
            b.HasIndex(x => x.Placa).IsUnique().HasDatabaseName("moto_placa_uk");
            b.HasIndex(x => x.Chassi).IsUnique().HasDatabaseName("moto_chassi_uk");

            b.HasOne(x => x.Zona).WithMany(z => z.Motos).HasForeignKey(x => x.ZonaId).HasConstraintName("moto_zona_fk");
            b.HasOne(x => x.Patio).WithMany(p => p.Motos).HasForeignKey(x => x.PatioId).HasConstraintName("moto_patio_fk");
            b.HasOne(x => x.Status).WithMany(s => s.Motos).HasForeignKey(x => x.StatusId).HasConstraintName("moto_status_fk");
        });
    }
}