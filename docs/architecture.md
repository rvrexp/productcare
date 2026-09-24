# ProductCare Architecture

## Overview

ProductCare uses a layered .NET backend with a separately deployed browser client.

The current frontend is standalone Blazor WebAssembly. It communicates with `ProductCare.Api` over HTTPS/JSON and has no direct dependency on backend implementation projects.

The backend structure follows the same separation I would expect in a conventional production .NET application: presentation, API boundary, application use cases, domain rules, external integrations, and specialized infrastructure are kept distinct.

AI-specific functionality is isolated in `ProductCare.AI` so model integration and orchestration do not leak into the rest of the application.

## Solution Structure

```text
src/
├── ProductCare.Web
├── ProductCare.Api
├── ProductCare.Application
├── ProductCare.Domain
├── ProductCare.Infrastructure
└── ProductCare.AI

tests/
├── ProductCare.UnitTests
└── ProductCare.IntegrationTests
```

## Dependency Direction

The backend project references are:

```text
ProductCare.Api
   ├── ProductCare.Application
   ├── ProductCare.Infrastructure
   └── ProductCare.AI

ProductCare.Infrastructure
   ├── ProductCare.Application
   └── ProductCare.Domain

ProductCare.AI
   ├── ProductCare.Application
   └── ProductCare.Domain

ProductCare.Application
   └── ProductCare.Domain
```

`ProductCare.Web` has no project reference to the backend layers.

Its dependency is the HTTP API:

```text
ProductCare.Web
       |
       | HTTPS / JSON
       v
ProductCare.Api
```

## ProductCare.Web

`ProductCare.Web` is the current presentation layer.

Responsibilities:

- render the user interface
- authenticate users through Microsoft Entra External ID
- acquire access tokens for ProductCare.Api
- call versioned API endpoints
- maintain presentation-specific state

It does not:

- access the database directly
- call Foundry directly
- call Azure AI Search directly
- contain privileged Azure credentials
- make authoritative authorization decisions

The client can use authentication state to control the UI, but the API remains responsible for enforcing access.

## ProductCare.Api

`ProductCare.Api` is the external application boundary.

Responsibilities:

- validate access tokens
- enforce authorization
- validate requests
- enforce resource ownership
- enforce usage limits
- expose versioned HTTP contracts
- invoke application use cases
- coordinate backend dependencies

Protected endpoints require an authenticated identity and the `access_as_user` delegated scope unless explicitly marked anonymous.

Health endpoints are intentionally anonymous so they can be used by infrastructure probes.

## ProductCare.Application

`ProductCare.Application` contains application use cases and the abstractions required to execute them.

It coordinates domain behavior without depending on concrete infrastructure or presentation technology.

It should not depend on:

- Blazor
- API controllers
- database implementations
- Azure SDK implementations
- Foundry-specific implementations

## ProductCare.Domain

`ProductCare.Domain` contains the core business model and domain rules.

It should remain independent of:

- ASP.NET Core
- Blazor
- Azure
- databases
- Foundry
- external APIs

Architecture tests are used to protect this dependency boundary.

## ProductCare.Infrastructure

`ProductCare.Infrastructure` contains non-AI integrations and infrastructure implementations.

Expected responsibilities include:

- persistence
- CPSC integration
- document storage
- external product data
- infrastructure-specific configuration

Infrastructure implementations satisfy interfaces required by the application layer.

The UI does not access Infrastructure directly.

## ProductCare.AI

`ProductCare.AI` contains AI-specific implementations.

Planned responsibilities include:

- product identification
- vision model integration
- structured model outputs
- retrieval and grounding
- specialist agents
- tool execution
- execution limits
- AI tracing
- evaluation hooks

The goal of keeping this separate is not to create another architectural layer for its own sake. It keeps model-specific implementation details from spreading across the API, application, and infrastructure projects.

The Foundry-specific implementation is being added incrementally as part of this project. The architecture leaves room to change models or AI-service implementations without changing the rest of the application.

## Authentication

The current flow is:

```text
User
  |
  v
ProductCare.Web
  |
  | Authorization Code + PKCE
  v
Microsoft Entra External ID
  |
  | access token
  v
ProductCare.Web
  |
  | Authorization: Bearer <token>
  v
ProductCare.Api
```

`ProductCare.Web` is a public OAuth client and does not contain a client secret.

`ProductCare.Api` validates the access token and the required delegated scope before executing protected requests.

## Replaceable Frontend

The backend does not depend on Blazor.

A future React or React Native client should be able to:

1. authenticate against the same identity provider
2. request the same ProductCare API permission
3. call the same versioned HTTP endpoints

Replacing the frontend should not require changes to ProductCare.Domain, ProductCare.Application, ProductCare.Infrastructure, or ProductCare.AI.

## API Contract

`ProductCare.Api` is the contract between client applications and the backend.

Current conventions:

- versioned routes under `/api/v1`
- JSON request and response models
- standard HTTP status codes
- bearer-token authentication
- OpenAPI metadata

The API contract should remain usable by non-.NET clients.

## Error Handling

Unexpected API exceptions are handled centrally.

Internal exception details are logged by the backend.

Clients receive a safe problem response rather than stack traces or implementation details.

## Request Tracing

Each API request receives a correlation ID.

The correlation ID is included in logging context and returned to the caller.

As AI and Azure services are added, the same request context will be carried into application and AI telemetry where practical.

## Current Security Boundaries

The current implementation follows these rules:

- browser clients are not trusted for authorization
- secrets are not stored in Blazor WebAssembly
- privileged Azure calls remain behind ProductCare.Api
- API endpoints are protected by default
- ownership and business rules are enforced server-side
- uploaded data is treated as untrusted input
- AI execution will have explicit limits
- least-privilege Azure access is preferred

## Deployment Direction

The current target is a low-traffic authenticated demo environment.

The design favors:

- low idle cost
- independently replaceable frontend
- versioned API contracts
- managed identity where supported
- bounded AI usage
- request tracing
- straightforward deployment and rollback

The demo environment is not intended to represent production-scale infrastructure.