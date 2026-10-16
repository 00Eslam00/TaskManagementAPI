import { createContext } from 'react'
import type { LoginRequest, UserResponse } from '../api/types'

/**
 * - anonymous: no valid token, only Login and Register are reachable
 * - loading: a token exists and the current user is being loaded from /api/users/me
 * - authenticated: the current user is known
 * - error: a token exists but /api/users/me failed for a reason other than 401 (server down)
 */
export type AuthStatus = 'anonymous' | 'loading' | 'authenticated' | 'error'

/** Why the last session ended, so the login page can explain it. */
export type SessionEndReason = 'expired' | 'loggedOut'

export interface AuthContextValue {
    status: AuthStatus
    user: UserResponse | null
    sessionEndReason: SessionEndReason | null
    login: (request: LoginRequest) => Promise<void>
    logout: () => Promise<void>
    retry: () => void
}

/** Router state passed to the login page. */
export interface LoginLocationState {
    /** Where to go after logging in. */
    from?: string
    reason?: 'expired'
}

export const AuthContext = createContext<AuthContextValue | null>(null)
