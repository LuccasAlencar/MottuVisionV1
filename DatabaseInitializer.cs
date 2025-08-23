using System.Data;
using Microsoft.EntityFrameworkCore;
using MottuVision.Data;
using Oracle.ManagedDataAccess.Client;

namespace MottuVision.Api;
public static class DatabaseInitializer
{
    public static async Task EnsureCreatedAndSeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var enabled = cfg.GetValue("Seed:Enabled", true);
        if (!enabled) return;

        var db = sp.GetRequiredService<AppDbContext>();
        var conn = (OracleConnection)db.Database.GetDbConnection();

        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();

        // ========== DROP TABLES (em ordem para respeitar FKs) ==========
        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE moto CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE status CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE status_grupo CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE patio CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE zona CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE usuario CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;";
        await cmd.ExecuteNonQueryAsync(ct);

        // ========== CREATE TABLES ==========
        cmd.CommandText = @"
CREATE TABLE usuario (
  id NUMBER(10,0) NOT NULL,
  usuario VARCHAR2(50) NOT NULL,
  senha VARCHAR2(255) NOT NULL,
  CONSTRAINT usuario_pk PRIMARY KEY (id),
  CONSTRAINT usuario_usuario_uk UNIQUE (usuario)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
CREATE TABLE zona (
  id NUMBER(10,0) NOT NULL,
  nome VARCHAR2(50) NOT NULL,
  letra VARCHAR2(1) NOT NULL,
  CONSTRAINT zona_pk PRIMARY KEY (id)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
CREATE TABLE patio (
  id NUMBER(10,0) NOT NULL,
  nome VARCHAR2(50) NOT NULL,
  CONSTRAINT patio_pk PRIMARY KEY (id)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
CREATE TABLE status_grupo (
  id NUMBER(10,0) NOT NULL,
  nome VARCHAR2(50) NOT NULL,
  CONSTRAINT status_grupo_pk PRIMARY KEY (id)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
CREATE TABLE status (
  id NUMBER(10,0) NOT NULL,
  nome VARCHAR2(50) NOT NULL,
  status_grupo_id NUMBER(10,0) NOT NULL,
  CONSTRAINT status_pk PRIMARY KEY (id),
  CONSTRAINT status_fk FOREIGN KEY (status_grupo_id) REFERENCES status_grupo(id)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
CREATE TABLE moto (
  id NUMBER(10,0) NOT NULL,
  placa VARCHAR2(10) NOT NULL,
  chassi VARCHAR2(20) NOT NULL,
  qr_code VARCHAR2(255),
  data_entrada TIMESTAMP(6) NOT NULL,
  previsao_entrega TIMESTAMP(6),
  fotos VARCHAR2(255),
  zona_id NUMBER(10,0) NOT NULL,
  patio_id NUMBER(10,0) NOT NULL,
  status_id NUMBER(10,0) NOT NULL,
  observacoes CLOB,
  CONSTRAINT moto_pk PRIMARY KEY (id),
  CONSTRAINT moto_placa_uk UNIQUE (placa),
  CONSTRAINT moto_chassi_uk UNIQUE (chassi),
  CONSTRAINT moto_zona_fk FOREIGN KEY (zona_id) REFERENCES zona(id),
  CONSTRAINT moto_patio_fk FOREIGN KEY (patio_id) REFERENCES patio(id),
  CONSTRAINT moto_status_fk FOREIGN KEY (status_id) REFERENCES status(id)
)";
        await cmd.ExecuteNonQueryAsync(ct);

        // ========== INSERT SEED DATA ==========
        cmd.CommandText = @"
INSERT INTO usuario (id, usuario, senha) VALUES (1, 'admin', 'admin@123');
INSERT INTO usuario (id, usuario, senha) VALUES (2, 'operador', '123456');";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
INSERT INTO zona (id, nome, letra) VALUES (1, 'Norte', 'N');
INSERT INTO zona (id, nome, letra) VALUES (2, 'Sul', 'S');";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
INSERT INTO patio (id, nome) VALUES (1, 'Pátio A');
INSERT INTO patio (id, nome) VALUES (2, 'Pátio B');";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
INSERT INTO status_grupo (id, nome) VALUES (1, 'Operacional');
INSERT INTO status_grupo (id, nome) VALUES (2, 'Exceção');
INSERT INTO status (id, nome, status_grupo_id) VALUES (1, 'OK', 1);
INSERT INTO status (id, nome, status_grupo_id) VALUES (2, 'Manutenção', 1);
INSERT INTO status (id, nome, status_grupo_id) VALUES (3, 'Sinistro', 2);";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
INSERT INTO moto (id, placa, chassi, qr_code, data_entrada, previsao_entrega, fotos, zona_id, patio_id, status_id, observacoes)
VALUES (1, 'ABC1D23', '9BWZZZ377VT004251', 'QR001', SYSTIMESTAMP, SYSTIMESTAMP+1, null, 1, 1, 1, 'Moto em perfeito estado');
INSERT INTO moto (id, placa, chassi, qr_code, data_entrada, previsao_entrega, fotos, zona_id, patio_id, status_id, observacoes)
VALUES (2, 'EFG4H56', '9BWZZZ377VT004252', 'QR002', SYSTIMESTAMP, null, null, 2, 2, 2, 'Em manutenção preventiva');";
        await cmd.ExecuteNonQueryAsync(ct);

        await conn.CloseAsync();
    }
}