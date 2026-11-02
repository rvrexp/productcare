# ProductCare Cost Budget

## Target

This budget is for the portfolio/demo environment, which is expected to run at low traffic and low spend.

In practice, spend is driven mostly by model inference volume and any Azure services that require fixed, always-on capacity.

## Cost Principles

Implementation should prioritize:

- consumption or scale-to-zero services where practical
- lower-cost models when they meet measured quality bars
- deterministic APIs and code paths instead of model calls when possible
- invoking only the AI capability required for the active request
- explicit output-token limits
- explicit agent/tool iteration limits
- authenticated access
- per-user quotas
- rate limiting

## AI Usage

Inference is the most volatile cost line item.

We should not use a model for tasks that are cheaper and more reliable in deterministic code or from an authoritative source.

Examples:

- CPSC recall status must come from the recall source, not model memory
- requests should not fan out to every specialist by default
- output-token limits should be tuned to each task
- simple validation and routing should stay deterministic where possible

Model choice should be based on measured task quality and latency/cost tradeoffs, not a default assumption that the largest model is always better.

## Azure AI Search

Search SKU/capacity needs a budget check before enabling in a continuously available demo environment.

For low traffic, an always-on paid tier can easily dominate monthly spend.

Use the lowest configuration that still meets retrieval quality requirements, or keep Search out of the always-on footprint until needed.

## Budget Protection

Before enabling public demo access:

- configure an Azure budget
- configure spend alerts
- enforce application quotas
- apply API rate limits
- cap model output
- cap agent/tool iterations
- provide an operational kill switch for AI execution

## Production

This budget is scoped to the portfolio environment only.

It is not a production estimate.

Production budgeting must be modeled against real traffic, request mix, model selection, SLO/availability targets, storage growth, search capacity, and on-call/operational requirements.
