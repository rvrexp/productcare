# ProductCare Environments

This document describes how ProductCare is configured across local development and deployed Azure environments. The intent is to capture how we actually run the system, where trust boundaries are enforced, and what must be reviewed before scale-up.

## Local Development

A standard local setup runs:

- `ProductCare.Web` over HTTPS;
- `ProductCare.Api` over HTTPS;
- Microsoft Entra External ID for customer authentication;
- local SQL Server/LocalDB where configured;
- remote Azure services for Foundry, Search, and Blob integration.

Server-side, environment-specific values stay out of source control. For local work we use .NET User Secrets for settings that should never be committed.

The API expects these configuration sections:

```text
AzureAd
ConnectionStrings:ProductCareDatabase
Foundry
ProductKnowledgeSearch
ProductManualIngestion
EventGridAuthentication
UsageQuotas
ApplicationInsights
Observability
Cors
```

Event Grid authentication settings:

```text
EventGridAuthentication:
  TenantId
  Audience
  AllowedCallerObjectId
```

These values identify the Azure tenant, webhook audience, and Event Grid topic managed identity. They are environment-specific and must be configured per environment; they are not shared secrets and are not hard-coded.

When present, local API runs can validate Event Grid authentication policy. In practice, Azure Event Grid does not deliver to localhost unless a publicly reachable HTTPS endpoint is intentionally exposed.

The Blazor WebAssembly client contains only public OAuth/API configuration:

- authority;
- application client ID;
- known authority;
- API base URL;
- API scope.

No client secret, database credential, Foundry credential, Search credential, Storage credential, or privileged server configuration belongs in `ProductCare.Web`.

Development CORS remains restricted to the configured web origin.

## Deployed Environment

Current deployment uses:

- Azure Static Web Apps for the client;
- Azure Container Apps for the API;
- Azure Container Registry;
- SQL Server;
- Microsoft Foundry;
- Azure AI Search;
- Azure Blob Storage;
- Microsoft Defender for Storage;
- Azure Event Grid;
- Azure Monitor/Application Insights.

Security and operational controls in place:

- HTTPS-only traffic;
- Microsoft Entra External ID authentication for customer access;
- server-side ownership and authorization enforcement;
- application access approval;
- per-user AI usage quotas;
- fixed-window rate limiting on access-request submission;
- bounded model and tool execution;
- authenticated Event Grid webhook delivery via the Event Grid topic system-assigned managed identity;
- malware-gated manual indexing;
- managed identity for supported Azure service access;
- request correlation and AI tracing;
- controlled error responses;
- CI/CD through GitHub Actions.

Event Grid webhook settings are provided to the Container App as environment configuration:

```text
EventGridAuthentication__TenantId
EventGridAuthentication__Audience
EventGridAuthentication__AllowedCallerObjectId
```

The legacy shared-secret webhook approach is intentionally not used.

The ProductCare API managed identity must have the minimum Azure permissions needed for runtime behavior. Manual processing includes Defender blob index-tag reads in addition to normal blob operations.

## Runtime Azure RBAC

In Azure, ProductCare.Api uses the Container App system-assigned managed identity for SDK access.

Lifecycle and manual-security flows require:

- Blob data permissions for normal manual operations and manual deletion;
- blob index-tag read permission for Defender malware verdict checks;
- Azure AI Search document query/write/delete permissions for cleanup.

`Search Index Data Contributor` is assigned to the ProductCare API managed identity so the service can identify and delete indexed manual chunks during product/account deletion.

RBAC scope should stay tight (service/resource level) and not be broadened for convenience.

## Environment Separation

Customer identity and Azure resource identity are intentionally separate trust paths.

Customer API traffic is authenticated through Microsoft Entra External ID.

Event Grid webhook traffic is authenticated with tokens issued for the Event Grid custom topic system-assigned managed identity in the Azure resource tenant.

The API registers separate authentication schemes and policies for these paths. A customer token cannot call the Event Grid webhook, and an Event Grid delivery token is never treated as customer identity.

## Production Readiness Considerations

The current deployment has the required baseline controls for the intended workload. Capacity and operational settings, however, must be based on measured SLOs and incident history, not assumptions from local or low-volume environments.

Before production-scale operation, review at minimum:

- availability and recovery objectives;
- database backup and restore procedures;
- regional and data-residency requirements;
- capacity and autoscaling thresholds;
- alert routing and incident response;
- retention and deletion policies for user accounts, products, uploaded manuals, Search documents, conversations, and logs;
- secret and certificate rotation procedures where applicable;
- dependency failure handling and retry policy;
- rate limiting and abuse controls across the public API;
- Azure RBAC review, including Search document deletion and Blob tag access;
- infrastructure-as-code coverage;
- disaster recovery;
- service-level objectives and operational ownership.
