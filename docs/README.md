# AgroSolutions.Functions

> Azure Function serverless para coleta e processamento assíncrono de dados de sensores agrícolas, integrada ao Azure Service Bus, Elastic APM e API de Telemetria.

## 📋 Índice

- [Overview](#-overview)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Functions](#-functions)
  - [CollectSensorData](#collectsensordata)
  - [ProcessSensorData](#processsensordata)
- [Payload Esperado](#-payload-esperado)
- [Arquitetura](#-arquitetura)
- [Observabilidade](#-observabilidade)
- [Estrutura de Pastas](#-estrutura-de-pastas)
- [Configuração](#-configuração)
- [Quick Start](#-quick-start)
- [CI/CD](#-cicd)

---

## 🎯 Overview

A **AgroSolutions.Functions** é um projeto de Azure Functions serverless (Isolated Process) que processa eventos agrícolas de forma assíncrona através do Azure Service Bus.

### Para que serve?

- ✅ **Coleta Automática de Dados**: Timer trigger diário que consulta campos ativos e coleta dados climáticos reais via API Open-Meteo
- ✅ **Ingestão de Dados de Sensores**: Recebe medições de campo (umidade, temperatura, precipitação) via Service Bus e encaminha para a API de Telemetria
- ✅ **Processamento Assíncrono**: Desacopla ingestão de dados do fluxo principal da aplicação
- ✅ **Escalabilidade Automática**: Processa mensagens sob demanda com Azure Functions
- ✅ **Rastreabilidade**: Correlation ID propagado em toda cadeia
- ✅ **Distributed Tracing**: Integração com Elastic APM para traces end-to-end via W3C Trace Context
- ✅ **Observabilidade**: Logs estruturados com Serilog + Elasticsearch
- ✅ **Resiliência**: Dead Letter Queue para falhas de deserialização/validação e retry automático para rate limiting (HTTP 429)

---

## 🛠️ Tecnologias Utilizadas

### Core Framework
- **.NET 10** - Framework principal
- **C# 14** - Linguagem de programação
- **Azure Functions v4** - Serverless compute (Isolated Process)

### Azure Services
- **Azure Service Bus** - Message broker assíncrono (Queue trigger)
- **Application Insights** - Telemetria e monitoramento

### Observabilidade
- **Elastic APM** - Application Performance Monitoring e Distributed Tracing
- **Serilog** - Logging estruturado
  - Console Sink
  - Elasticsearch Sink
- **Custom Enrichers**:
  - `CorrelationIdEnricher` - Adiciona CorrelationId a cada log event
  - `ServiceInfoEnricher` - Adiciona ServiceName e Environment

### Integrações
- **API de Fields** (`aks-agro-fields`) - Retorna campos ativos com latitude/longitude
- **Open-Meteo API** - API pública de dados climáticos (temperatura, precipitação)
- **API de Telemetria** (`aks-agro-telemetry`) - Recebe dados de sensores via HTTP POST

---

## ⚡ Functions

### CollectSensorData

Coleta diária automática de dados climáticos para todos os campos ativos, publicando mensagens na fila para processamento posterior.

**Trigger:** `TimerTrigger("0 0 6 * * *")` — Executa diariamente às 06:00 UTC

**Output:** `ServiceBusOutput("sensor-data-received-queue")`

#### Fluxo de Processamento

```
Timer trigger (06:00 UTC)
     │
     ▼
Inicia Transaction no Elastic APM
     │
     ▼
[Span] Fetch Active Fields
     │   GET → API de Fields (com Bearer token)
     │   Filtra apenas campos com isActive = true
     │   ❌ Sem campos ativos → retorna vazio
     │
     ▼
[Span] Collect Weather Data for Fields
     │   Para cada campo ativo:
     │     ├─ GET → Open-Meteo API (latitude/longitude do campo)
     │     │   Obtém: AirTemperature + Precipitation
     │     │   ❌ Falha → usa dados mockados (25.5°C / 2.3mm)
     │     ├─ Mock: SoilMoisture (20-80% aleatório)
     │     └─ Serializa SensorDataRequest
     │
     ▼
Publica mensagem(s) na fila via output binding ✅
(uma mensagem por campo ativo)
```

### ProcessSensorData

Consome mensagens da fila `sensor-data-received-queue`, valida os dados de sensores e encaminha para a API de Telemetria.

**Trigger:** `ServiceBusTrigger("sensor-data-received-queue")`

#### Fluxo de Processamento

```
Mensagem recebida
     │
     ▼
Extrai TracingContext (CorrelationId + Traceparent)
     │
     ▼
Inicia Transaction no Elastic APM
     │
     ▼
[Span] Parse Service Bus Message
     │   Deserializa JSON → SensorDataRequest
     │   ❌ Falha → Dead Letter (DeserializationError)
     │
     ▼
Validação de campos
     │   ❌ Falha → Dead Letter (ValidationError)
     │
     ▼
[Span] Send Sensor Data to Telemetry API
     │   POST → API de Telemetria (com Bearer token)
     │   Headers: x-correlation-id (manual) + traceparent (auto via APM agent)
     │
     ▼
Complete message ✅
```

---

## 📦 Payload Esperado

### SensorDataRequest (fila `sensor-data-received-queue`)

```json
{
  "fieldId": 42,
  "soilMoisture": 35.5,
  "airTemperature": 28.3,
  "precipitation": 12.0,
  "collectedAt": "2025-01-15T10:30:00Z",
  "alertEmailTo": "agronomist@farm.com"
}
```

### Application Properties da Mensagem (opcionais)

| Property | Descrição |
|----------|-----------|
| `CorrelationId` | ID de correlação (fallback se `message.CorrelationId` estiver vazio) |
| `traceparent` | W3C Trace Context para distributed tracing |

---

## 🏗️ Arquitetura

### Arquitetura High-Level

```
┌─────────────────────────────────────────────────────────────┐
│                    COLETA DE DADOS (Timer)                   │
└─────────────────────────────────────────────────────────────┘

  Timer (06:00 UTC diário)
           │
           ▼
┌──────────────────────────────────┐
│  CollectSensorDataFunction        │
│  ├─ ApiClientService              │ → GET Fields API (campos ativos)
│  ├─ ApiClientService              │ → GET Open-Meteo (temperatura, precipitação)
│  ├─ Mock SoilMoisture             │ → Gera valor aleatório (20-80%)
│  └─ ServiceBusOutput              │ → Publica na fila
└──────────┬───────────────────────┘
           │ 1 mensagem por campo ativo
           ▼
┌──────────────────────────────────┐
│  Azure Service Bus Queue          │
│  "sensor-data-received-queue"     │
└──────────┬───────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                 PROCESSAMENTO DE DADOS (Queue)               │
└─────────────────────────────────────────────────────────────┘

           │ Service Bus Trigger
           ▼
┌──────────────────────────────────┐
│  ProcessSensorDataFunction        │
│  ├─ MessageTracingService         │ → Extrai CorrelationId + Traceparent
│  ├─ Parse & Validate              │ → Deserializa e valida payload
│  └─ ApiClientService              │ → POST para API de Telemetria
└──────────┬───────────────────────┘
           │ HTTP POST (Bearer token)
           │ Headers: x-correlation-id, traceparent (auto)
           ▼
┌──────────────────────────────────┐
│  API de Telemetria                │
│  (aks-agro-telemetry)             │
│  └─ Persiste dados de sensores    │
└──────────────────────────────────┘

  ┌─────────────────────────────┐
  │  Elastic APM                │ ◄── Transactions, Spans & Distributed Traces
  │  Elasticsearch              │ ◄── Structured Logs (Serilog)
  │  Application Insights       │ ◄── Telemetria Azure
  └─────────────────────────────┘
```

### Padrões Aplicados

- ✅ **Dependency Injection** - IoC container nativo do .NET
- ✅ **Interface Segregation** - `IApiClientService`, `IMessageTracingService`
- ✅ **Single Responsibility** - Cada serviço com responsabilidade clara
- ✅ **Typed HttpClient** - `AddHttpClient<IApiClientService, ApiClientService>()`

---

## 📁 Estrutura de Pastas

```
AgroSolutions.Functions/
│
├── Functions/                  # Azure Functions (entry points)
│   ├── CollectSensorDataFunction.cs    # Timer Trigger - Coleta diária de dados climáticos
│   └── ProcessSensorDataFunction.cs    # Service Bus Trigger - Processamento de dados de sensores
│
├── Interfaces/                 # Contratos e abstrações
│   ├── IApiClientService.cs            # Interface do client HTTP
│   └── IMessageTracingService.cs       # Interface de extração de tracing
│
├── Logging/                    # Logging estruturado e enrichers
│   ├── CorrelationIdEnricher.cs        # Adiciona CorrelationId aos logs
│   └── ServiceInfoEnricher.cs          # Adiciona ServiceName e Environment
│
├── Models/                     # DTOs e modelos
│   ├── FieldResponse.cs                # DTO de resposta da API de Fields
│   ├── OpenMeteoResponse.cs            # DTO de resposta da API Open-Meteo
│   ├── SensorDataRequest.cs            # DTO de dados de sensores
│   └── TracingContext.cs               # Contexto de tracing (CorrelationId + Traceparent)
│
├── Services/                   # Implementações de serviços
│   ├── ApiClientService.cs             # Client HTTP genérico com suporte a tracing
│   └── MessageTracingService.cs        # Extrai tracing context de mensagens Service Bus
│
├── docs/                       # Documentação
│   └── README.md
│
├── host.json                   # Configuração do Azure Functions host
├── local.settings.json         # Configurações locais (não versionado)
└── Program.cs                  # Entry point, DI e configuração de Serilog/APM
```

---

## ⚙️ Configuração

### Variáveis de Configuração

#### Service Bus
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `ServiceBusConnection` | Connection string do Azure Service Bus | ✅ |

#### API de Telemetria
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `TelemetryApi:Url` | URL da API de Telemetria | ✅ |
| `TelemetryApi:Token` | Bearer token para autenticação | ✅ |

#### API de Fields
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `FieldsApi:Url` | URL da API de Fields (ex: `https://host/v1/fields`) | ✅ |
| `FieldsApi:Token` | Bearer token para autenticação | ✅ |

#### CollectSensorData
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `CollectSensorData:AlertEmailTo` | Email de alerta para as medições coletadas | ✅ |
| `CollectSensorData:ServiceNames` | Service name no Elastic APM (default: `func-agro-collect-data`) | ❌ |

#### ProcessSensorData
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `ProcessSensorData:ServiceName` | Service name no Elastic APM (default: `func-agro-process-data`) | ❌ |

#### Elastic APM
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `ElasticApm:Enabled` | Habilita Elastic APM (`true`/`false`) | ❌ |
| `ElasticApm:ServerUrl` | URL do servidor APM | ❌ |
| `ElasticApm:SecretToken` | Token de autenticação do APM | ❌ |
| `ElasticApm:ServiceName` | Nome do serviço no APM (default: `func-agro`) | ❌ |
| `ElasticApm:Environment` | Ambiente no APM (default: `Development`) | ❌ |

#### Elastic Logs (Serilog → Elasticsearch)
| Chave | Descrição | Obrigatório |
|-------|-----------|:-----------:|
| `ElasticLogs:Endpoint` | URL do Elasticsearch | ❌ |
| `ElasticLogs:ApiKey` | API Key do Elasticsearch | ❌ |
| `ElasticLogs:IndexPrefix` | Prefixo do índice (default: `agro`) | ❌ |

### Exemplo `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;...",
    "TelemetryApi:Url": "https://aks-agro-telemetry.yourdomain.com/v1/field-measurements",
    "TelemetryApi:Token": "your-bearer-token",
    "FieldsApi:Url": "https://aks-agro-fields.yourdomain.com/v1/fields",
    "FieldsApi:Token": "your-bearer-token",
    "CollectSensorData:AlertEmailTo": "agronomist@farm.com",
    "CollectSensorData:ServiceNames": "func-agro-collect-data",
    "ProcessSensorData:ServiceName": "func-agro-process-data",
    "ElasticApm:Enabled": "true",
    "ElasticApm:ServerUrl": "https://your-apm.elastic-cloud.com",
    "ElasticApm:SecretToken": "your-secret-token",
    "ElasticApm:ServiceName": "func-agro",
    "ElasticApm:Environment": "Development",
    "ElasticLogs:Endpoint": "https://your-es.elastic-cloud.com",
    "ElasticLogs:ApiKey": "your-api-key",
    "ElasticLogs:IndexPrefix": "agro"
  }
}
```

---

## ⚡ Quick Start

### 1️⃣ Pré-requisitos

```bash
dotnet --version    # .NET 10
func --version      # Azure Functions Core Tools v4
```

### 2️⃣ Clonar e Configurar

```bash
git clone https://github.com/fkwesley/AgroSolutions.Functions.git
cd AgroSolutions.Functions

# Configurar local.settings.json com suas credenciais
```

### 3️⃣ Executar Localmente

```bash
# Via CLI
func start

# Via Visual Studio
Pressione F5
```

### 4️⃣ Testar

Publique uma mensagem na fila `sensor-data-received-queue` do Service Bus com o payload JSON descrito na seção [Payload Esperado](#-payload-esperado).

---

## 🚀 CI/CD

| Environment | Branch | Auto-Deploy |
|-------------|--------|:-----------:|
| **Development** | `develop` | ✅ |
| **Staging** | `release/*` | ✅ |
| **Production** | `main` | ⚠️ Manual approval |

### Checklist de Deploy

- ✅ Build sem warnings
- ✅ Configurações de ambiente corretas (`ServiceBusConnection`, `TelemetryApi:*`)
- ✅ Elastic APM configurado
- ✅ Application Insights habilitado
- ✅ Dead Letter Queue configurada no Service Bus

---

## ✍️ Autor
- Frank Vieira
- GitHub: @fkwesley
