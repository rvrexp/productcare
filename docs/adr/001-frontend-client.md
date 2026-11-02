# ADR 001: Standalone Blazor WebAssembly Client

## Status

Accepted

## Context

The first ProductCare client prototype was built in React Native/Expo.

For this milestone, delivery time is limited and the highest-risk work sits in the backend: API security boundaries, AI integration, retrieval/grounding behavior, and operational controls.

Keeping React Native in this phase would add frontend complexity and maintenance overhead without materially improving those backend outcomes.

We also want to keep a clean client/API contract so frontend technology can change later without forcing backend refactoring.

## Decision

Use standalone Blazor WebAssembly for the current ProductCare client.

`ProductCare.Web` communicates with `ProductCare.Api` strictly via HTTPS/JSON and OAuth bearer tokens.

`ProductCare.Web` has no project reference to:

- ProductCare.Application
- ProductCare.Domain
- ProductCare.Infrastructure
- ProductCare.AI

Business rules, authorization, and data access remain behind `ProductCare.Api`.

## Authentication

`ProductCare.Web` is a public OAuth client.

Authentication is handled through Microsoft Entra External ID using Authorization Code + PKCE.

The client requests delegated `access_as_user` and sends the access token to `ProductCare.Api`.

`ProductCare.Api` validates tokens and enforces authorization independently of any UI behavior.

## Replaceability

The backend is intentionally not coupled to Blazor.

A future React or React Native client should be able to replace `ProductCare.Web` by:

1. authenticating against the same identity provider
2. requesting the same API permission scope
3. calling the same versioned `ProductCare.Api` endpoints

Replacing the frontend should not require changes in Domain, Application, Infrastructure, or AI layers.

## Tradeoff

Blazor WebAssembly is not the final mobile strategy.

For this milestone, it is the shortest path to a maintainable client while preserving a real client/API boundary and keeping engineering effort focused on backend correctness.

The non-negotiable constraint is that `ProductCare.Web` remains an API client only and never becomes a shortcut into backend implementation code.
