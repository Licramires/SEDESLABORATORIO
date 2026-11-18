// La actualización conserva la última consulta si la red falla o deja de responder.
export async function requestJson(url, fetcher = fetch, timeoutMs = 15000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetcher(url, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' }, signal: controller.signal });
    if (!response.ok || !response.headers.get('content-type')?.toLowerCase().includes('application/json')) throw new Error('Respuesta no disponible o sesión caducada.');
    return await response.json();
  } finally {
    clearTimeout(timeout);
  }
}
