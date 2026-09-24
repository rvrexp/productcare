# ProductCare AI

ProductCare is an AI-assisted product support application built around a conventional .NET application architecture.

The project combines product identification, recall checking, documentation-grounded Q&A, maintenance support, and agent-based AI workflows.

The primary engineering goal is to integrate modern AI capabilities into a backend architecture with clear boundaries, explicit security controls, testable contracts, and a replaceable client.

The AI-specific parts of the project are also providing hands-on experience with Microsoft Foundry, retrieval, evaluation, and agent orchestration. Those areas are being implemented incrementally rather than treated as production-proven capabilities from the start.

## Architecture

```text
Blazor WebAssembly
        |
        | HTTPS / JSON
        | Bearer access token
        v
ASP.NET Core API
        |
        v
Application
      /     \
Infrastructure  AI
        \     /
         Domain
```

`ProductCare.Web` is a standalone Blazor WebAssembly client.

It communicates with `ProductCare.Api` over HTTP and does not reference backend implementation projects directly.

The backend is intentionally independent of Blazor. A future React, React Native, or other client should be able to use the same API without changing the Domain, Application, Infrastructure, or AI layers.

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

docs/
└── adr/

infra/
```

## Current Stack

- .NET 10
- ASP.NET Core Web API
- standalone Blazor WebAssembly
- Microsoft Entra External ID
- Microsoft Identity Web
- Microsoft Authentication WebAssembly MSAL
- xUnit

Planned Azure and AI components include:

- Microsoft Foundry
- vision-capable models
- Azure AI Search
- Application Insights
- managed identity and Azure RBAC
- infrastructure as code

## Authentication

ProductCare uses Microsoft Entra External ID for customer authentication.

The Blazor WebAssembly client uses Authorization Code with PKCE and requests the delegated `access_as_user` scope for `ProductCare.Api`.

The API validates the bearer token and required scope independently.

Frontend authentication state is used for presentation purposes only. Authorization decisions remain in the API.

## Current Status

Implemented:

- solution and project boundaries
- ASP.NET Core API foundation
- centralized exception handling
- correlation IDs
- liveness and readiness endpoints
- Microsoft Entra External ID setup
- delegated `access_as_user` API scope
- standalone Blazor WebAssembly client
- Authorization Code with PKCE
- authenticated client-to-API calls
- development CORS restricted to the configured frontend origin
- unit and integration test projects

## Next Work

The next implementation steps are:

1. identify a product from a model-label image
2. persist product information and enforce user ownership
3. integrate CPSC recall data
4. ingest and retrieve product documentation
5. add grounded Q&A with citations
6. add specialist-agent orchestration
7. add evaluation, safety controls, and tracing
8. deploy the demo environment to Azure

## Frontend Decision

The initial client prototype used React Native and Expo.

For the current portfolio milestone, I switched to standalone Blazor WebAssembly to keep the project within scope and concentrate the implementation effort on the API, AI integration, retrieval, evaluation, and operational concerns.

`ProductCare.Web` remains an API client. The backend is not coupled to Blazor and can support a different client later without being redesigned.