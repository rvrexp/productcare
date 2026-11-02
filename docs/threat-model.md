# ProductCare Threat Model

This is the working threat model for ProductCare in production. It captures the threats we actively design for and the controls currently enforced in application code and Azure.

Treat this as an engineering artifact, not a compliance substitute. When architecture changes, this file should be updated in the same PR.

## Unauthenticated Customer API Access

### Risk

A caller accesses protected ProductCare functionality without a valid customer identity.

### Controls

- Microsoft Entra External ID authentication.
- Bearer-token validation in ProductCare.Api.
- Required delegated `access_as_user` scope for protected customer endpoints.
- Application authorization policies for user, active-user, and administrator access.
- Health endpoints are explicitly anonymous for infrastructure probes.

## Frontend Bypass

### Risk

A user modifies the browser application, bypasses UI restrictions, or calls ProductCare.Api directly.

### Controls

ProductCare.Web is not treated as a security boundary.

ProductCare.Api independently enforces:

- authentication;
- authorization;
- ownership;
- request validation;
- usage quotas;
- business rules.

UI visibility is never the only authorization control.

## Cross-User Data Access

### Risk

An authenticated user accesses another user's products, documents, conversation state, or usage data.

### Controls

- Product ownership is derived from the authenticated tenant ID and user ID.
- Repository queries include ownership values in database predicates.
- Client-supplied product identifiers are not accepted as proof of ownership.
- Conversation association is scoped through the owned product.
- Product deletion resolves the product through the authenticated owner before resource cleanup is allowed.

## Suspended or Malicious User

### Risk

A user with a valid Microsoft Entra token continues to call ProductCare after an administrator has determined that access should be removed.

### Controls

- ProductCare authorization evaluates application account state in addition to token validity.
- Administrator suspension changes the account state to `Disabled`.
- `Disabled` users do not satisfy `ProductCare.ActiveUser`, even when an already-issued Entra access token remains valid.
- Suspension does not delete user data, preserving the ability to investigate or restore access.
- A security purge deletes ProductCare-owned products, manuals, indexed content, conversations, usage counters, and access requests while retaining the `Disabled` user-account row.
- Normal account deletion is not used as the malicious-user containment action because deleting the ProductCare user row would allow the unchanged external Entra identity to register again.

Directory-level identity blocking, session revocation, and Entra identity deletion remain separate administrative controls outside the ProductCare application boundary.

## Data Lifecycle and Orphaned Resources

### Risk

Deleting only the SQL record leaves product data in Blob Storage, Azure AI Search, or the remote conversation service.

### Controls

- Product and account deletion are coordinated by an application lifecycle service.
- Product cleanup identifies Azure AI Search chunks by ProductCare product ID and deletes them explicitly.
- The uploaded manual blob is deleted from the private manual container.
- The associated Foundry conversation is deleted when one exists.
- Required external cleanup occurs before the corresponding SQL product record is removed.
- Azure AI Search document deletion is treated as an idempotent operation.
- Self-service account deletion removes ProductCare application data but does not delete the external Microsoft Entra identity.
- Administrative security purge preserves a disabled ProductCare account row after dependent data has been removed.

## Credential Exposure

### Risk

Privileged credentials are committed to source control, exposed through the browser client, or unnecessarily distributed between services.

### Controls

- No client secret in Blazor WebAssembly.
- No Foundry, Search, Storage, or database credentials in the browser.
- Local server configuration uses .NET User Secrets.
- Deployed Azure SDK access uses managed identity where supported.
- Event Grid webhook delivery uses managed identity rather than a shared query-string secret.
- Privileged Azure operations remain behind ProductCare.Api.
- Sensitive exception details are not returned to clients.
- The API managed identity receives Search document data-plane permission only where required for retrieval and lifecycle cleanup.

## Event Grid Webhook Spoofing

### Risk

An attacker or unrelated Azure principal submits a forged Defender scan-result event to the internal webhook and attempts to release an untrusted manual for indexing.

### Controls

- The webhook requires a dedicated JWT bearer authentication scheme.
- Event Grid delivers using the custom topic's system-assigned managed identity.
- Token issuer is restricted to the configured Azure resource tenant.
- Token audience is restricted to the dedicated webhook application registration.
- Authorization requires the token `oid` to match the configured Event Grid topic managed identity object ID.
- The previous shared-secret query-token design is not used.
- The event payload alone is never sufficient to release a manual.

## Event Replay or Stale Scan Result

### Risk

A valid but old or duplicated scan-result event is delivered after the blob has been replaced or changed.

### Controls

Before releasing a manual, ProductCare verifies:

- the blob URI belongs to the configured storage account, container, and upload path;
- the scan event ETag matches the current blob ETag;
- the current Defender blob index tag is `No threats found`;
- an already-approved ETag is handled idempotently.

A stale clean event cannot approve a different blob revision.

## Malicious or Unscanned Manual Indexing

### Risk

A malicious, failed, or unscanned PDF becomes searchable and is supplied to the AI retrieval pipeline.

