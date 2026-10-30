# ProductCare Cost Budget

## Target

The demo environment is intended for low traffic and cost should remain low.

Actual cost will depend mainly on model usage and which Azure resources require continuous capacity.

## Cost Principles

The current implementation should favor:

- consumption or scale-to-zero services where practical
- lower-cost models when they meet the quality requirement
- deterministic APIs instead of model calls when possible
- only invoking AI capabilities required for the request
- explicit model-output limits
- explicit agent and tool-call limits
- authenticated access
- per-user quotas
- rate limiting

## AI Usage

AI inference is expected to be one of the most variable costs.

The application should not use a model for work that can be handled more reliably and cheaply through normal code or an authoritative data source.

Examples:

- CPSC recall information should come from the recall data source rather than model memory
- every request should not automatically invoke every specialist
- output-token limits should be appropriate to the task
- simple validation and routing decisions should remain deterministic where possible

Model selection should be based on measured quality for the task rather than always using the most capable model.

## Azure AI Search

Search pricing needs to be reviewed against the demo budget before deployment.

A continuously running paid tier can dominate the monthly cost of a low-traffic portfolio application.

The final configuration should use the lowest option that meets the retrieval requirements, or keep the resource out of the always-on demo environment until it is needed.

## Budget Protection

Before public demo access is enabled:

- configure an Azure budget
- configure cost alerts
- enforce application quotas
- apply API rate limits
- cap model output
- cap agent/tool iterations
- provide a way to disable AI execution quickly

## Production

The current budget applies only to the portfolio environment.

It should not be treated as a production cost estimate.

Production cost would need to be modeled against actual traffic, model usage, availability requirements, storage, search capacity, and operational requirements.
