import { Navigate, Outlet, useLocation } from 'react-router'
import type { LoginLocationState } from './authContext'
import { useAuth } from './useAuth'

/**
 * Login and Register. A logged-in user is sent on to the app. This is also what moves the
 * user forward right after a successful login, back to the page they originally asked for.
 */
export function PublicOnlyRoute() {
    const { status } = useAuth()
    const location = useLocation()

    if (status !== 'anonymous') {
        const from = (location.state as LoginLocationState | null)?.from
        return <Navigate to={from && from !== '/login' && from !== '/register' ? from : '/tasks'} replace />
    }

    return <Outlet />
}