### Controls

- PDF extension, content type, size, and `%PDF-` signature validation.
- Uploads are stored with `AzureSearch_Skip=true`.
- Defender for Storage on-upload malware scanning.
- Defender scan results are sent through Event Grid.
- Only an authenticated clean event combined with a current clean blob tag can remove the search-skip metadata.
- `Malicious`, `Error`, `Not scanned`, unknown results, missing verdicts, and tag-read failures fail closed.
- A separate quarantine container is not required; the existing private container remains blocked from Search until approval.

## Blob Index Tag Tampering

### Risk

A principal with permission to modify blob index tags changes the Defender result.

### Controls

- Blob tag permissions are restricted through Azure RBAC.
- ProductCare does not rely on the tag as the sole approval signal.
- Release requires both an authenticated Defender/Event Grid event and the current clean tag.
- The event ETag must match the current blob.
- RBAC assignments should be reviewed periodically and kept to the minimum set required by runtime operations.

## AI Usage Abuse

### Risk

A user creates excessive model requests, large prompts, repeated tool execution, or unnecessarily long-running agent activity.

### Controls

- Authentication is required for AI features.
- Per-user daily AI quotas are persisted in SQL Server.
- Tool follow-up rounds are bounded.
- Inputs and retrieval sets are bounded.
- Access-request submission is rate limited.

### Remaining Work

API-wide throttling and broader abuse detection are still on the hardening backlog and should be closed before higher-volume rollout.

## Uploaded Images

### Risk

An image upload is oversized, malformed, or uses an unsupported format.

### Controls

- Allowed media types are validated.
- File signatures are validated.
- Input size is bounded.
- Model output is structured and validated.

## Prompt Injection in User Input or Retrieved Documents

### Risk

User-provided text or retrieved manual content attempts to override application instructions, manipulate tool selection, or influence grounded answers.

### Controls

- Retrieved content is treated as untrusted document data.
- Grounding instructions separate application instructions from document content.
- Foundry guardrails are applied to the configured deployments.
- Tool capabilities are explicitly defined by application code.
- The model does not receive arbitrary network, SQL, file-system, or code-execution tools.
- Tool execution is bounded.
- Manual answers must be grounded in fresh retrieval results.
- Citation IDs returned by the model are validated against the retrieved source set.

Prompt-injection resistance is defense in depth. It is not treated as proof that model behavior can never be manipulated.

## Unsafe Troubleshooting or Safety Claims

### Risk

The model provides unsafe repair guidance or presents incomplete recall information as proof of product safety.

### Controls

- Product-manual answers prefer retrieved documentation.
- Unsupported manual answers return a no-answer result.
- Recall information comes from the CPSC service rather than model memory.
- Failure to reach CPSC is reported as source unavailability.
- The absence of a matching recall is not presented as proof that a product is safe.
- High-risk guidance is constrained by application and model instructions.

## AI Reliability and Hallucination

### Risk

Model output is plausible but incorrect, incomplete, or unsupported.

### Controls

- Structured outputs are used where appropriate.
- Product identification preserves explicit uncertainty.
- Manual Q&A is grounded against Azure AI Search results.
- Citation IDs are validated in code.
- Conversation history is used for intent, not as authoritative product evidence.
- External recall truth is retrieved from CPSC.
- Model and tool execution are bounded.

### Remaining Work

We still need a broader regression evaluation suite that covers grounding drift, prompt-injection resilience, tool selection quality, follow-up rewriting, recall phrasing, and model/deployment changes.

## Dependency or External-Service Failure

### Risk

Foundry, Search, Storage, Defender, Event Grid, CPSC, or another dependency fails or returns an unexpected result.

### Controls

- Expected application failures are translated into controlled HTTP responses.
- Unexpected exceptions are logged with correlation identifiers.
- CPSC unavailability is distinguished from "no recall found."
- Manual security failures fail closed.
- Event Grid delivery can retry retryable webhook failures.
- Search indexing state is checked independently from indexer invocation.
- Lifecycle cleanup stops before SQL product deletion when required external cleanup fails, avoiding silent orphaning of remote data.

## Logging and Sensitive Data

### Risk

Sensitive request or model content is captured in telemetry unintentionally.

### Controls

- GenAI message-content tracing is disabled by default.
- Content tracing requires Development plus explicit `Observability:CaptureGenAIContent` enablement.
- Correlation IDs support troubleshooting without exposing internal exceptions to clients.
- Application telemetry configuration remains server-side.

## Operational Security Review

As deployment evolves, these controls should be reviewed during release readiness and platform change reviews:

- Azure RBAC scope and role assignments;
- managed-identity ownership and lifecycle;
- public ingress requirements;
- API-wide throttling and abuse controls;
- alert routing;
- dependency retry and circuit-breaker behavior;
- retention and deletion requirements, including malicious-user evidence retention before purge;
- backup and restore;
- security-event monitoring;
- infrastructure-as-code coverage.
