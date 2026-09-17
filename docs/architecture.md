\# Architecture



\## Client



The client is a React Native app built with Expo.



I chose React Native because the main workflows are photo-first and mobile

use makes more sense than a desktop-only interface.



The client does not call Azure AI services directly. It only calls the

ASP.NET Core API.



\## Backend



The backend is an ASP.NET Core Web API.



All authentication, authorization, quotas, AI orchestration, storage access,

and external API calls happen here.



I am keeping the backend as a modular monolith for the first version.

Separate microservices would add deployment and operational cost without

providing useful value at this stage.



\## AI



The initial agent design contains:



\- Product identification

\- Documentation retrieval

\- Recall/safety

\- Maintenance



A coordinator routes a request only to the required specialist instead of

calling every agent.



This is mainly a cost and latency decision.



\## Data flow



React Native

&#x20;   -> ASP.NET Core API

&#x20;       -> ProductCare AI layer

&#x20;       -> Azure AI Search

&#x20;       -> Azure AI services

&#x20;       -> external tools such as the CPSC API



Privileged Azure credentials never exist in the mobile application.

