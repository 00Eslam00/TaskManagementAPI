// All environment-specific settings are read here, so no component hardcodes an address.
const apiUrl = import.meta.env.VITE_API_URL

if (!apiUrl) {
  throw new Error('VITE_API_URL is not set. Copy .env.example to .env and fill it in.')
}

export const config = {
  // Without a trailing slash, so paths like "/api/tasks" can be appended directly.
  apiUrl: apiUrl.replace(/\/+$/, ''),
}
