# ProductCare Architecture

## Overview

ProductCare runs as a layered .NET backend with a separately deployed browser client.

The frontend is a standalone Blazor WebAssembly application that communicates with `ProductCare.Api` over HTTPS. The browser is an untrusted presentation layer; it has no direct project dependency on backend implementation projects and does not make authoritative authorization decisions.

Backend responsibilities are separated into API, application, domain, infrastructure, and AI projects. This keeps Azure SDK details and model-specific behavior out of business rules, while making external integrations easier to test and replace.

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

The backend's intended dependency direction is:

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

`ProductCare.Web` does not reference backend implementation projects. Its connection is the HTTP API:

```text
ProductCare.Web
       |
       | HTTPS / JSON
       v
ProductCare.Api
```

Architecture tests enforce key dependency constraints in CI.

## ProductCare.Web

`ProductCare.Web` is the presentation layer. It:

- renders the user interface;
- authenticates users through Microsoft Entra External ID;
- acquires delegated access tokens for `ProductCare.Api`;
- calls versioned API endpoints;
- maintains presentation-specific state, including the visible assistant transcript.

It does not access SQL Server, Foundry, Azure AI Search, or Blob Storage directly. It does not contain privileged Azure credentials or perform final authorization checks.

Authentication state can control which UI elements appear, but the API independently enforces access rules.

## ProductCare.Api

`ProductCare.Api` is the public application boundary and the main security enforcement point.

Responsibilities include:

- validating customer access tokens and delegated permissions;
- validating Event Grid webhook tokens through a separate authentication scheme;
- enforcing ProductCare authorization policies;
- validating inputs, product ownership, and account access;
- applying usage quotas and request limits;
- exposing versioned HTTP contracts;
- composing application services and infrastructure integrations;
- providing health endpoints, exception handling, and request telemetry.

Customer API calls require the delegated `access_as_user` scope and the policy appropriate to each endpoint. The `/health/live` and `/health/ready` endpoints are intentionally anonymous for platform probes. The Event Grid webhook is authenticated and uses a separate issuer, audience, and allowed-caller policy.

The API registers the customer authentication configuration when `AzureAd:ClientId` is present. In Production, a missing value is a startup error. This fail-fast condition is part of the production configuration correction delivered with this documentation update.

## ProductCare.Application

`ProductCare.Application` implements use cases and defines service abstractions. It coordinates domain behavior without depending on controllers, Blazor, concrete EF Core repositories, Azure SDK implementations, or Foundry-specific clients.

## ProductCare.Domain

`ProductCare.Domain` contains the core product, identity, access, and account rules. It remains independent of ASP.NET Core, Blazor, Azure, databases, and external AI APIs.

## ProductCare.Infrastructure

`ProductCare.Infrastructure` contains non-AI infrastructure, including EF Core persistence, SQL Server repository implementations, quota persistence, and the CPSC recall integration.

## ProductCare.AI

`ProductCare.AI` contains model and product-knowledge integrations:

- photo-based product identification;
- Foundry Responses API interactions;
- structured model output handling;
- Azure AI Search retrieval;
- product-manual Blob Storage operations;
- Defender scan-result processing and indexing release;
- cleanup of Search, Blob, and Foundry conversation resources;
- grounded answer generation;
- assistant routing and bounded tool execution;
- streaming and AI tracing.

Separating these implementations limits coupling between business logic and evolving managed-service APIs.

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

The web client is a public OAuth client and has no client secret. `ProductCare.Api` validates the access token and delegated scope, then applies ProductCare authorization requirements.

Current policies are:

| Policy | API enforcement |
| --- | --- |
| `ProductCare.User` | Valid customer authentication and delegated `access_as_user` scope |
| `ProductCare.ActiveUser` | Customer authentication, scope, and active ProductCare account access |
| `ProductCare.NotDisabled` | Customer authentication, scope, and an account state other than `Disabled` |
| `ProductCare.Admin` | Customer authentication, scope, `ProductCare.Admin` role, and an account state other than `Disabled` |

`NotDisabled` does not require an active demo access period. An expired user can therefore access the self-service account-deletion endpoint. A user with no ProductCare account record is treated as `Registered` for this check; it is not a substitute for the `ActiveUser` requirement on product and AI endpoints.

A valid Entra token alone does not grant active application access. Moving an account to `Disabled` blocks endpoints requiring active access and the administrative and account-deletion operations listed above, even if an issued token has not yet expired.

## Event Grid Authentication Flow

Defender for Storage scan results are delivered through an Event Grid custom topic:

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

The webhook uses a separate Microsoft Entra application registration as the token audience. The API validates the token signature and lifetime, the configured tenant issuer and audience, and the `oid` claim against the Event Grid custom topic's managed identity object ID.

A valid token from an unrelated principal in the same tenant is insufficient. The webhook does not use a shared query-string token.

## Product Manual Security Boundary

Product manuals are untrusted input. The upload and indexing path is:

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
  | validate event and current blob state
  v
Remove AzureSearch_Skip
  |
  v
