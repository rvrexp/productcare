# ProductCare Threat Model

This document tracks the security risks that currently affect ProductCare and the controls that influence implementation decisions.

It is not intended to be an exhaustive enterprise threat model.

## Unauthenticated API Access

### Risk

A caller accesses protected ProductCare functionality without a valid identity.

### Current Controls

- Microsoft Entra External ID authentication
- ProductCare.Api protected by default
- `access_as_user` scope required for protected endpoints
- bearer-token validation performed by the API
- health endpoints explicitly marked anonymous

## Frontend Bypass

### Risk

A user modifies the browser application, bypasses UI restrictions, or calls ProductCare.Api directly.

### Controls

ProductCare.Web is not treated as a security boundary.

ProductCare.Api independently enforces:

- authentication
- authorization
- ownership
- request validation
- quotas
- business rules

UI visibility is never the only authorization control.

## Cross-User Access

### Risk

An authenticated user accesses another user's products, documents, history, or maintenance data.

### Planned Control

Ownership checks will be performed server-side using the authenticated identity.

A client-provided user identifier will not be accepted as proof of ownership.

## Credential Exposure

### Risk

Secrets or privileged credentials are committed to source control or exposed through the browser client.

### Controls

- no client secret in Blazor WebAssembly
- no Foundry or Search credentials in the frontend
- local server configuration uses .NET User Secrets
- secret files are excluded from Git
- managed identity will be preferred for Azure service access
- privileged Azure operations remain behind ProductCare.Api

## AI Usage Abuse

### Risk

A user intentionally creates excessive model requests, large prompts, repeated tool execution, or long-running agent activity.

### Planned Controls

- authentication required
- per-user quotas
- rate limiting
- model token limits
- tool-call limits
- bounded agent execution
- Azure budget alerts
- global AI kill switch

## Uploaded Images and Documents

### Risk

Uploaded files are oversized, malformed, unsupported, or malicious.

### Planned Controls

The API will validate:

- allowed file types
- actual content type
- maximum file size
- processing limits

Uploaded content will be treated as untrusted input.

## Prompt Injection

### Risk

User-provided or retrieved content attempts to override model instructions or influence tool use.

### Planned Controls

- retrieved content treated as data rather than trusted instructions
- constrained system instructions
- explicit tool permissions
- bounded agent execution
- grounding requirements
- prompt-injection evaluation
- restricted handling of high-risk actions

## Unsafe Troubleshooting

### Risk

The model provides unsafe repair or maintenance guidance.

### Planned Controls

ProductCare should:

- prefer authoritative documentation
- distinguish evidence from assumptions
- avoid unsupported safety claims
- avoid presenting absence of recalls as proof of safety
- stop or escalate high-risk procedures
- recommend qualified service where appropriate

## AI Reliability

### Risk

Model output is plausible but incorrect, incomplete, or unsupported.

### Planned Controls

- structured outputs where appropriate
- grounding against source material
- citations for documentation-based answers
- explicit uncertainty
- representative evaluation cases
- regression testing as prompts and models change

The AI implementation is being evaluated as part of the project rather than being assumed correct because a model produced a valid-looking answer.
