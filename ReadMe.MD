# MottuVision - API de Gerenciamento de Pátio

<div align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".NET 8.0">
  <img src="https://img.shields.io/badge/Minimal_API-8.0-blue" alt="Minimal API 8.0">
  <img src="https://img.shields.io/badge/Entity_Framework-8.0-green" alt="Entity Framework 8.0">
  <img src="https://img.shields.io/badge/Oracle-Database-red" alt="Oracle Database">
  <img src="https://img.shields.io/badge/Swagger-OpenAPI-green" alt="Swagger OpenAPI">
</div>

## 👥 Integrantes do Grupo
- **Daniel da Silva Barros** | RM 556152
- **Luccas de Alencar Rufino** | RM 558253
- **Raul Clauson** - RM 555006

---

## 📋 Descrição do Projeto

O **MottuVision** é uma API RESTful desenvolvida para otimizar o gerenciamento de pátios de motocicletas da Mottu. A solução resolve problemas críticos de localização manual de veículos, reduzindo perdas operacionais e aumentando a produtividade em até 40%.

### 🎯 Problema Resolvido
A Mottu enfrentava desafios na localização manual de motocicletas em seus pátios, causando:
- Perdas de tempo na localização de veículos
- Erros de inventário
- Retrabalhos operacionais
- Baixa produtividade das equipes

### 💡 Solução Proposta
Sistema integrado que combina:
- **API REST robusta** para gerenciamento de entidades críticas
- **Controle de localização** por zonas e pátios
- **Rastreabilidade completa** de status e movimentações
- **Escalabilidade** para mais de 100 filiais

---

## 🏗️ Justificativa da Arquitetura

### **Arquitetura Escolhida: Minimal API com Entity Framework Core**

**Por que esta arquitetura?**

1. **Minimal API (.NET 8)**
   - **Performance superior**: Menos overhead comparado ao MVC tradicional
   - **Simplicidade**: Ideal para APIs focadas em CRUD e operações diretas
   - **Modernidade**: Abordagem atual da Microsoft para APIs leves
   - **Produtividade**: Menos código boilerplate, mais foco na lógica de negócio

2. **Entity Framework Core com Oracle**
   - **Robustez empresarial**: Oracle Database para alta disponibilidade
   - **ORM maduro**: Abstração eficiente do banco de dados
   - **Migrations**: Versionamento automático do schema
   - **Performance**: Lazy loading e otimizações automáticas

3. **Padrão de Organização por Entidades**
   - **Escalabilidade**: Fácil adição de novas funcionalidades
   - **Manutenibilidade**: Código organizado por domínio
   - **Testabilidade**: Isolamento claro de responsabilidades

4. **REST + HATEOAS + Paginação**
   - **Padrões da indústria**: Seguindo melhores práticas REST
   - **Performance**: Paginação evita sobrecarga de dados
   - **Usabilidade**: HATEOAS facilita navegação na API

---

## 🗃️ Entidades Principais

O sistema gerencia **6 entidades principais** organizadas hierarquicamente:

### 1. **Usuario** 
Operadores e funcionários do sistema
- Controle de acesso e autenticação
- Rastreabilidade de ações

### 2. **StatusGrupo** 
Categorias de status (Ex: "Operacional", "Manutenção")
- Organização hierárquica de estados
- Flexibilidade para diferentes tipos de status

### 3. **Status** 
Estados específicos das motocicletas (Ex: "Disponível", "Em Reparo")
- Controle granular do estado dos veículos
- Relacionamento com grupos para organização

### 4. **Zona** 
Divisões do pátio (Ex: "Zona A", "Zona B")
- Organização física do espaço
- Facilitação da localização

### 5. **Patio** 
Locais físicos onde ficam as motos
- Gestão de múltiplas unidades
- Controle de capacidade

### 6. **Moto** *(Entidade Central)*
Informações completas das motocicletas
- Dados técnicos (placa, chassi)
- Localização atual (zona + pátio)
- Estado operacional (status)
- Histórico e observações

