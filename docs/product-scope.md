# ProductCare Scope

## Purpose

ProductCare is an AI-assisted product support application.

For this milestone, we are deliberately shipping a narrow, connected workflow instead of trying to solve the full consumer-product lifecycle in one pass.

The goal is to prove a production-style architecture for AI features in .NET: vision-based identification, grounded retrieval, recall checks, and controlled orchestration with measurable behavior.

## Product Identification

Users start by uploading an image of a product or model label.

ProductCare attempts to identify:

- manufacturer
- model number
- product type
- product name (only when evidence supports it)

We preserve uncertainty by design. If required fields cannot be derived with confidence, they stay unknown rather than being backfilled with guesses.

## Recall Checking

Once we have product identity data, ProductCare checks authoritative recall sources.

Results are explicitly separated into:

- matching recall found
- no matching recall found
- insufficient product data for a reliable match

"No matching recall found" is never presented as a safety guarantee.

## Documentation Q&A

ProductCare retrieves product-specific documentation and answers questions from retrieved material.

Answers should stay grounded in retrieved content and include citations when practical.

If supporting documentation is missing or ambiguous, the system should return that limitation directly rather than filling gaps from model memory.

## Maintenance

The app includes a limited maintenance workflow tied to stored product context.

In this portfolio release, the objective is to demonstrate that maintenance guidance can reuse the same identified product and documentation context already established by the user journey.

## Frontend

The current client is standalone Blazor WebAssembly.

It calls `ProductCare.Api` over HTTPS and has no direct references to backend implementation projects.

Backend contracts are intentionally client-agnostic so a future React or React Native app can use the same API without backend redesign.

## Authentication

Customer authentication uses Microsoft Entra External ID.

The browser client uses Authorization Code + PKCE.

`ProductCare.Api` remains the enforcement point for authorization and ownership.

## AI Design

The AI workflow is split into specialist capabilities for:

- product identification
- documentation retrieval and Q&A
- recall and safety checks
- maintenance support

A coordinator invokes only the capabilities required for the active request.

This keeps latency and cost predictable, reduces unnecessary model calls, and enforces execution limits.

AI behavior is shipped incrementally and treated as untrusted until validated against representative scenarios.

## Out of Scope

This milestone does not include:

- subscriptions or payments
- marketplace features
- technician booking
- social features
- voice interaction
- video processing
- native mobile packaging
- production-scale multi-region deployment

If the project moves beyond portfolio scope, we can revisit these items with explicit product and operational requirements.
