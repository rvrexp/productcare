# ProductCare Environments

## Local

Local development currently runs:

- ProductCare.Web over HTTPS
- ProductCare.Api over HTTPS
- Microsoft Entra External ID for authentication

Server-side development settings that should not be committed use .NET User Secrets.

The Blazor WebAssembly client contains only values that must be considered public, including:

- tenant ID
- application client ID
- authority
- API URL
- API scope

No client secret or privileged Azure credential belongs in ProductCare.Web.

Development CORS is restricted to the configured ProductCare.Web origin.

## Demo

The demo environment is intended for portfolio and interviewer access.

Requirements:

- authentication required
- no anonymous AI endpoints
- per-user usage limits
- API rate limiting
- bounded model and tool execution
- HTTPS only
- request and AI tracing
- Azure budget alerts
- low-cost service configuration where practical
- no development-only diagnostic endpoints exposed publicly

The demo environment is deliberately sized for low traffic.

It should not be represented as a production-scale deployment.

## Production

A production environment is outside the current milestone.

If ProductCare moves toward real usage, the following areas need to be revisited based on actual operational requirements:

- availability targets
- regional and data-residency requirements
- backup and recovery
- scaling
- data retention
- incident response
- operational support
- disaster recovery
- abuse protection
- service-level objectives
