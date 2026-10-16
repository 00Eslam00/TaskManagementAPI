import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import * as authApi from '../api/auth'
import { setUnauthorizedHandler } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import type { LoginRequest } from '../api/types'
import { getCurrentUser } from '../api/users'
import { AuthContext, type AuthContextValue, type AuthStatus, type SessionEndReason } from './authContext'
import { clearSession, loadSession, saveSession, type Session } from './session'

export function AuthProvider({ children }: { children: ReactNode }) {
    const queryClient = useQueryClient()

    // Read once on startup: a token that is still valid keeps the user logged in after a refresh.
    const [session, setSession] = useState<Session | null>(() => loadSession())
    const [sessionEndReason, setSessionEndReason] = useState<SessionEndReason | null>(null)

    const sessionRef = useRef(session)
    useEffect(() => {
        sessionRef.current = session
    }, [session])

    // The header shows the user from the "get current user" endpoint.
    const meQuery = useQuery({
        queryKey: queryKeys.me,
        queryFn: getCurrentUser,
        enabled: session !== null,
        staleTime: Infinity,
    })

    // No navigation here: once the session is gone, ProtectedRoute redirects to the login page
    // and passes the reason along. Navigating here as well would race with that redirect.
    const endSession = useCallback(
        (reason: SessionEndReason) => {
            sessionRef.current = null
            clearSession()
            setSessionEndReason(reason)
            setSession(null)
            // Drop every cached response so the next user never sees the previous user's data.
            queryClient.clear()
        },
        [queryClient],
    )

    useEffect(() => {
        setUnauthorizedHandler(() => {
            // Several requests can fail with 401 at once; only the first one ends the session.
            if (!sessionRef.current) return
            endSession('expired')
        })
        return () => setUnauthorizedHandler(null)
    }, [endSession])

    const login = useCallback(
        async (request: LoginRequest) => {
            const auth = await authApi.login(request)
            queryClient.removeQueries({ queryKey: queryKeys.me })
            const newSession = saveSession(auth)
            sessionRef.current = newSession
            setSessionEndReason(null)
            setSession(newSession)
        },
        [queryClient],
    )

    const logout = useCallback(async () => {

        sessionRef.current = null
        try {
            await authApi.logout()
        } catch {

        }
        endSession('loggedOut')
    }, [endSession])

    const status: AuthStatus = !session
        ? 'anonymous'
        : meQuery.isPending
            ? 'loading'
            : meQuery.isError
                ? 'error'
                : 'authenticated'

    const { data: user, refetch } = meQuery
    const value = useMemo<AuthContextValue>(
        () => ({
            status,
            user: status === 'authenticated' ? (user ?? null) : null,
            sessionEndReason,
            login,
            logout,
            retry: () => void refetch(),
        }),
        [status, user, sessionEndReason, login, logout, refetch],
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
