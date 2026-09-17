\# Initial Threat Model



This is not intended to be a complete security assessment. It records the

main risks I want to address while building V1.



\## AI usage abuse



The demo will not expose anonymous AI endpoints.



Interviewer accounts will have request quotas and rate limits so automated

traffic cannot generate unlimited model costs.



\## Cross-user data access



Product and document access must always be scoped to the authenticated user.

The API must not trust a user ID supplied by the mobile client.



\## Uploaded documents



Manuals and images are treated as untrusted input.



Files will be validated before processing, and text extracted from documents

must not be treated as agent instructions.



\## Unsafe troubleshooting



The application should distinguish normal user-serviceable troubleshooting

from higher-risk electrical, gas, battery, or safety-interlock procedures.



High-risk requests should not be handled as ordinary troubleshooting.



\## Credentials



No Azure service credentials are stored in the React Native application.



Managed Identity will be preferred for Azure-to-Azure access once the demo

is deployed.

