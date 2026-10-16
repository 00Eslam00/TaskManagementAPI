import { Navigate, Outlet, useLocation } from 'react-router'
import { Button, Container, Stack } from '@mui/material'
import { ErrorState, FullPageLoader } from '../components/PageStates'
import type { LoginLocationState } from './authContext'
import { useAuth } from './useAuth'

/** Every page except Login and Register goes through here. */
export function ProtectedRoute() {
    const { status, sessionEndReason, retry, logout } = useAuth()
    const location = useLocation()

    if (status === 'anonymous') {
        // After an explicit logout the next login starts fresh at the task list. Otherwise
        // (first visit, expired token) the user comes back to the page they asked for.
        const state: LoginLocationState | undefined =
            sessionEndReason === 'loggedOut'
                ? undefined
                : {
                    from: location.pathname + location.search,
                    reason: sessionEndReason === 'expired' ? 'expired' : undefined,
                }
        return <Navigate to="/login" replace state={state} />
    }

    if (status === 'loading') return <FullPageLoader />

    if (status === 'error') {
        return (
            <Container maxWidth="sm" sx={{ py: 8 }}>
                <Stack spacing={2}>
                    <ErrorState message="Could not load your account. The server may be unavailable." onRetry={retry} />
                    <Button onClick={() => void logout()}>Log out</Button>
                </Stack>
            </Container>
        )
    }

    return <Outlet />
}
