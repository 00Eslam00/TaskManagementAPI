import axios from 'axios'
import { config } from '../config'
import { loadSession } from '../auth/session'

// The single HTTP client for the whole app. Every API module goes through it, so the
// token and the 401 handling live in one place.
export const apiClient = axios.create({
  baseURL: config.apiUrl,
  headers: { 'Content-Type': 'application/json' },
})

// Adds the access token to every request while the user is logged in.
apiClient.interceptors.request.use((request) => {
  const session = loadSession()
  if (session) {
    request.headers.Authorization = `Bearer ${session.accessToken}`
  }
  return request
})

type UnauthorizedHandler = () => void

let onUnauthorized: UnauthorizedHandler | null = null

/** The auth provider registers how to log the user out; the client only reports it. */
export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  onUnauthorized = handler
}

// A 401 on a request that carried a token means the token expired or the user was deleted,
// so the session is ended. A 401 without a token (wrong password on the login form) is
// left to the caller, which shows "Invalid email or password".
apiClient.interceptors.response.use(undefined, (error: unknown) => {
  if (
    axios.isAxiosError(error) &&
    error.response?.status === 401 &&
    error.config?.headers?.Authorization
  ) {
    onUnauthorized?.()
  }
  return Promise.reject(error)
})
