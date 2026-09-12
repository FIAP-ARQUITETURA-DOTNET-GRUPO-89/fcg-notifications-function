# 🚀 FcgNotifications API

Microsserviço de notificação da plataforma **FCG Games**. Este serviço é responsável pelo processamento assíncrono de eventos e entrega de notificações aos usuários. Ele atua como um consumidor de eventos críticos do sistema, garantindo que o usuário seja mantido informado sobre suas atividades na plataforma.

---

# 📑 Sumário

- [👁 Visão Geral](#-visão-geral)
- [📋 Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [🏛 Arquitetura](#-arquitetura)
- [📁 Estrutura da Solução](#-estrutura-da-solução)
- [🔄 Fluxo de Eventos (Mensageria)](#-fluxo-de-eventos-mensageria)
- [⚙️ Pré-requisitos](#️-pré-requisitos)
- [▶️ Executando Localmente](#️-executando-localmente)
- [🗄 Banco de Dados](#-banco-de-dados)
- [🧪 Testes](#-testes)
- [🌍 Variáveis de Ambiente](#-variáveis-de-ambiente)
- [📄 Licença](#-licença)

---

# 👁 Visão Geral

O **FcgNotifications API** é um serviço orientado a eventos (**Event-Driven**) projetado para alta escalabilidade e resiliência.

Sua principal responsabilidade é escutar filas do **RabbitMQ**, processar regras de negócio relacionadas a notificações e persistir o histórico de comunicações enviadas.

Utiliza o padrão **Worker Service** para processamento em background, garantindo que o sistema seja robusto, desacoplado e altamente resiliente.

---

# 📋 Tecnologias Utilizadas

| Categoria | Tecnologia |
|-----------|------------|
| Runtime | .NET 10 |
| Padrão | Worker Service / Background Tasks |
| Orquestração | .NET Aspire (Local e Cloud Ready) |
| Mensageria | MassTransit + RabbitMQ |
| Persistência | Entity Framework Core + PostgreSQL |
| Mediação | MediatR + CQRS + OperationResult |
| Testes | xUnit, Shouldly, NSubstitute, Respawn e Testcontainers |

---

# 🏛 Arquitetura

O projeto segue os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**, organizando o código em camadas bem definidas.

| Camada | Responsabilidade |
|---------|------------------|
| **FcgNotifications.Worker** | Entry Point do Worker, Consumers e Extensions |
| **FcgNotifications.Application** | Casos de Uso, Commands, Handlers e Loggers |
| **FcgNotifications.Domain** | Agregados (Notification), Enums e Interfaces |
| **FcgNotifications.Infrastructure** | Repositórios, EF Core, Migrations e Messaging |
| **FcgNotifications.IoC** | Registro de Dependências |

---

# 📁 Estrutura da Solução

```text
src/
├── FcgNotifications.Worker          # Entry Point do serviço de background
├── FcgNotifications.Application     # Casos de uso e Handlers
├── FcgNotifications.Domain          # Entidades e regras de negócio
├── FcgNotifications.Infrastructure  # Persistência e Mensageria
├── FcgNotifications.IoC             # Injeção de Dependências
└── FcgNotifications.SharedKernel    # Código compartilhado

tests/
├── FcgNotifications.UnitTests         # Testes de Handlers e Domínio
└── FcgNotifications.IntegrationTests  # Testes de Consumers (Integration)
```

---

# 🔄 Fluxo de Eventos (Mensageria)

A API atua como um consumidor de eventos, processando mensagens enviadas por outros microsserviços da plataforma.

## Eventos Consumidos

- `UserCreatedEvent` *(via UserCreatedConsumer)*
- `PaymentProcessedEvent` *(via PaymentProcessedConsumer)*

## Broker

- RabbitMQ

## Resiliência

Cada Consumer possui **Retry Policy** configurada via **MassTransit**, utilizando **Exponential Backoff**, garantindo que falhas temporárias (como indisponibilidade momentânea do banco de dados) não resultem em perda de mensagens.

---

# ⚙️ Pré-requisitos

Antes de executar a aplicação, instale:

- .NET SDK 10.0 ou superior
- Docker Desktop
- Workload do .NET Aspire

## Instalação do Aspire

```bash
dotnet workload install aspire
```

---

# ▶️ Executando Localmente

O ambiente completo é provisionado automaticamente pelo **.NET Aspire**.

Execute:

```bash
dotnet run --project src/FcgNotifications.AppHost
```

Após iniciar a aplicação, o **Aspire Dashboard** será aberto automaticamente, exibindo:

- Logs da aplicação
- Métricas
- Status dos containers
- PostgreSQL
- RabbitMQ

---

# 🗄 Banco de Dados

A persistência utiliza **Entity Framework Core** com **PostgreSQL**.

O histórico de notificações é armazenado para fins de auditoria e consulta pelo usuário.

## Criando uma Migration

```bash
dotnet ef migrations add NomeDaMigration \
-p src/FcgNotifications.Infrastructure \
-s src/FcgNotifications.Worker
```

---

# 🧪 Testes

## Testes Unitários

Focados na lógica dos Handlers e regras de negócio do domínio.

```bash
dotnet test tests/FcgNotifications.UnitTests
```

---

## Testes de Integração

Validam o fluxo completo de mensageria:

- Envio do evento pelo Bus
- Processamento pelo Consumer
- Persistência no banco de dados real

Os testes utilizam:

- Testcontainers
- Respawn

para garantir isolamento entre execuções.

```bash
dotnet test tests/FcgNotifications.IntegrationTests
```

---

# 🌍 Variáveis de Ambiente

Como a aplicação utiliza **.NET Aspire**, a configuração é gerenciada automaticamente.

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings:Default` | String de conexão com PostgreSQL |
| `ConnectionStrings:rabbitmq` | Endereço do RabbitMQ |
| `MassTransit:RetryLimit` | Quantidade máxima de tentativas de reprocessamento (Padrão: 5) |

---

# 📄 Licença

Projeto desenvolvido para a plataforma **FCG Games**.
