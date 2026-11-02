# ProductCare Architecture

## Overview

ProductCare runs as a layered .NET backend with a separately deployed browser client.

The frontend is a standalone Blazor WebAssembly app that talks to `ProductCare.Api` over HTTPS. We treat the browser as an untrusted presentation layer: it has no direct project dependency on backend implementation layers and is never the place where authorization is finalized.

Backend responsibilities are split across API, application, domain, infrastructure, and AI projects. That separation keeps Azure SDK and model-specific details out of business rules, which makes day-to-day changes safer and easier to review.

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

Backend project references are:

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

`ProductCare.Web` has no project reference to the backend layers. Its dependency is the HTTP API:

```text
ProductCare.Web
       |
       | HTTPS / JSON
       v
ProductCare.Api
```

Architecture tests enforce this dependency direction so layering drift is caught in CI rather than during incident response.

## ProductCare.Web

`ProductCare.Web` is the presentation layer and should stay thin.

Responsibilities:

- render the user interface;
- authenticate users through Microsoft Entra External ID;
- acquire access tokens for ProductCare.Api;
- call versioned API endpoints;
- maintain presentation-specific state.

It does not:

- access SQL Server directly;
- call Microsoft Foundry directly;
- call Azure AI Search directly;
- access Blob Storage directly;
- contain privileged Azure credentials;
- make authoritative authorization decisions.

Authentication state can influence UI visibility, but all real access decisions are enforced by the API.

## ProductCare.Api

`ProductCare.Api` is the public application boundary and the primary security boundary.

Responsibilities:

- validate customer access tokens;
- validate Event Grid delivery tokens;
- enforce authorization policies;
- validate requests;
- enforce resource ownership;
- enforce usage controls;
- expose versioned HTTP contracts;
- coordinate application use cases and integrations;
- provide health endpoints and request telemetry.

Customer endpoints require an authenticated identity and the delegated `access_as_user` scope unless explicitly designed otherwise.

Health endpoints are intentionally anonymous for platform probes.

The internal Event Grid webhook is intentionally non-anonymous. It runs on a dedicated JWT bearer scheme and policy, fully separated from customer-authenticated traffic.

## ProductCare.Application

`ProductCare.Application` contains application use cases and the abstractions required to execute them.

It coordinates domain behavior without depending on concrete infrastructure or presentation technology.

It does not depend on:

- Blazor;
- API controllers;
- concrete database implementations;
- Azure SDK implementations;
- Foundry-specific implementations.

## ProductCare.Domain

`ProductCare.Domain` contains the core business model and domain rules.

It remains independent of:

- ASP.NET Core;
- Blazor;
- Azure;
- databases;
- Foundry;
- external APIs.

## ProductCare.Infrastructure

`ProductCare.Infrastructure` contains non-AI infrastructure and external-service implementations.

Current responsibilities include:

- EF Core persistence;
- SQL Server repositories;
- CPSC recall integration;
- infrastructure-specific configuration.

Infrastructure implementations satisfy interfaces consumed by the application layer. The UI does not access Infrastructure directly.

## ProductCare.AI

`ProductCare.AI` contains AI and product-knowledge integrations.

Current responsibilities include:

- product identification;
- Foundry Responses API integration;
- structured model output handling;
- Azure AI Search retrieval;
- product-manual Blob Storage integration;
- Defender scan-result processing;
- lifecycle cleanup of Blob, Search, and Foundry product resources;
- grounded answer generation;
- assistant routing and tool orchestration;
- bounded tool execution;
- streaming;
- AI tracing.

Keeping these implementations in a dedicated project limits the blast radius when model behavior, SDK APIs, or managed service contracts change.

## Customer Authentication Flow

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

`ProductCare.Web` is a public OAuth client and contains no client secret.

`ProductCare.Api` validates the token and delegated scope before executing protected customer requests.

Application authorization then applies ProductCare-specific access state and roles.

A valid Entra token alone is not enough for active ProductCare use. Endpoints guarded by `ProductCare.ActiveUser` also evaluate current ProductCare account state, so moving an account to `Disabled` cuts off feature access even if previously issued tokens have not yet expired.

## Event Grid Authentication Flow

Defender for Storage scan results are delivered through an Event Grid custom topic.

```text
Defender for Storage
       |
       | malware scan result
       v
Event Grid custom topic
       |
       | system-assigned managed identity
       | Microsoft Entra bearer token
       v
ProductCare.Api
       |
       | EventGridAuthentication policy
       v
ProductManualSecurityEventsController
```

The Event Grid webhook uses a dedicated Microsoft Entra application registration as the token audience.

The API validates:

- token signature and lifetime;
- configured tenant issuer;
- configured audience;
- the `oid` claim against the Event Grid custom topic's system-assigned managed identity object ID.

This closes a common multi-tenant failure mode: a valid token from the same tenant but from the wrong application or managed identity is still rejected.

The webhook does not use a shared query-string token.

## Product Manual Security Boundary

Product manuals are treated as untrusted input.

The upload and indexing flow is:

```text
Client
  |
  | validated PDF
  v
ProductCare.Api
  |
  | upload with AzureSearch_Skip=true
  v
Private Blob container
  |
  v
Defender for Storage
  |
  | scan result
  v
Event Grid custom topic
  |
  | authenticated webhook delivery
  v
ProductCare.Api
  |
  | validate event + current blob state
  v
Remove AzureSearch_Skip
  |
  v
Run Azure AI Search indexer
```

We do not use a separate quarantine container. Quarantine is represented by search-skip metadata on blobs in the existing private manual container.

