# ProductCare Scope

## Purpose

ProductCare is an AI-assisted product support application.

The current milestone focuses on a small number of connected workflows rather than attempting to build a complete consumer-product platform.

The project applies established .NET application design practices to AI-assisted workflows and provides hands-on implementation experience with vision models, retrieval, grounding, evaluation, and agent orchestration.

## Product Identification

A user provides an image of a product or model label.

ProductCare attempts to identify:

- manufacturer
- model number
- product type
- product name when supported by the available evidence

The result should preserve uncertainty.

Missing information should remain missing rather than being filled with an unsupported guess.

## Recall Checking

After a product is identified, ProductCare checks authoritative recall information.

The result should distinguish between:

- a matching recall
- no matching recall found
- insufficient product information to make a reliable match

"No matching recall found" must not be presented as proof that a product is safe.

## Documentation Q&A

ProductCare retrieves relevant product documentation and uses that material to answer product-specific questions.

Answers should be grounded in retrieved source material and include citations where practical.

If the available documentation does not support an answer, the system should say so instead of filling the gap from model knowledge.

## Maintenance

The application will support a limited maintenance workflow based on stored product information.

The portfolio version only needs enough functionality to demonstrate that maintenance guidance can use the same identified product and documentation context.

## Frontend

The current client is standalone Blazor WebAssembly.

It communicates with ProductCare.Api over HTTP and does not reference backend implementation projects.

A future React or React Native client should be able to use the same backend contracts.

## Authentication

Customer authentication uses Microsoft Entra External ID.

The browser client uses Authorization Code with PKCE.

ProductCare.Api remains responsible for authorization and ownership enforcement.

## AI Design

The planned AI workflow includes specialist capabilities for:

- product identification
- documentation retrieval and answering
- recall and safety
- maintenance support

A coordinator should invoke only the capabilities required for the current request.

This avoids unnecessary model calls and gives clearer control over latency, cost, and execution limits.

The AI portions of the project are being implemented and evaluated incrementally. Model behavior is not assumed to be reliable until it is measured against representative scenarios.

## Out of Scope

The current milestone does not include:

- subscriptions or payments
- marketplace functionality
- technician booking
- social features
- voice interaction
- video processing
- native mobile packaging
- production-scale multi-region deployment

These can be reconsidered if the project moves beyond the current portfolio scope.
