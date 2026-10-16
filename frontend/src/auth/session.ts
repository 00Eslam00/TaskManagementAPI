import type { AuthResponse } from '../api/types'

// The session lives in localStorage so the user stays logged in after a page refresh.
// Trade-off: any script running on the page could read it (XSS); an httpOnly cookie would
// avoid that but needs CSRF protection and backend changes, which is beyond this task.
const STORAGE_KEY = 'taskManagement.session'

export interface Session {
    accessToken: string
    /** UTC expiry of the access token, from the login response. */
    expiresAt: string
}

export function saveSession(auth: AuthResponse): Session {
    const session: Session = { accessToken: auth.accessToken, expiresAt: auth.expiresAt }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    return session
}

/** Returns the stored session, or null if there is none or its token has expired. */
export function loadSession(): Session | null {
    try {
        const raw = localStorage.getItem(STORAGE_KEY)
        if (!raw) return null

        const session = JSON.parse(raw) as Partial<Session>
        if (typeof session.accessToken !== 'string' || typeof session.expiresAt !== 'string') {
            clearSession()
            return null
        }

        if (isExpired(session.expiresAt)) {
            clearSession()
            return null
        }

        return { accessToken: session.accessToken, expiresAt: session.expiresAt }
    } catch {
        // Corrupted value or storage not available (private mode): treat as logged out.
        return null
    }
}

export function clearSession(): void {
    try {
        localStorage.removeItem(STORAGE_KEY)
    } catch {
        // Nothing to clear.
    }
}

function isExpired(expiresAt: string): boolean {
    const expiry = Date.parse(expiresAt)
    return Number.isNaN(expiry) || expiry <= Date.now()
}
