import type { TryRequestPayload } from '../api/tryRequestApi';

export function parseCurlToPayload(curlCmd: string): TryRequestPayload {
  const normalized = curlCmd.replace(/\\\n\s*/g, ' ').trim();

  let method = 'GET';
  const methodMatch = normalized.match(/-X\s+(\w+)/);
  if (methodMatch) {
    method = methodMatch[1].toUpperCase();
  }

  let url = '';
  const urlMatch = normalized.match(/curl\s+(?:-[^\s]+\s+)*'([^']+)'/) ||
    normalized.match(/curl\s+(?:-[^\s]+\s+)*"([^"]+)"/) ||
    normalized.match(/curl\s+(?:-[^\s]+\s+)*(https?:\/\/\S+)/);
  if (urlMatch) {
    url = urlMatch[1];
  }

  const headers: Record<string, string> = {};
  const headerRegex = /-H\s+'([^']+)'/g;
  let hMatch;
  while ((hMatch = headerRegex.exec(normalized)) !== null) {
    const colonIdx = hMatch[1].indexOf(':');
    if (colonIdx > 0) {
      const key = hMatch[1].slice(0, colonIdx).trim();
      const val = hMatch[1].slice(colonIdx + 1).trim();
      headers[key] = val;
    }
  }

  let body: string | undefined;
  const bodyMatch = normalized.match(/-d\s+'([^']*)'/) ||
    normalized.match(/-d\s+"([^"]*)"/);
  if (bodyMatch) {
    body = bodyMatch[1];
    if (!methodMatch) {
      method = 'POST';
    }
  }

  return { method, url, headers: Object.keys(headers).length > 0 ? headers : undefined, body };
}
