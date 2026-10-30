# ADR 001: Standalone Blazor WebAssembly Client

## Status

Accepted

## Context

The initial ProductCare client prototype used React Native and Expo.

The current milestone has a limited delivery window, and the main technical work I want to exercise is in the API and AI portions of the system: model integration, retrieval, grounding, evaluation, security, and operational controls.

Keeping React Native for this milestone would add a substantial frontend learning and maintenance surface without improving those parts of the architecture.

At the same time, changing frontend technology should not result in coupling the backend to the replacement client.

## Decision

Use standalone Blazor WebAssembly as the current ProductCare client.

`ProductCare.Web` communicates with `ProductCare.Api` only through HTTPS/JSON and OAuth bearer tokens.

It has no project reference to:

- ProductCare.Application
- ProductCare.Domain
- ProductCare.Infrastructure
- ProductCare.AI

Business logic and authorization remain behind ProductCare.Api.

## Authentication

ProductCare.Web is a public OAuth client.

It authenticates through Microsoft Entra External ID using Authorization Code with PKCE.

The client requests the delegated `access_as_user` scope and sends the resulting token to ProductCare.Api.

ProductCare.Api validates the token and enforces authorization independently.

## Replaceability

The backend does not depend on Blazor.

A future React or React Native client should be able to replace ProductCare.Web by:

1. authenticating against the same identity provider
2. requesting the same API scope
3. calling the same versioned ProductCare.Api endpoints

Replacing the frontend should not require changes to the Domain, Application, Infrastructure, or AI projects.

## Tradeoff

Blazor WebAssembly is not intended to represent the final native mobile experience.

For the current milestone, the choice reduces unnecessary frontend scope while preserving a real client/API boundary.

The main constraint is that ProductCare.Web remains an API client and does not become a shortcut into backend implementation layers.
