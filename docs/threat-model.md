# ProductCare Threat Model

This is the working threat model for ProductCare's deployed application. It records controls implemented in source code and identifies limitations that still require operational review. It is an engineering document, not evidence of compliance certification or a substitute for penetration testing.

Update this document when a change alters an authentication, authorization, data-lifecycle, or AI trust boundary.

## Unauthenticated Customer API Access

### Risk

A caller accesses protected ProductCare functionality without a valid customer identity.

### Controls

- Customer tokens are validated through Microsoft Entra External ID and Microsoft Identity Web.
- Customer endpoints require the delegated `access_as_user` permission.
- `ProductCare.ActiveUser` additionally verifies active ProductCare access in the application database.
- `ProductCare.NotDisabled` denies administratively disabled identities from using the self-service account-deletion endpoint.
- `ProductCare.Admin` requires the application role and a non-disabled ProductCare account status.
- Health endpoints are explicitly anonymous for infrastructure probes.
- Production API startup requires the `AzureAd:ClientId` configuration value.

The policy requirements are evaluated by `ProductCare.Api`; the browser's authenticated appearance is not evidence of API authorization.

## Frontend Bypass

### Risk

A user modifies the browser, bypasses UI restrictions, or calls `ProductCare.Api` directly.

### Controls

The API independently enforces authentication, authorization, ownership, request validation, usage quotas, and domain rules. UI visibility alone cannot grant access.

## Cross-User Data Access

### Risk

An authenticated user requests another person's products, uploaded documents, conversation state, or usage data.

### Controls

- The API derives tenant and user IDs from validated token claims.
- Database queries include both ownership values.
- A product ID supplied by the caller is never accepted as proof of ownership.
- Conversation association is scoped through the saved product.
- Product deletion loads the caller-owned product before invoking external resource cleanup.

## Suspended or Malicious User

### Risk

An identity retains a valid Entra access token after a ProductCare administrator revokes access.

### Controls

- Suspension sets the ProductCare account to `Disabled`.
- `Disabled` users do not satisfy the `ActiveUser` or `NotDisabled` requirements.
- Administrative endpoints require a token carrying the `ProductCare.Admin` role and a non-disabled account status.
- A disabled account cannot submit a new access request while the record remains in the database.
- Suspension retains application data until a separate cleanup decision is made.
- An administrative purge removes ProductCare-managed data while retaining the disabled account record.
- Normal account deletion is not the malicious-user containment operation because it removes the application account record but leaves the Entra identity in place.

Directory-level blocking, identity deletion, and token/session revocation are outside this application's account-management boundary.

### Current data-deletion limitation

An account with expired demo access can still invoke the authenticated self-service account-deletion endpoint. An administratively disabled account cannot. The current application does **not** provide disabled users with a separate self-service data-only purge endpoint. An administrator can purge the associated ProductCare-managed data while retaining the block.

This distinction preserves the application-level restriction but creates an operational privacy-request dependency. Requests for deletion from disabled users require an administrative process; the implementation does not guarantee that backups, operational logs, or the external Entra identity are erased.

## Data Lifecycle and Orphaned Resources

### Risk

Removing only a SQL record leaves associated data in Blob Storage, Azure AI Search, or the remote conversation service.

### Controls

- Product and account deletion are coordinated by an application lifecycle service.
- Search chunks are selected by ProductCare product ID and explicitly deleted.
- The product manual is deleted from the private Blob container.
- The associated Foundry conversation is deleted when present.
- Required external resource cleanup runs before the corresponding SQL product row is removed.
- Already-absent resources are handled idempotently where supported.
- Administrative purge retains the disabled ProductCare user-account row.
- Self-service deletion removes ProductCare-managed data and the local user-account row for callers allowed by `NotDisabled`; it does not delete their Entra identity.

Backup retention, telemetry retention, downstream deletion consistency, and failed external cleanup require separate operational controls.

## Credential Exposure

### Risk

Privileged credentials are committed to source control, exposed through Blazor WebAssembly, or shared unnecessarily between services.

### Controls

- The browser contains public OAuth client and API configuration, not privileged server credentials.
- Local server secrets are kept in .NET User Secrets rather than checked-in configuration.
- Supported Azure SDK calls use the Container App's managed identity.
- The Event Grid webhook uses managed-identity bearer-token authentication, not a shared query-string token.
- Privileged Azure operations remain behind the API.
- Unexpected exception details are not returned to API callers.

This description does not substitute for secret scanning of all historical Git commits or for checking Azure secret rotation and RBAC assignments.

## Event Grid Webhook Spoofing

### Risk

An attacker or unrelated Azure principal submits a forged Defender scan event in an attempt to release an unsafe manual for indexing.

### Controls

- The webhook requires a separate JWT bearer scheme and authorization policy.
- Event Grid delivers using the custom topic's system-assigned managed identity.
- Token issuer is restricted to the configured Azure tenant.
- Token audience is restricted to the dedicated webhook application registration.
- Authorization requires the token `oid` to match the configured Event Grid identity object ID.
- An event payload alone cannot release a manual; current Blob state must be verified.

## Event Replay or Stale Scan Result

### Risk

An old or duplicate clean event arrives after a manual Blob has been changed.

### Controls

ProductCare verifies the configured storage account, container, and upload path; compares the event ETag with the current Blob ETag; reads the current Defender Blob index tag; and requires the tag `No threats found`. Previously approved ETags are treated idempotently.

