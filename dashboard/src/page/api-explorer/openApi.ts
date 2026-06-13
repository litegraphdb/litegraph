import { ApiOperation, ApiParameter } from './types';
import { getRequestBodyTemplate } from './requestTemplates';

type OpenApiSchema = { [key: string]: unknown };

type OpenApiSpec = {
  paths?: Record<string, Record<string, OpenApiOperationDef>>;
  components?: { schemas?: Record<string, OpenApiSchema> };
};

type OpenApiOperationDef = {
  summary?: string;
  description?: string;
  tags?: string[];
  parameters?: ApiParameter[];
  requestBody?: {
    content?: Record<string, { schema?: OpenApiSchema; example?: unknown }>;
  };
};

const METHODS = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

const isBulkCreatePath = (path: string, method: string): boolean => (
  method === 'put' &&
  (
    /\/labels\/bulk$/i.test(path) ||
    /\/tags\/bulk$/i.test(path) ||
    /\/vectors\/bulk$/i.test(path) ||
    /\/nodes\/bulk$/i.test(path) ||
    /\/edges\/bulk$/i.test(path)
  )
);

const exampleFromSchema = (
  schema: OpenApiSchema | undefined,
  components: Record<string, OpenApiSchema> | undefined,
  seen: Set<string> = new Set()
): unknown => {
  if (!schema) return null;
  const ref = (schema as { $ref?: string }).$ref;
  if (ref && components) {
    const name = ref.replace('#/components/schemas/', '');
    if (seen.has(name)) return null;
    seen.add(name);
    return exampleFromSchema(components[name], components, seen);
  }
  if ((schema as { example?: unknown }).example !== undefined) {
    return (schema as { example: unknown }).example;
  }
  const type = (schema as { type?: string }).type;
  if (type === 'string') return '';
  if (type === 'integer' || type === 'number') return 0;
  if (type === 'boolean') return false;
  if (type === 'array') {
    const items = (schema as { items?: OpenApiSchema }).items;
    return [exampleFromSchema(items, components, seen)];
  }
  if (type === 'object' || (schema as { properties?: unknown }).properties) {
    const properties = (schema as { properties?: Record<string, OpenApiSchema> }).properties || {};
    const out: Record<string, unknown> = {};
    for (const key of Object.keys(properties)) {
      out[key] = exampleFromSchema(properties[key], components, seen);
    }
    return out;
  }
  return null;
};

export const flattenOpenApi = (spec: OpenApiSpec | null | undefined): ApiOperation[] => {
  if (!spec?.paths) return [];
  const components = spec.components?.schemas;
  const ops: ApiOperation[] = [];
  for (const [path, methods] of Object.entries(spec.paths)) {
    for (const method of METHODS) {
      const def = (methods as Record<string, OpenApiOperationDef>)[method];
      if (!def) continue;

      const parameters: ApiParameter[] = (def.parameters || []).map((p) => ({
        name: p.name,
        in: p.in,
        required: p.required,
        description: p.description,
        schema: p.schema,
      }));

      if (isBulkCreatePath(path, method) && !parameters.some((p) => p.in === 'query' && p.name === 'return')) {
        parameters.push({
          name: 'return',
          in: 'query',
          required: false,
          description: 'Bulk create response shape. Use full for the existing response or minimal to skip optional node/edge response hydration.',
          schema: { type: 'string', enum: ['full', 'minimal'] },
        });
      }

      let requestBodyExample: string | undefined;
      let hasBody = false;
      if (def.requestBody?.content) {
        hasBody = true;
        const json = def.requestBody.content['application/json'];
        if (json) {
          if (json.example !== undefined) {
            requestBodyExample = JSON.stringify(json.example, null, 2);
          } else if (json.schema) {
            const sample = exampleFromSchema(json.schema, components);
            if (sample !== null && sample !== undefined) {
              requestBodyExample = JSON.stringify(sample, null, 2);
            }
          }
        }
      }

      requestBodyExample =
        getRequestBodyTemplate(path, method.toUpperCase()) || requestBodyExample;

      const tag = def.tags?.[0] || 'Other';
      ops.push({
        id: `${method.toUpperCase()} ${path}`,
        method: method.toUpperCase(),
        path,
        summary: def.summary,
        description: def.description,
        tag,
        parameters,
        requestBodyExample,
        hasRequestBody: hasBody,
      });
    }
  }
  ops.sort((a, b) => (a.tag === b.tag ? a.path.localeCompare(b.path) : a.tag.localeCompare(b.tag)));
  return ops;
};

export const buildRequestUrl = (
  baseUrl: string,
  operation: ApiOperation,
  pathValues: Record<string, string>,
  queryValues: Record<string, string>
): string => {
  let path = operation.path;
  for (const p of operation.parameters.filter((x) => x.in === 'path')) {
    const v = pathValues[p.name] ?? '';
    path = path.replace(new RegExp(`\\{${p.name}\\}`, 'g'), encodeURIComponent(v));
  }
  const queryEntries = Object.entries(queryValues).filter(([, v]) => v !== '' && v !== undefined);
  const queryString = queryEntries
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
    .join('&');
  const normalizedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
  return `${normalizedBase}${path}${queryString ? `?${queryString}` : ''}`;
};