ProductCare releases a manual only after all of the following are true:

- the Event Grid delivery is authenticated;
- the event represents the clean Defender result;
- the blob URI belongs to the configured storage account, container, and upload path;
- the event ETag matches the current blob;
- the current Defender blob index tag is `No threats found`.

The current scan-result tag is checked in addition to the event because event payloads can be delayed or duplicated and the blob may have changed since the scan.

The API's managed identity requires permission to read blob index tags. Blob tag permissions are separate from ordinary blob read/write permissions.

## Product and Account Lifecycle

Lifecycle operations are coordinated by application services rather than implemented directly in the browser or controller.

```text
Product / account delete request
        |
        v
ProductCare.Api
        |
        v
AccountLifecycleService
        |
        +--> Azure AI Search: delete product chunks
        |
        +--> Blob Storage: delete manual
        |
        +--> Foundry: delete product conversation
        |
        +--> SQL Server: delete ProductCare records
```

Product deletion is owner-scoped by authenticated tenant ID and user ID.

Self-service account deletion removes ProductCare application data and the ProductCare user-account record. It does not modify the Microsoft Entra External ID directory identity.

Administrative containment is intentionally separated from account deletion:

- `Disabled` is the application-level blocked state.
- Suspend changes the account to `Disabled` and leaves application data intact.
- Restore returns an unexpired disabled account to `Active`, otherwise to `Expired`.
- Security purge removes ProductCare-owned data while preserving the `Disabled` user-account row.
- Normal administrator account deletion removes the ProductCare user-account row and is therefore not used as the malicious-user containment action.

This distinction prevents a security purge from accidentally removing the application-level block. If the external identity must also be disabled or deleted, that is an Entra administrative operation outside the ProductCare runtime.

Cleanup is intentionally ordered so required external deletion runs before the corresponding SQL record is removed. If a remote resource is already gone, we rely on idempotent behavior and continue safely.

## Retrieval and Grounding

Product manual Q&A uses Azure AI Search for product-scoped retrieval.

The retrieval path:

1. derives a stable knowledge key from the saved product;
2. filters by product identity where applicable;
3. performs semantic and vector retrieval;
4. passes a bounded set of retrieved chunks to the model as untrusted document content;
5. requires structured output;
6. validates citation IDs against the retrieved sources.

Conversation history is used to understand follow-up intent, but it is not treated as authoritative product evidence. Manual evidence is retrieved again for each question.

## Assistant Orchestration

The ProductCare Assistant is application-owned orchestration on top of the Foundry Responses API.

The router can select:

- manual retrieval;
- CPSC recall lookup;
- both;
- no external tool.

Tools are executed by ProductCare code. The model does not receive generic network, SQL, file-system, or code-execution capabilities.

Tool follow-up rounds are bounded.

## Replaceable Frontend

The backend does not depend on Blazor.

A different client can:

1. authenticate against the supported identity provider;
2. request the same ProductCare API permission;
3. call the same versioned HTTP endpoints.

Replacing the frontend does not require redesigning the Domain, Application, Infrastructure, or AI projects.

## API Contract

`ProductCare.Api` is the contract between client applications and the backend.

Conventions include:

- versioned routes under `/api/v1`;
- JSON request and response models;
- standard HTTP status codes;
- bearer-token authentication;
- `ProblemDetails` for controlled errors;
- OpenAPI metadata.

The API contract is not tied to a .NET client.

## Error Handling

Expected application failures are translated into controlled HTTP responses.

Unexpected exceptions are logged server-side with trace context. Clients receive a generic response plus a trace identifier, never stack traces or internal implementation details.

## Request Tracing

Each API request gets a correlation ID.

That ID is attached to server logs and returned to callers to support incident triage. When enabled, OpenTelemetry carries request and AI activity traces into Azure Monitor/Application Insights.

GenAI content capture is disabled by default and only allowed in Development with explicit opt-in.

## Security Boundaries

The implementation follows these rules:

- browser clients are not trusted for authorization;
- protected customer endpoints validate Entra External ID tokens;
- the Event Grid webhook validates a separate managed-identity bearer token;
- secrets are not stored in Blazor WebAssembly;
- privileged Azure calls remain behind ProductCare.Api;
- ownership and business rules are enforced server-side;
- uploaded files and retrieved document content are treated as untrusted;
- manual indexing fails closed until Defender approval is verified;
- external recall status comes from CPSC rather than model memory;
- model-provided citations are validated by application code;
- AI execution has explicit bounds;
- Azure service access uses managed identity where supported.

## Deployment

In Azure, the API uses its system-assigned managed identity for SDK operations. Lifecycle cleanup needs Azure AI Search data-plane permission to delete indexed manual documents, so the runtime identity is assigned `Search Index Data Contributor`. Storage permissions must also allow manual deletion and Defender blob-index-tag reads.

The deployed architecture uses:

- Azure Static Web Apps for the Blazor WebAssembly client;
- Azure Container Apps for ProductCare.Api;
- Azure Container Registry for container images;
- SQL Server for application persistence;
- Microsoft Foundry for model execution;
- Azure AI Search for product-manual retrieval;
- Azure Blob Storage for manuals;
- Microsoft Defender for Storage for malware scanning;
- Azure Event Grid for scan-result delivery;
- Azure Monitor/Application Insights for telemetry.

GitHub Actions builds and tests the solution and deploys the API and web client through separate workflows.

The API uses its Container App system-assigned managed identity for supported Azure SDK access. Event Grid uses the custom topic's separate system-assigned managed identity for authenticated webhook delivery.
