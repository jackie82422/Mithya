import yaml from 'js-yaml';

// ── Types ──

export interface ParsedEndpoint {
  key: string;
  name: string;
  serviceName: string;
  path: string;
  httpMethod: string;
  defaultStatusCode: number;
  defaultResponseBody: string;
  selected: boolean;
}

export interface OpenApiParseResult {
  endpoints: ParsedEndpoint[];
  title: string;
  version: string;
  specVersion: 'swagger2' | 'openapi3';
  warnings: string[];
}

// ── Internal helpers ──

type SchemaObj = Record<string, unknown>;

const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options', 'trace'];

function resolveRef(root: SchemaObj, ref: string): SchemaObj | undefined {
  // e.g. "#/definitions/Pet" or "#/components/schemas/Pet"
  if (!ref.startsWith('#/')) return undefined;
  const parts = ref.slice(2).split('/');
  let cur: unknown = root;
  for (const p of parts) {
    if (cur == null || typeof cur !== 'object') return undefined;
    cur = (cur as SchemaObj)[p];
  }
  return cur as SchemaObj | undefined;
}

function generateFromSchema(
  root: SchemaObj,
  schema: SchemaObj | undefined,
  visited: Set<string>,
  depth: number,
): unknown {
  if (!schema || depth > 5) return null;

  // Handle $ref
  if (typeof schema.$ref === 'string') {
    if (visited.has(schema.$ref)) return null;
    visited.add(schema.$ref);
    const resolved = resolveRef(root, schema.$ref);
    const result = generateFromSchema(root, resolved, visited, depth + 1);
    visited.delete(schema.$ref);
    return result;
  }

  // Handle example
  if (schema.example !== undefined) return schema.example;

  // Handle allOf
  if (Array.isArray(schema.allOf)) {
    const merged: SchemaObj = {};
    const mergedProps: SchemaObj = {};
    for (const sub of schema.allOf) {
      const generated = generateFromSchema(root, sub as SchemaObj, visited, depth + 1);
      if (generated && typeof generated === 'object' && !Array.isArray(generated)) {
        Object.assign(mergedProps, generated);
      }
    }
    return Object.keys(mergedProps).length > 0 ? mergedProps : merged;
  }

  // Handle oneOf / anyOf — take the first
  if (Array.isArray(schema.oneOf) && schema.oneOf.length > 0) {
    return generateFromSchema(root, schema.oneOf[0] as SchemaObj, visited, depth + 1);
  }
  if (Array.isArray(schema.anyOf) && schema.anyOf.length > 0) {
    return generateFromSchema(root, schema.anyOf[0] as SchemaObj, visited, depth + 1);
  }

  const type = schema.type as string | undefined;

  if (type === 'object' || schema.properties) {
    const props = (schema.properties ?? {}) as SchemaObj;
    const result: Record<string, unknown> = {};
    for (const [key, val] of Object.entries(props)) {
      result[key] = generateFromSchema(root, val as SchemaObj, visited, depth + 1);
    }
    return result;
  }

  if (type === 'array') {
    const items = schema.items as SchemaObj | undefined;
    const item = generateFromSchema(root, items, visited, depth + 1);
    return [item];
  }

  if (type === 'string') {
    if (Array.isArray(schema.enum) && schema.enum.length > 0) return schema.enum[0];
    const format = schema.format as string | undefined;
    if (format === 'date-time') return '2024-01-01T00:00:00Z';
    if (format === 'date') return '2024-01-01';
    if (format === 'email') return 'user@example.com';
    if (format === 'uuid') return '00000000-0000-0000-0000-000000000000';
    if (format === 'uri' || format === 'url') return 'https://example.com';
    return 'string';
  }

  if (type === 'integer' || type === 'number') {
    if (Array.isArray(schema.enum) && schema.enum.length > 0) return schema.enum[0];
    return 0;
  }

  if (type === 'boolean') return true;

  return null;
}

function pickBestStatusCode(responses: SchemaObj): string {
  const codes = Object.keys(responses);
  for (const c of ['200', '201', '202', '204']) {
    if (codes.includes(c)) return c;
  }
  if (codes.includes('default')) return 'default';
  // Return the first 2xx code found
  const twoXx = codes.find((c) => c.startsWith('2'));
  if (twoXx) return twoXx;
  return codes[0] ?? '200';
}