Run Azure AI Search indexer
```

There is no separate quarantine container in the current implementation. A pending or rejected manual remains excluded from Search through metadata on the existing private manual container.

A manual is released for indexing only when:

- the Event Grid delivery is authenticated;
- the event reports a clean Defender result;
- the blob URI belongs to the configured storage account, container, and upload path;
- the event ETag matches the current blob;
- the current Defender blob index tag is `No threats found`.

The tag is checked independently from the incoming event because deliveries may be delayed or duplicated and blobs may be replaced. Reading blob index tags requires a distinct permission from ordinary blob data operations.

## Product and Account Lifecycle

Lifecycle operations are coordinated through application services:

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

Product deletion is scoped by authenticated tenant ID and user ID. Required external cleanup runs before the corresponding SQL product row is removed. Missing remote resources are tolerated where the underlying operation supports idempotency.

Self-service account deletion is available to authenticated users who satisfy `ProductCare.NotDisabled`, including users whose approved access has expired. It removes ProductCare-managed data and the local account record, but does not remove the Microsoft Entra External ID identity. Administratively disabled users cannot invoke this self-service deletion endpoint.

Administrative account operations have different effects:

- **Suspend:** Set the ProductCare account status to `Disabled`, without deleting its data.
- **Restore:** Restore the account according to its prior plan, access expiration, and latest access-request state. A demo account can return to `Active` or `Expired`; accounts without a demo plan may return to `Registered`, `PendingApproval`, or `Rejected`.
- **Purge:** Remove ProductCare-owned products and related resources, usage counters, and access requests while retaining the disabled account record.
- **Delete:** Remove the ProductCare account record and its dependent data. This is not used as the malicious-user containment operation, because the external Entra identity is not deleted.

A disabled user does not currently have a separate self-service data-only purge endpoint. Administrators can use the purge operation to remove ProductCare-managed data without removing the application-level block. This behavior is a deliberate current limitation, not a claim of complete erasure from backups, logs, or external identity services.

Directory identity blocking or deletion and session revocation require separate Entra administrative actions.

## Retrieval and Grounding

Product-manual questions use Azure AI Search to retrieve product-scoped information. The retrieval path:

1. derives a stable knowledge key from the saved product;
2. filters by the relevant product identity;
3. retrieves a bounded set of chunks through semantic and vector search;
4. supplies retrieved text to the model as untrusted document content;
5. requires structured output where applicable;
6. validates model-produced citation IDs against the retrieved sources.

Conversation history helps interpret follow-up questions, but it is not considered authoritative product evidence. Manual retrieval runs again for each question.

## Assistant Orchestration

ProductCare Assistant uses application-controlled orchestration on the Foundry Responses API. Available tools include manual retrieval and CPSC recall lookup. The router can select either, both, or neither. ProductCare code defines and executes the tools; the model has no arbitrary network, SQL, filesystem, or code-execution tools.

Tool follow-up rounds are bounded. Answers stream to the browser over a structured newline-delimited JSON response, followed by a validated completed result.

A saved product retains an active Foundry conversation ID to support follow-up context. The browser transcript itself is not durable chat history.

## Replaceable Frontend

The backend does not depend on Blazor. A different client can authenticate against the supported provider, request the delegated API permission, and use the same versioned HTTP endpoints. Replacing the frontend does not require redesigning the Domain, Application, Infrastructure, or AI projects.

## API Contract

`ProductCare.Api` is the contract between clients and the backend. Conventions include versioned `/api/v1` routes, JSON contracts, appropriate HTTP status codes, bearer-token authentication, `ProblemDetails` for controlled errors, and OpenAPI metadata.

OpenAPI is mapped by the application in Development, rather than published as an unrestricted Production endpoint.

## Error Handling

Expected application failures return controlled HTTP responses. Unexpected exceptions are logged server-side with trace context. Clients receive a generic failure response with a trace identifier, not the server exception or stack trace.

## Request Tracing

Each API request gets a correlation ID attached to logging scope and returned to callers. When configured, OpenTelemetry exports request and AI traces to Azure Monitor/Application Insights.

GenAI message-content capture is disabled by default and is enabled only in Development with the explicit `Observability:CaptureGenAIContent` setting.

## Security Boundaries

The implementation follows these boundaries:

- browser clients are not trusted for authorization;
- customer API endpoints validate delegated Entra External ID tokens;
- administrative endpoints additionally require the admin role and a non-disabled account;
- product and AI operations require active ProductCare access;
- the Event Grid webhook validates a separate managed-identity bearer token;
- secrets and privileged Azure operations remain on the server;
- ownership and business rules are enforced server-side;
- uploaded files and retrieved content are treated as untrusted;
- manual indexing fails closed until Defender approval is verified;
- recall information comes from CPSC rather than model memory;
- citations and tool execution are validated or bounded by application code;
- managed identity is used for supported Azure service access.

## Deployment

The deployed architecture uses Azure Static Web Apps, Azure Container Apps, Azure Container Registry, SQL Server, Microsoft Foundry, Azure AI Search, Azure Blob Storage, Defender for Storage, Event Grid, and Azure Monitor/Application Insights.

The Container App system-assigned managed identity is used for supported Azure SDK operations. Product cleanup requires Azure AI Search data-plane permission to query and delete document chunks; the runtime identity is assigned `Search Index Data Contributor`. Storage permissions must permit manual deletion and Defender blob-index-tag reads.

GitHub Actions builds and tests the solution. The API workflow validates EF Core model changes, generates a migration bundle, authenticates to Azure with GitHub OIDC, deploys the image, and applies migrations. The web client is deployed through its own Azure Static Web Apps workflow.

The Event Grid custom topic uses a different system-assigned managed identity for authenticated webhook delivery. Production API startup requires a configured `AzureAd:ClientId`.