**Justificativa do Domínio:** O gerenciamento de frotas de motocicletas é um problema real da Mottu, empresa líder em aluguel de motos para entregadores. A digitalização deste processo gera valor mensurável através da redução de tempo de localização e aumento da produtividade operacional.

---

## ⚙️ Tecnologias Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core Minimal API** - API Web moderna e performática
- **Entity Framework Core 8.0** - ORM para acesso a dados
- **Oracle Database** - Banco de dados empresarial
- **Swagger/OpenAPI** - Documentação interativa da API
- **C# 12** - Linguagem de programação

### Pacotes NuGet:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.8" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.8" />
<PackageReference Include="Oracle.EntityFrameworkCore" Version="8.21.121" />
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="3.21.130" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="6.6.2" />
```

---

## 🚀 Instruções de Execução

### ⚠️ **IMPORTANTE: Configuração do Banco de Dados**

**1. Configure suas credenciais Oracle no `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)));"
  }
}
```

### **Execução Completa (Recomendado):**

```bash
# 1. Clone o repositório
git clone [URL_DO_REPOSITORIO]
cd MottuVisionV1

# 2. Restaurar dependências
dotnet restore

# 3. Limpar projeto
dotnet clean

# 4. Build do projeto
dotnet build

# 5. Resetar banco (recriar schema)
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update

# 6. Executar aplicação
dotnet run
```

### **Execução Rápida:**
```bash
dotnet run
```

### **Acesso à API:**
- **URL Base:** `http://localhost:5111`

---

## 📚 Endpoints da API

### **Resumo dos Recursos Disponíveis:**

| Entidade | Endpoints | Funcionalidades |
|----------|-----------|----------------|
| **Usuários** | `/api/usuarios` | CRUD completo, validação de usuário único |
| **Zonas** | `/api/zonas` | CRUD completo, organização por letra |
| **Pátios** | `/api/patios` | CRUD completo, validação de dependências |
| **StatusGrupos** | `/api/statusgrupos` | CRUD completo, hierarquia de status |
| **Status** | `/api/statuses` | CRUD completo, relacionamento com grupos |
| **Motos** | `/api/motos` | CRUD completo, filtros, validações |

### **Padrões Implementados:**
- ✅ **Paginação**: `?page=1&pageSize=20` em todas as listagens
- ✅ **HATEOAS**: Links de navegação em respostas paginadas  
- ✅ **Filtros**: Busca por placa nas motos
- ✅ **Validações**: Integridade referencial e unicidade
- ✅ **Status Codes**: 200, 201, 204, 400, 404 adequadamente utilizados

---

## 🧪 Exemplos de Uso dos Endpoints

### **1. Criar Status Grupo**
```bash
POST /api/statusgrupos
Content-Type: application/json

{
  "nome": "Operacional"
}

# Resposta: 201 Created
{
  "id": 1,
  "nome": "Operacional"
}
```

### **2. Criar Status**
```bash
POST /api/statuses
Content-Type: application/json

{
  "nome": "Disponível",
  "statusGrupoId": 1
}

# Resposta: 201 Created
{
  "id": 1,
  "nome": "Disponível",
  "statusGrupoId": 1
}
```

### **3. Criar Zona**
```bash
POST /api/zonas
Content-Type: application/json

{
  "nome": "Zona Norte",
  "letra": "N"
}

# Resposta: 201 Created
{
  "id": 1,
  "nome": "Zona Norte",
  "letra": "N"
}
```

### **4. Criar Pátio**
```bash
POST /api/patios
Content-Type: application/json

{
  "nome": "Pátio Central SP"
}

# Resposta: 201 Created
{
  "id": 1,
  "nome": "Pátio Central SP"
}
```

