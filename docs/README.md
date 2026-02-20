# Common.Notifications.Function

> Azure Function serverless para processamento assíncrono de notificações por email com templates HTML profissionais, integrada ao Azure Service Bus e Azure Communication Services.

## 📋 Índice

- [Overview](#-overview)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Payload Esperado](#-payload-esperado)
- [Arquitetura](#-arquitetura)
- [Estrutura de Pastas](#-estrutura-de-pastas)
- [Extensibilidade](#-extensibilidade)
- [Testes](#-testes)
- [CI/CD](#-cicd)
- [Quick Start](#-quick-start)

---

## 🎯 Overview

A **Common.Notifications.Function** é uma Azure Function serverless que processa notificações de email de forma assíncrona através do Azure Service Bus.

### Para que serve?

- ✅ **Processamento Assíncrono**: Desacopla envio de emails do fluxo principal da aplicação
- ✅ **Escalabilidade Automática**: Processa mensagens sob demanda com Azure Functions
- ✅ **Emails via Templates**: Templates HTML customizáveis pré-configurados
- ✅ **Rastreabilidade**: Correlation ID em toda cadeia de processamento
- ✅ **Observabilidade**: Integração com Elastic APM e logs estruturados (Serilog)
- ✅ **Resiliência**: Dead Letter Queue automática para falhas
- ✅ **Type-Safe**: Validação forte de tipos com enums e DTOs

---

## 🛠️ Tecnologias Utilizadas

### Core Framework
- **.NET 10** - Framework principal (LTS)
- **C# 13** - Linguagem de programação
- **Azure Functions v4** - Serverless compute platform

### Azure Services
- **Azure Service Bus** - Message broker assíncrono (Queue trigger)
- **Azure Communication Services** - Email delivery provider
- **Application Insights** - Telemetria e monitoramento

### Observabilidade
- **Elastic APM** - Application Performance Monitoring
- **Serilog** - Logging estruturado
  - Console Sink
  - Elasticsearch Sink
- **Custom Enrichers** - CorrelationId, ServiceInfo, ThreadId, MachineName

---

## 📦 Payload Esperado

### Estrutura do NotificationRequest

```json
{
  "TemplateId": "Drought",
  "EmailTo": [
    "agronomist@farm.com",
    "manager@farm.com"
  ],
  "EmailCc": [
    "supervisor@farm.com"
  ],
  "EmailBcc": [
    "audit@company.com"
  ],
  "Parameters": {  //dinâmicos para cada template
    "{fieldId}": "42",
    "{severity}": "Alta",
    "{recommendations}": "Iniciar irrigação imediatamente"
  },
  "Metadata": {
    "CorrelationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Severity": "High"
  }
}
```

---

## 🏗️ Arquitetura

### Arquitetura High-Level

```
┌─────────────────┐
│   Producer      │ (API, Worker, etc.)
│   Application   │
└────────┬────────┘
         │ Publica mensagem
         ▼
┌─────────────────────────────────┐
│   Azure Service Bus Queue       │
│   "notifications-queue"          │
└────────┬────────────────────────┘
         │ Service Bus Trigger
         ▼
┌─────────────────────────────────┐
│  NotificationFunction            │
│  • Deserializa payload           │
│  • Extrai CorrelationId          │
│  • Valida requisição             │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  EmailTemplateService            │
│  • Orquestra processamento       │
│  • Seleciona template            │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  EmailTemplateFactory            │
│  • Cria instância do template    │
│  • Factory Pattern               │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  Template Específico             │
│  (DroughtTemplate, etc.)         │
│  • Gera Subject                  │
│  • Gera HTML Body                │
│  • Substitui parâmetros          │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  AzureCommunicationEmailService  │
│  • Envia email                   │
│  • Retry automático              │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  Azure Communication Services    │
│  • Email Delivery                │
└─────────────────────────────────┘

      ┌───────────────────────┐
      │  Elastic APM          │ ◄─── Traces & Metrics
      │  Elasticsearch Logs   │ ◄─── Structured Logs
      └───────────────────────┘
```

### Padrões Arquiteturais Aplicados

- ✅ **SOLID Principles** - Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- ✅ **Factory Pattern** - `EmailTemplateFactory` cria templates dinamicamente
- ✅ **Template Method Pattern** - `EmailTemplateBase` define estrutura comum
- ✅ **Strategy Pattern** - Cada template é uma estratégia de geração
- ✅ **Dependency Injection** - IoC container nativo do .NET
- ✅ **Domain-Driven Design (DDD)** - Modelos ricos, value objects, entities
- ✅ **Clean Architecture** - Separação de camadas (Domain, Application, Infrastructure)

---

## 📁 Estrutura de Pastas

```
Common.Notifications.Function/
│
├── Configuration/              # Configurações e setup do projeto
│   └── EmailServiceConfiguration.cs   # Extension methods para DI
│
├── Functions/                  # Azure Functions (entry points)
│   ├── NotificationFunction.cs         # Service Bus Trigger principal
│   └── NotificationFunctionTests.cs    # Function de teste HTTP
│
├── Interfaces/                 # Contratos e abstrações
│   ├── IEmailService.cs                # Interface do serviço de email
│   └── IEmailTemplateService.cs        # Interface de templates
│
├── Logging/                    # Logging estruturado e enrichers
│   ├── CorrelationIdEnricher.cs        # Adiciona CorrelationId aos logs
│   └── ServiceInfoEnricher.cs          # Adiciona metadados do serviço
│
├── Models/                     # DTOs e modelos de domínio
│   ├── AlertMetadata.cs                # Metadados de alertas
│   ├── EmailMessage.cs                 # Modelo de mensagem de email
│   ├── EmailSendResult.cs              # Resultado de envio
│   ├── EmailTemplateType.cs            # Enum de tipos de template
│   └── NotificationRequest.cs          # DTO de requisição
│
├── Services/                   # Implementações de serviços
│   ├── AzureCommunicationEmailService.cs    # Integração com Azure Communication Services
│   └── EmailTemplateService.cs              # Orquestração de templates
│
├── Templates/                  # Factory e templates de email
│   ├── EmailTemplateBase.cs            # Classe base abstrata
│   ├── EmailTemplateFactory.cs         # Factory pattern
│   ├── IEmailTemplate.cs               # Interface de templates
│   ├── DroughtTemplate.cs              # Template de seca
│   ├── ExcessiveRainfallTemplate.cs    # Template de chuva
│   ├── ExtremeHeatTemplate.cs          # Template de calor
│   ├── FreezingTemperatureTemplate.cs  # Template de geada
│   ├── HeatStressTemplate.cs           # Template de estresse térmico
│   ├── IrrigationTemplate.cs           # Template de irrigação
│   └── PestRiskTemplate.cs             # Template de pragas
│
├── docs/                       # Documentação completa
├── tests/                      # Testes unitários e integração
├── host.json                   # Configuração do Azure Functions host
├── local.settings.json         # Configurações locais (não versionado)
└── Program.cs                  # Entry point e configuração de DI

```

---

## 🔧 Extensibilidade

### 🆕 Como Adicionar um Novo Template

**3 Passos Simples:**

#### 1️⃣ Adicionar ao Enum `EmailTemplateType`

```csharp
// Models/EmailTemplateType.cs
public enum EmailTemplateType
{
    // ... templates existentes

    [Display(Name = "Fertilization")]
    Fertilization
}
```

#### 2️⃣ Criar Classe do Template

```csharp
// Templates/FertilizationTemplate.cs
namespace Common.Notifications.Function.Templates;

public class FertilizationTemplate : EmailTemplateBase
{
    protected override string GetSubject(Dictionary<string, string> parameters)
    {
        return "🌱 Recomendação de Fertilização";
    }

    protected override string GetHtmlBody(Dictionary<string, string> parameters)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: Arial, sans-serif; }}
                .header {{ background: #28a745; color: white; padding: 20px; }}
                .content {{ padding: 20px; }}
            </style>
        </head>
        <body>
            <div class='header'>
                <h1>🌱 Recomendação de Fertilização</h1>
            </div>
            <div class='content'>
                <p>Talhão: <strong>{parameters.GetValueOrDefault("{fieldId}", "N/A")}</strong></p>
                <p>Cultura: <strong>{parameters.GetValueOrDefault("{cropType}", "N/A")}</strong></p>
                <p>Nutriente Recomendado: <strong>{parameters.GetValueOrDefault("{nutrient}", "N/A")}</strong></p>
                <p>Dosagem: <strong>{parameters.GetValueOrDefault("{dosage}", "N/A")} kg/ha</strong></p>
            </div>
        </body>
        </html>";
    }
}
```

#### 3️⃣ Registrar no Factory

```csharp
// Templates/EmailTemplateFactory.cs
public class EmailTemplateFactory
{
    public IEmailTemplate CreateTemplate(string templateId)
    {
        return templateId switch
        {
            // ... templates existentes

            nameof(EmailTemplateType.Fertilization) => new FertilizationTemplate(),

            _ => throw new ArgumentException($"Template '{templateId}' não encontrado.")
        };
    }
}
```

**Pronto! ✅** Seu novo template está disponível.

---

## 🧪 Testes

### Estrutura de Testes

```
tests/
└── UnitTests/
    ├── Services/
    │   ├── EmailTemplateServiceTests.cs
    │   └── AzureCommunicationEmailServiceTests.cs
    ├── Templates/
    │   ├── DroughtTemplateTests.cs
    │   ├── ExcessiveRainfallTemplateTests.cs
    │   └── EmailTemplateFactoryTests.cs
    ├── Functions/
    │   └── NotificationFunctionTests.cs
    └── UnitTests.csproj
```

### Executar Testes

```bash
# Via CLI
dotnet test

# Via Visual Studio
Test Explorer → Run All

# Com Coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 🚀 CI/CD

### Pipeline Recomendado (Azure DevOps / GitHub Actions)

| Environment | Branch | Auto-Deploy |
|-------------|--------|-------------|
| **Development** | `develop` | ✅ Sim |
| **Staging** | `release/*` | ✅ Sim |
| **Production** | `main` | ⚠️ Manual approval |

### Checklist de Deploy

- ✅ Testes unitários passando
- ✅ Build sem warnings
- ✅ Configurações de ambiente corretas (`ServiceBusConnection`, `AzureCommunicationServices:ConnectionString`)
- ✅ Elastic APM configurado
- ✅ Application Insights habilitado
- ✅ Dead Letter Queue configurada

---

## ⚡ Quick Start

### 1️⃣ Pré-requisitos

```bash
# Verificar instalações
dotnet --version    # Deve ser .NET 10
func --version      # Deve ser Azure Functions Core Tools v4
```

### 2️⃣ Clonar e Configurar

```bash
git clone https://github.com/fkwesley/Common.Notifications.Function.git
cd Common.Notifications.Function

# Copiar template de configuração
cp local.settings.template.json local.settings.json
```

### 3️⃣ Configurar `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;...",
    "AzureCommunicationServices:ConnectionString": "endpoint=https://your-acs.communication.azure.com/;...",
    "AzureCommunicationServices:SenderEmail": "noreply@yourapp.com",
    "ElasticApm:Enabled": "true",
    "ElasticApm:ServerUrl": "http://localhost:8200",
    "ElasticApm:ServiceName": "func-notifications",
    "ElasticApm:Environment": "Development"
  }
}
```

### 4️⃣ Executar Localmente

```bash
# Via CLI
func start

# Via Visual Studio
Pressione F5
```

---

 ## ✍️ Autor
- Frank Vieira
- GitHub: @fkwesley
