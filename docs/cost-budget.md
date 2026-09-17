\# Cost Notes



The demo environment is intentionally designed for very low traffic.



My target is to keep the portfolio deployment below approximately $25/month

during normal low usage.



The main cost controls are:



\- authenticated access only

\- hard guest quotas

\- scale-to-zero hosting where practical

\- use of cheaper models for routing/simple tasks

\- deterministic API calls where an LLM is unnecessary

\- only invoke specialist agents required for the request

\- token and tool-call limits



Azure budget alerts will be configured before the demo environment is made

available externally.



This is a design target, not a guaranteed Azure bill.