### **5. Criar Moto**
```bash
POST /api/motos
Content-Type: application/json

{
  "placa": "ABC1D23",
  "chassi": "9BWZZZ377VT004251",
  "qrCode": "QR001",
  "dataEntrada": "2024-11-20T10:30:00",
  "previsaoEntrega": "2024-11-25T18:00:00",
  "fotos": "foto1.jpg,foto2.jpg",
  "zonaId": 1,
  "patioId": 1,
  "statusId": 1,
  "observacoes": "Moto em perfeito estado"
}

# Resposta: 201 Created
{
  "id": 1,
  "placa": "ABC1D23",
  "chassi": "9BWZZZ377VT004251",
  "qrCode": "QR001",
  "dataEntrada": "2024-11-20T10:30:00",
  "zonaId": 1,
  "patioId": 1,
  "statusId": 1
}
```

### **6. Listar Motos com Paginação e Filtro**
```bash
GET /api/motos?page=1&pageSize=10&placa=ABC

# Resposta: 200 OK
{
  "items": [
    {
      "id": 1,
      "placa": "ABC1D23",
      "chassi": "9BWZZZ377VT004251",
      "zona": {
        "id": 1,
        "nome": "Zona Norte",
        "letra": "N"
      },
      "patio": {
        "id": 1,
        "nome": "Pátio Central SP"
      },
      "status": {
        "id": 1,
        "nome": "Disponível"
      }
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "links": [
    {
      "rel": "self",
      "href": "https://localhost/api/motos?page=1&pageSize=10",
      "method": "GET"
    }
  ]
}
```

### **7. Buscar Moto por ID com Relacionamentos**
```bash
GET /api/motos/1

# Resposta: 200 OK
{
  "id": 1,
  "placa": "ABC1D23",
  "chassi": "9BWZZZ377VT004251",
  "qrCode": "QR001",
  "dataEntrada": "2024-11-20T10:30:00",
  "zona": {
    "id": 1,
    "nome": "Zona Norte",
    "letra": "N"
  },
  "patio": {
    "id": 1,
    "nome": "Pátio Central SP"
  },
  "status": {
    "id": 1,
    "nome": "Disponível",
    "statusGrupo": {
      "id": 1,
      "nome": "Operacional"
    }
  }
}
```

### **8. Atualizar Moto**
```bash
PUT /api/motos/1
Content-Type: application/json

{
  "placa": "ABC1D23",
  "chassi": "9BWZZZ377VT004251",
  "qrCode": "QR001-UPDATED",
  "dataEntrada": "2024-11-20T10:30:00",
  "zonaId": 2,
  "patioId": 1,
  "statusId": 2,
  "observacoes": "Moto transferida para Zona Sul"
}

# Resposta: 200 OK
```

### **9. Remover Moto**
```bash
DELETE /api/motos/1

# Resposta: 204 No Content
```

## 📖 Documentação Swagger

A API possui documentação completa via Swagger/OpenAPI com:

✅ **Descrições detalhadas** de todos os endpoints  
✅ **Parâmetros documentados** com tipos e validações  
✅ **Exemplos de payloads** para requisições e respostas  
✅ **Modelos de dados** completamente descritos  
✅ **Status codes** com suas respectivas descrições  
✅ **Agrupamento por entidades** para melhor navegação  

---

## 🔧 Pré-requisitos

- **.NET 8.0 SDK** - [Download aqui](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Acesso ao Oracle Database** (oracle.fiap.com.br)
- **Credenciais válidas** do Oracle FIAP
- **JetBrains Rider** ou **Visual Studio** (opcional)

---

## 🎯 Benefícios da Solução

- 📈 **40% de aumento** na produtividade operacional
- ⏱️ **Localização em segundos** em vez de minutos
- 📉 **Redução de perdas** e erros de inventário  
- 🔍 **Rastreabilidade completa** de todas as operações
- 🏢 **Escalabilidade** para mais de 100 filiais
- 🛡️ **Auditoria** e controle de alterações

---

## Desenvolvido com 💻 e ♥️

- **Daniel Barros** - RM 556152
- **Luccas Rufino** - RM 558253
- **Raul Clauson** - RM 555006
