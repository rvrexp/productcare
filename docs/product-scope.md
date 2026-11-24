# ProductCare Scope

## Purpose

ProductCare is an AI-assisted product support application.

The current release delivers a connected workflow rather than attempting to cover the full consumer-product lifecycle. It demonstrates vision-based identification, product-scoped retrieval, recall checks, and application-controlled AI orchestration in a layered .NET solution.

## Product Identification

Users upload an image of a product or model label. ProductCare attempts to identify:

- manufacturer;
- model number;
- product type;
- product name, when the evidence supports it;
- serial number, when visible and supported by the image.

Users review and can correct the detected values before saving. Uncertain fields are not filled with guesses.

## Recall Checking

Recall checking is **on demand**, not an automatic check performed when every product is saved.

On the saved product page, the user can run a U.S. CPSC recall lookup. ProductCare uses the saved manufacturer and model information to find and filter potential matches. The ProductCare Assistant can also invoke the recall capability when a question concerns product safety or recalls.

The UI distinguishes a matching recall, no matching recall, and a source-unavailable result. Missing product identity information prevents a reliable lookup. No matching recall is never presented as a guarantee that the product is safe.

## Product Documentation and Questions

Users can upload a PDF manual for a saved product. The PDF remains excluded from Azure AI Search while Defender for Storage scanning is unresolved. Only a validated clean result permits indexing.

ProductCare retrieves product-specific document chunks and generates grounded answers. The manual-question API and ProductCare Assistant are separate entry points. The assistant can retrieve documentation as part of its controlled tool workflow, cite relevant sources, and use conversation context for follow-up questions.

When retrieved material is missing or insufficient, ProductCare should communicate that limitation rather than invent an answer.

## Maintenance and Troubleshooting

There is no separate maintenance scheduling or service-management module in this release. Maintenance, setup, cartridge replacement, and troubleshooting guidance are supported through ProductCare Assistant using the user's saved product details and available manual content.

## Frontend

The current client is standalone Blazor WebAssembly. It calls `ProductCare.Api` over HTTPS and has no direct references to backend implementation projects.

The API contracts are client-agnostic. A future React or React Native client could use the same versioned endpoints after implementing the required identity flow and permissions.

## Authentication and Access

Customer authentication uses Microsoft Entra External ID. The browser client uses Authorization Code with PKCE, and the API enforces token scope, ProductCare access state, resource ownership, and administrator policies.

Application access is requested and approved manually. Active access can expire or be disabled independently of the Entra sign-in identity. ProductCare currently supports self-service account deletion for authenticated accounts that are not administratively disabled. Administrators can purge a disabled account's data while retaining its blocked account record.

## AI Design

The implementation separates these capabilities:

- product identification;
- documentation retrieval and grounded question answering;
- CPSC recall and safety lookup;
- product-specific maintenance and troubleshooting guidance through the assistant.

ProductCare code defines available tools and executes the capabilities selected by the assistant. Model output is treated as untrusted until validated. Request limits, tool bounds, and retrieval constraints limit unnecessary model work and risk, but do not guarantee output correctness.

## Out of Scope

The current release does not include:

- subscriptions or payments;
- marketplace features;
- technician booking or maintenance scheduling;
- social features;
- voice interaction;
- video processing;
- native mobile packaging;
- production-scale multi-region deployment.

Expanded product features and production-scale operations would require separate requirements, security review, and capacity planning.
