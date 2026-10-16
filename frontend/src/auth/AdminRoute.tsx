import { Outlet } from 'react-router'
import AccessDeniedPage from '../pages/AccessDeniedPage'
import { useAuth } from './useAuth'

/**
 * Admin pages. Sits inside ProtectedRoute, so the user is already known here.
 * A normal user who types an admin URL stays on that URL and sees "Access denied"
 * instead of being silently redirected. The API enforces the same rule with 403.
 */
export function AdminRoute() {
    const { user } = useAuth()
    return user?.role === 'Admin' ? <Outlet /> : <AccessDeniedPage />
}