function extractResponseBody(
  root: SchemaObj,
  responses: SchemaObj,
  statusCode: string,
  isSwagger2: boolean,
): string {
  const resp = responses[statusCode] as SchemaObj | undefined;
  if (!resp) return '{}';

  if (isSwagger2) {
    // Swagger 2: examples["application/json"] → schema.example → generate from schema
    const examples = resp.examples as SchemaObj | undefined;
    if (examples?.['application/json'] !== undefined) {
      return JSON.stringify(examples['application/json'], null, 2);
    }
    const schema = resp.schema as SchemaObj | undefined;
    if (schema?.example !== undefined) {
      return JSON.stringify(schema.example, null, 2);
    }
    if (schema) {
      const generated = generateFromSchema(root, schema, new Set(), 0);
      if (generated !== null) return JSON.stringify(generated, null, 2);
    }
  } else {
    // OpenAPI 3: content["application/json"]
    const content = resp.content as SchemaObj | undefined;
    const jsonContent = content?.['application/json'] as SchemaObj | undefined;
    if (jsonContent) {
      if (jsonContent.example !== undefined) {
        return JSON.stringify(jsonContent.example, null, 2);
      }
      const schema = jsonContent.schema as SchemaObj | undefined;
      if (schema?.example !== undefined) {
        return JSON.stringify(schema.example, null, 2);
      }
      if (schema) {
        const generated = generateFromSchema(root, schema, new Set(), 0);
        if (generated !== null) return JSON.stringify(generated, null, 2);
      }
    }
  }

  return '{}';
}

function pathToName(method: string, path: string): string {
  const parts = path
    .split('/')
    .filter(Boolean)
    .map((seg) => {
      // Remove path params like {id}
      if (seg.startsWith('{') && seg.endsWith('}')) {
        return 'By ' + capitalize(seg.slice(1, -1));
      }
      return capitalize(seg);
    });
  return capitalize(method) + ' ' + parts.join(' ');
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

// ── Main parser ──

export function parseOpenApiSpec(input: string): OpenApiParseResult {
  let doc: SchemaObj;

  // 1. Format detection: JSON first, then YAML
  try {
    doc = JSON.parse(input) as SchemaObj;
  } catch {
    try {
      doc = yaml.load(input) as SchemaObj;
    } catch {
      throw new Error('PARSE_ERROR');
    }
  }

  if (!doc || typeof doc !== 'object') {
    throw new Error('INVALID_SPEC');
  }

  // 2. Version detection
  const isSwagger2 = typeof doc.swagger === 'string' && doc.swagger.startsWith('2');
  const isOpenApi3 = typeof doc.openapi === 'string' && (doc.openapi as string).startsWith('3');

  if (!isSwagger2 && !isOpenApi3) {
    throw new Error('INVALID_SPEC');
  }

  const specVersion: 'swagger2' | 'openapi3' = isSwagger2 ? 'swagger2' : 'openapi3';
  const info = (doc.info ?? {}) as SchemaObj;
  const title = (info.title as string) ?? 'Untitled';
  const version = (info.version as string) ?? '';
  const warnings: string[] = [];

  // 3. Base path
  let basePath = '';
  if (isSwagger2) {
    basePath = ((doc.basePath as string) ?? '').replace(/\/$/, '');
  } else {
    const servers = doc.servers as SchemaObj[] | undefined;
    if (servers && servers.length > 0) {
      try {
        const serverUrl = servers[0].url as string;
        // Extract path portion from URL
        if (serverUrl.startsWith('http://') || serverUrl.startsWith('https://')) {
          const url = new URL(serverUrl);
          basePath = url.pathname.replace(/\/$/, '');
        } else if (serverUrl.startsWith('/')) {
          basePath = serverUrl.replace(/\/$/, '');
        }
      } catch {
        // ignore invalid server URL
      }
    }
  }

  // 4. Iterate paths
  const paths = (doc.paths ?? {}) as SchemaObj;
  const endpoints: ParsedEndpoint[] = [];

  for (const [pathKey, pathItem] of Object.entries(paths)) {
    if (!pathItem || typeof pathItem !== 'object') continue;
    const pathObj = pathItem as SchemaObj;

    for (const method of HTTP_METHODS) {
      const operation = pathObj[method] as SchemaObj | undefined;
      if (!operation) continue;

      const httpMethod = method.toUpperCase();
      const fullPath = basePath + pathKey;

      // Name
      const operationId = operation.operationId as string | undefined;
      const name = operationId ?? pathToName(method, pathKey);

      // Service name
      const tags = operation.tags as string[] | undefined;
      const serviceName = tags?.[0] ?? title;

      // Response
      const responses = (operation.responses ?? {}) as SchemaObj;
      const bestCode = pickBestStatusCode(responses);
      const defaultStatusCode = bestCode === 'default' ? 200 : parseInt(bestCode, 10);
      const defaultResponseBody = extractResponseBody(doc, responses, bestCode, isSwagger2);

      endpoints.push({
        key: `${httpMethod}:${fullPath}`,
        name,
        serviceName,
        path: fullPath,
        httpMethod,
        defaultStatusCode,
        defaultResponseBody,
        selected: true,
      });
    }
  }

  if (endpoints.length === 0) {
    throw new Error('NO_ENDPOINTS');
  }

  return { endpoints, title, version, specVersion, warnings };
}
