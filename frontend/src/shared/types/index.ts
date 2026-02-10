// ── Enums ──

export enum ProtocolType {
  REST = 1,
  SOAP = 2,
  gRPC = 3,
  GraphQL = 4,
}

export enum FieldSourceType {
  Body = 1,
  Header = 2,
  Query = 3,
  Path = 4,
  Metadata = 5,
}

export enum MatchOperator {
  Equals = 1,
  Contains = 2,
  Regex = 3,
  StartsWith = 4,
  EndsWith = 5,
  GreaterThan = 6,
  LessThan = 7,
  Exists = 8,
  NotEquals = 9,
  JsonSchema = 10,
  IsEmpty = 11,
  NotExists = 12,
}

export enum FaultType {
  None = 0,
  FixedDelay = 1,
  RandomDelay = 2,
  ConnectionReset = 3,
  EmptyResponse = 4,
  MalformedResponse = 5,
  Timeout = 6,
}

export type LogicMode = 'AND' | 'OR';

// ── Label Maps ──

export const ProtocolTypeLabel: Record<ProtocolType, string> = {
  [ProtocolType.REST]: 'REST',
  [ProtocolType.SOAP]: 'SOAP',
  [ProtocolType.gRPC]: 'gRPC',
  [ProtocolType.GraphQL]: 'GraphQL',
};

export const FieldSourceTypeLabel: Record<FieldSourceType, string> = {
  [FieldSourceType.Body]: 'Body',
  [FieldSourceType.Header]: 'Header',
  [FieldSourceType.Query]: 'Query',
  [FieldSourceType.Path]: 'Path',
  [FieldSourceType.Metadata]: 'Metadata',
};

export const MatchOperatorLabel: Record<MatchOperator, string> = {
  [MatchOperator.Equals]: 'Equals',
  [MatchOperator.Contains]: 'Contains',
  [MatchOperator.Regex]: 'Regex',
  [MatchOperator.StartsWith]: 'StartsWith',
  [MatchOperator.EndsWith]: 'EndsWith',
  [MatchOperator.GreaterThan]: 'GreaterThan',
  [MatchOperator.LessThan]: 'LessThan',
  [MatchOperator.Exists]: 'Exists',
  [MatchOperator.NotEquals]: 'NotEquals',
  [MatchOperator.JsonSchema]: 'JsonSchema',
  [MatchOperator.IsEmpty]: 'IsEmpty',
  [MatchOperator.NotExists]: 'NotExists',
};

export const FaultTypeLabel: Record<FaultType, string> = {
  [FaultType.None]: 'None',
  [FaultType.FixedDelay]: 'Fixed Delay',
  [FaultType.RandomDelay]: 'Random Delay',
  [FaultType.ConnectionReset]: 'Connection Reset',
  [FaultType.EmptyResponse]: 'Empty Response',
  [FaultType.MalformedResponse]: 'Malformed Response',
  [FaultType.Timeout]: 'Timeout',
};

// ── Entity Types ──

export interface MockEndpoint {
  id: string;
  name: string;
  serviceName: string;
  protocol: ProtocolType;
  path: string;
  httpMethod: string;
  defaultResponse: string | null;
  defaultStatusCode: number | null;
  protocolSettings: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  rules: MockRule[];
}

export interface MockRule {
  id: string;
  endpointId: string;
  ruleName: string;
  priority: number;
  matchConditions: string;
  responseStatusCode: number;
  responseBody: string;
  responseHeaders: string | null;
  delayMs: number;
  isActive: boolean;
  isTemplate: boolean;
  isResponseHeadersTemplate: boolean;
  faultType: FaultType;
  faultConfig: string | null;
  logicMode: number;
  createdAt: string;
  updatedAt: string;
}

export interface MatchCondition {
  sourceType: FieldSourceType;
  fieldPath: string;
  operator: MatchOperator;
  value: string;
}

