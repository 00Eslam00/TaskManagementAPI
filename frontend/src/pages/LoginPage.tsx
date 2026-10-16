import { useState } from 'react'
import { Link as RouterLink, useLocation } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Alert, Button, Link, Stack, TextField, Typography } from '@mui/material'
import type { LoginLocationState } from '../auth/authContext'
import { useAuth } from '../auth/useAuth'
import { AuthCard } from '../components/AuthCard'
import { toApiError } from '../lib/errors'
import { applyFieldErrors } from '../lib/forms'

const loginSchema = z.object({
    email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
    // No length rule here: the login form must accept any password the account already has.
    password: z.string().min(1, 'Password is required'),
})

type LoginForm = z.infer<typeof loginSchema>

export default function LoginPage() {
    const { login } = useAuth()
    const location = useLocation()
    const locationState = location.state as LoginLocationState | null
    const [formError, setFormError] = useState<string | null>(null)

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })

    // On success, PublicOnlyRoute sees the new session and moves the user on.
    const onSubmit = async (values: LoginForm) => {
        setFormError(null)
        try {
            await login(values)
        } catch (error) {
            const apiError = toApiError(error)
            setFormError(apiError.message)
            applyFieldErrors(setError, apiError.fieldErrors, ['email', 'password'])
        }
    }

    return (
        <AuthCard title="Log in">
            <Stack component="form" spacing={2} noValidate onSubmit={handleSubmit(onSubmit)}>
                {locationState?.reason === 'expired' && !formError && (
                    <Alert severity="warning">Your session has expired. Please log in again.</Alert>
                )}
                {formError && <Alert severity="error">{formError}</Alert>}

                <TextField
                    label="Email"
                    type="email"
                    autoComplete="email"
                    autoFocus
                    {...register('email')}
                    error={!!errors.email}
                    helperText={errors.email?.message}
                />
                <TextField
                    label="Password"
                    type="password"
                    autoComplete="current-password"
                    {...register('password')}
                    error={!!errors.password}
                    helperText={errors.password?.message}
                />

                <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
                    {isSubmitting ? 'Logging in…' : 'Log in'}
                </Button>

                <Typography variant="body2" textAlign="center">
                    No account yet?{' '}
                    <Link component={RouterLink} to="/register" state={locationState?.from ? { from: locationState.from } : undefined}>
                        Register
                    </Link>
                </Typography>
            </Stack>
        </AuthCard>
    )
}
