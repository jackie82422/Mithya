# Mithya Mock Server - Feature Enhancement Design Overview

## Background

Based on competitive analysis against WireMock, MockServer, Mockoon, Prism, Hoverfly, Smocker, and MSW, the following features are identified as high-priority enhancements to close gaps with industry leaders while building on Mithya's existing strengths (Web UI, i18n, multi-protocol, Dashboard monitoring).

## Priority Roadmap

### P0 - Response Template Engine (Handlebars)

**Gap**: All major competitors have dynamic response generation. This is Mithya's biggest functional gap.

| Aspect | Description |
|--------|-------------|
| **What** | Allow response bodies to contain Handlebars templates with access to request context (path params, headers, query, body, built-in helpers) |
| **Why** | Every competitor has this: WireMock (Handlebars), MockServer (Mustache+Velocity+JS), Mockoon (Handlebars+Faker.js) |
| **Impact** | Transforms Mithya from static mock to dynamic mock, unlocking 80% of advanced use cases |
| **Backend** | New `TemplateEngine` service, modify `ResponseRenderer`, add `isTemplate` flag to `MockRule` |
| **Frontend** | Template toggle in RuleForm, template variable reference panel, live preview |

### P1 - Fault Injection

**Gap**: WireMock has 5 fault types, MockServer supports drop/random bytes. Mithya only has fixed delay.

| Aspect | Description |
|--------|-------------|
| **What** | Support random delay distribution, connection abort, empty response, malformed response, HTTP error injection |
| **Why** | Essential for resilience testing, chaos engineering |
| **Impact** | Enables teams to test error handling paths |
| **Backend** | New `FaultType` enum, modify `ResponseRenderer`, add fault config to `MockRule` |
| **Frontend** | Fault injection section in RuleForm with type selector and parameters |

### P1 - Proxy / Record & Playback

**Gap**: WireMock, Hoverfly, Mockoon all support recording real API traffic.

| Aspect | Description |
|--------|-------------|
| **What** | Forward unmatched requests to a real API, optionally record responses as new rules |
| **Why** | Dramatically reduces mock setup time, enables capture-based testing |
| **Impact** | New workflow: point at real API -> record -> disconnect -> replay |
| **Backend** | New `ProxyConfig` entity, `ProxyMiddleware`, recording pipeline |
| **Frontend** | New Proxy Config page/module, recording controls, recorded rule review |

### P2 - Stateful Scenarios

**Gap**: WireMock has Scenarios (state machine), Hoverfly has stateful matching.

| Aspect | Description |
|--------|-------------|
| **What** | Define state machines where the same endpoint returns different responses based on prior interactions |
| **Why** | Needed for testing multi-step workflows (login -> authenticated -> expired) |
| **Impact** | Enables realistic testing of stateful APIs |
| **Backend** | New `Scenario`, `ScenarioState` entities, state tracking in-memory |
| **Frontend** | Scenario builder page with state flow visualization |

### P2 - Enhanced Request Matching

**Gap**: WireMock/MockServer have JSONPath, XPath, JSON Schema matching.

| Aspect | Description |
|--------|-------------|
| **What** | Full JSONPath query support, JSON Schema validation, AND/OR logic toggle |
| **Why** | Current `$.field` path is basic; competitors support complex nested queries |
| **Impact** | Handles complex request structures without workarounds |
| **Backend** | Upgrade JSONPath library, add `JsonSchema` operator, add `LogicMode` to rules |
| **Frontend** | JSONPath autocomplete hints, logic mode toggle (AND/OR) |

---

## Design Documents

| Document | Target Team | Content |
|----------|-------------|---------|
| [Backend Design](./backend-design.md) | Backend (.NET) | Database schema, API contracts, engine extensions, middleware changes |
| [Frontend Design](./frontend-design.md) | Frontend (React) | New components, module structure, form designs, i18n keys |

## Architecture Principles

1. **Backward Compatible**: All new features are opt-in; existing endpoints/rules continue to work unchanged
2. **Follow Existing Patterns**: Use the same Repository/Cache/Engine pipeline; follow module/api/hooks pattern on frontend
3. **Cache-First**: All runtime matching goes through `MockRuleCache`; database is only for persistence
4. **i18n Required**: All new UI text must have both `en` and `zh-TW` translations
5. **Theme Aware**: All new components must support light/dark mode via CSS custom properties
