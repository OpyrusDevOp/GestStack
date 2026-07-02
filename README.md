# GestStack

GestStack is an ERP system covering Procurement, Inventory, and Finance. It is built as a portfolio project to demonstrate software engineering practices: clean architecture, domain-driven design, and a tested, maintainable .NET codebase.

## Modules

- **Procurement** — supplier management, purchase orders, and receiving.
- **Inventory** — stock tracking, warehouses, and stock movements.
- **Finance** — accounts, invoicing, and payments.

## Architecture

The solution follows a clean architecture layout. Dependencies point inward: the Domain layer has no dependencies, and outer layers depend on inner ones.

| Project | Role |
|---|---|
| `GestStack.Domain` | Entities, value objects, and business rules. No external dependencies. |
| `GestStack.Application` | Use cases and application services, orchestrating the domain. |
| `GestStack.Infrastructure` | Persistence and external service implementations. |
| `GestStack.API` | ASP.NET Core Web API exposing the application over HTTP. |
| `GestStack.DesktopClient` | Avalonia desktop client consuming the API. |
| `GestStack.Tests.Unit` | Unit tests for the Domain and Application layers. |

## Tech stack

- .NET 10 / C#
- ASP.NET Core (Web API)
- Avalonia 12 (cross-platform desktop UI)
- xUnit (testing)

## Getting started

Prerequisites: .NET 10 SDK.

```bash
# Build the whole solution
dotnet build

# Run the API
dotnet run --project GestStack.API

# Run the desktop client
dotnet run --project GestStack.DesktopClient

# Run the tests
dotnet test
```

## Status

Early development. The solution structure is in place; domain modules are being implemented.