/** Normalize PascalCase keys from backend to camelCase MatchCondition[] */
export function parseMatchConditions(raw: string): MatchCondition[] {
  try {
    const arr = JSON.parse(raw) as Record<string, unknown>[];
    return arr.map((c) => ({
      sourceType: (c.sourceType ?? c.SourceType) as FieldSourceType,
      fieldPath: (c.fieldPath ?? c.FieldPath) as string,
      operator: (c.operator ?? c.Operator) as MatchOperator,
      value: (c.value ?? c.Value) as string,
    }));
  } catch {
    return [];
  }
}

export interface MockRequestLog {
  id: string;
  endpointId: string | null;
  ruleId: string | null;
  timestamp: string;
  method: string;
  path: string;
  queryString: string | null;
  headers: string | null;
  body: string | null;
  responseStatusCode: number;
  responseBody: string | null;
  responseTimeMs: number;
  isMatched: boolean;
  isProxied: boolean;
  proxyTargetUrl: string | null;
  faultTypeApplied: FaultType | null;
}

export interface ProtocolSchema {
  protocol: ProtocolType;
  name: string;
  description: string;
  supportedSources: FieldSourceType[];
  supportedOperators: MatchOperator[];
  exampleFieldPaths: Record<string, string>;
}

// ── Request DTOs ──

export interface CreateEndpointRequest {
  name: string;
  serviceName: string;
  protocol: ProtocolType;
  path: string;
  httpMethod: string;
  protocolSettings?: string | null;
}

export interface UpdateEndpointRequest {
  name: string;
  serviceName: string;
  path: string;
  httpMethod: string;
  protocolSettings?: string | null;
}

export interface SetDefaultResponseRequest {
  statusCode: number;
  responseBody: string;
}

export interface CreateRuleRequest {
  ruleName: string;
  priority?: number;
  conditions: MatchCondition[];
  statusCode?: number;
  responseBody: string;
  responseHeaders?: Record<string, string>;
  delayMs?: number;
  isTemplate?: boolean;
  isResponseHeadersTemplate?: boolean;
  faultType?: FaultType;
  faultConfig?: string | null;
  logicMode?: number;
}

// ── Template Preview ──

export interface TemplatePreviewRequest {
  template: string;
  mockRequest: {
    method: string;
    path: string;
    body: string;
    headers: Record<string, string>;
    query: Record<string, string>;
    pathParams: Record<string, string>;
  };
}

export interface TemplatePreviewResponse {
  rendered: string;
  error: string | null;
}

// ── Service Proxy ──

export interface ServiceProxy {
  id: string;
  serviceName: string;
  targetBaseUrl: string;
  isActive: boolean;
  isRecording: boolean;
  forwardHeaders: boolean;
  additionalHeaders: string | null;
  timeoutMs: number;
  stripPathPrefix: string | null;
  fallbackEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServiceProxyRequest {
  serviceName: string;
  targetBaseUrl: string;
  isRecording?: boolean;
  forwardHeaders?: boolean;
  additionalHeaders?: string | null;
  timeoutMs?: number;
  stripPathPrefix?: string | null;
  fallbackEnabled?: boolean;
}

export interface ServiceInfo {
  serviceName: string;
  endpointCount: number;
  hasProxy: boolean;
}

// ── Scenarios ──

export interface Scenario {
  id: string;
  name: string;
  description: string | null;
  initialState: string;
  currentState: string;
  isActive: boolean;
  steps: ScenarioStep[];
  createdAt: string;
  updatedAt: string;
}

export interface ScenarioStep {
  id: string;
  scenarioId: string;
  stateName: string;
  endpointId: string;
  matchConditions: string | null;
  responseStatusCode: number;
  responseBody: string;
  responseHeaders: string | null;
  isTemplate: boolean;
  delayMs: number;
  nextState: string | null;
  priority: number;
}

export interface CreateScenarioRequest {
  name: string;
  description?: string;
  initialState: string;
}

export interface UpdateScenarioRequest {
  name: string;
  description?: string;
  initialState: string;
}

export interface CreateStepRequest {
  stateName: string;
  endpointId: string;
  matchConditions?: MatchCondition[];
  responseStatusCode: number;
  responseBody: string;
  responseHeaders?: Record<string, string>;
  isTemplate?: boolean;
  delayMs?: number;
  nextState?: string | null;
  priority?: number;
}