A stale event cannot approve a different Blob revision by relying on the event result alone.

## Malicious or Unscanned Manual Indexing

### Risk

A failed, malicious, or unscanned PDF becomes searchable and influences assistant answers.

### Controls

- The API checks the PDF extension, content type, length, and `%PDF-` signature.
- Uploaded Blobs begin with `AzureSearch_Skip=true`.
- Defender for Storage scans uploads.
- The webhook must be authenticated and report a clean result.
- The current Blob tag and ETag must match the clean event.
- `Malicious`, `Error`, `Not scanned`, unknown results, missing verdicts, or tag-read failures do not authorize indexing.
- No separate quarantine container is required; the existing private manual container remains excluded from Search until approved.

## Blob Index Tag Tampering

### Risk

A principal with Blob index-tag write permission falsifies a Defender verdict.

### Controls

- Tag permissions are constrained through Azure RBAC.
- ProductCare never relies on the tag alone as the approval signal.
- Release requires the authenticated event, current clean tag, and matching ETag.
- Azure role assignments need periodic review and least-privilege scoping.

## AI Usage Abuse

### Risk

A user makes excessive model calls, submits oversized inputs, or causes repeated tool execution.

### Controls

- AI endpoints require active application access.
- Daily usage quotas are persisted by user and UTC date in SQL Server.
- The checked-in defaults are 5 identification requests, 30 separate manual questions, and 30 assistant requests per day.
- Manual-question, identification, and assistant operations have separate quota keys.
- Request sizes, retrieved context, and tool iterations are bounded.
- Access-request submission uses fixed-window rate limiting.

### Remaining work

Global API throttling and broader abuse detection are not implemented. Those controls need further review before higher-volume rollout.

## Uploaded Images

### Risk

A product-identification image is oversized, malformed, or uses an unsupported format.

### Controls

- Only approved media types are accepted.
- Image signatures are validated against the submitted media type.
- Upload size is bounded at 5 MB.
- Model results use structured output and application validation.

## Prompt Injection in Input or Retrieved Documents

### Risk

User input or retrieved manual content attempts to override instructions, alter tool selection, or cause unsupported answers.

### Controls

- Retrieved manual content is treated as untrusted data.
- Application instructions separate model behavior from source evidence.
- Where implemented, the application handles Foundry content-filter rejections as controlled failures. The effective provider-side safety policy must be reviewed in the deployed configuration.
- Tools are explicitly defined and executed by ProductCare code.
- Arbitrary HTTP, SQL, filesystem, and code-execution tools are not exposed to the model.
- Tool iterations are bounded.
- Manual answers rely on fresh retrieval; citation IDs are checked against retrieved source IDs.

Prompt-injection controls are defense in depth, not proof of immunity.

## Unsafe Troubleshooting or Safety Claims

### Risk

The model produces dangerous repair instructions or presents incomplete recall information as evidence that a product is safe.

### Controls

- Product-manual answers use retrieved documentation and acknowledge missing evidence.
- The assistant has a CPSC recall capability and a direct fallback for recognized safety questions.
- CPSC unavailability is distinguished from no matching recall.
- A missing recall match is not a safety guarantee.
- Application and model instructions constrain high-risk guidance.

## AI Reliability and Hallucination

### Risk

Model output is plausible but incorrect, incomplete, or unsupported.

### Controls

- Structured outputs are used where appropriate.
- Product identification preserves uncertainty.
- Manual Q&A is grounded using Azure AI Search.
- Model-generated citations are validated by application code.
- Conversation history clarifies intent but is not authoritative manual evidence.
- Recall data comes from CPSC, not model memory.
- Tool execution and retrieval are bounded.

### Remaining work

A broader regression suite is needed for grounding drift, prompt injection, tool selection, conversation follow-ups, recall phrasing, and changes to model deployments.

## Dependency or External-Service Failure

### Risk

Foundry, Search, Storage, Defender, Event Grid, CPSC, or another dependency is unavailable or returns an unexpected response.

### Controls

- Expected application failures return controlled HTTP responses.
- Unexpected exceptions are logged with correlation identifiers.
- CPSC unavailability is not presented as no matching recall.
- Manual security processing fails closed.
- Event Grid can retry webhook failures returned as a retryable status.
- Search indexing state is checked independently from indexer invocation.
- Lifecycle cleanup stops before SQL deletion when required external cleanup fails.

## Logging and Sensitive Data

### Risk

Request data or model content is captured in telemetry unexpectedly.

### Controls

- GenAI message-content tracing is disabled by default.
- Content capture requires Development and explicit `Observability:CaptureGenAIContent` opt-in.
- Correlation identifiers support troubleshooting without returning exception details to callers.
- Telemetry configuration remains server-side.

## Operational Security Review

These controls require periodic review as deployment evolves:

- Azure RBAC scope, role assignments, and managed-identity ownership;
- public ingress, CORS, and HTTPS configuration;
- API-wide throttling and abuse monitoring;
- alert routing and incident response;
- dependency retries and circuit breakers;
- user data retention, backup deletion, and handling of privacy requests for disabled accounts;
- secret scanning and credential rotation;
- backup and restore procedures;
- security-event monitoring;
- infrastructure-as-code coverage and disaster recovery.
