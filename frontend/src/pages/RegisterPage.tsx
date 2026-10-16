import { useState } from 'react'
import { Link as RouterLink, useLocation } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Alert, Button, Link, Stack, TextField, Typography } from '@mui/material'
import * as authApi from '../api/auth'
import type { LoginLocationState } from '../auth/authContext'
import { useAuth } from '../auth/useAuth'
import { AuthCard } from '../components/AuthCard'
import { toApiError } from '../lib/errors'
import { applyFieldErrors } from '../lib/forms'
import { limits } from '../lib/limits'

const registerSchema = z
    .object({
        name: z
            .string()
            .trim()
            .min(1, 'Name is required')
            .max(limits.nameMax, `Name cannot be longer than ${limits.nameMax} characters`),
        email: z
            .string()
            .trim()
            .min(1, 'Email is required')
            .email('Enter a valid email address')
            .max(limits.emailMax, `Email cannot be longer than ${limits.emailMax} characters`),
        password: z
            .string()
            .min(limits.passwordMin, `Password must be at least ${limits.passwordMin} characters`)
            .max(limits.passwordMax, `Password cannot be longer than ${limits.passwordMax} characters`),
        confirmPassword: z.string().min(1, 'Please confirm your password'),
    })
    .refine((values) => values.password === values.confirmPassword, {
        message: 'Passwords do not match',
        path: ['confirmPassword'],
    })

type RegisterForm = z.infer<typeof registerSchema>

export default function RegisterPage() {
    const { login } = useAuth()
    const location = useLocation()
    const [formError, setFormError] = useState<string | null>(null)

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<RegisterForm>({
        resolver: zodResolver(registerSchema),
        defaultValues: { name: '', email: '', password: '', confirmPassword: '' },
    })

    const onSubmit = async ({ name, email, password }: RegisterForm) => {
        setFormError(null)
        try {
            await authApi.register({ name, email, password })
        } catch (error) {
            const apiError = toApiError(error)
            setFormError(apiError.message)
            // A duplicate email comes back as 409 with a message, not as a field error.
            if (apiError.status === 409) setError('email', { type: 'server', message: apiError.message })
            applyFieldErrors(setError, apiError.fieldErrors, ['name', 'email', 'password'])
            return
        }

        // Registration does not return a token, so log in with the same credentials straight away
        // instead of making the user type them again. PublicOnlyRoute then opens the task list.
        try {
            await login({ email, password })
        } catch (error) {
            setFormError(`Your account was created, but logging in failed: ${toApiError(error).message}`)
        }
    }

    const loginState = location.state as LoginLocationState | null

    return (
        <AuthCard title="Create an account">
            <Stack component="form" spacing={2} noValidate onSubmit={handleSubmit(onSubmit)}>
                {formError && <Alert severity="error">{formError}</Alert>}

                <TextField
                    label="Name"
                    autoComplete="name"
                    autoFocus
                    {...register('name')}
                    error={!!errors.name}
                    helperText={errors.name?.message}
                />
                <TextField
                    label="Email"
                    type="email"
                    autoComplete="email"
                    {...register('email')}
                    error={!!errors.email}
                    helperText={errors.email?.message}
                />
                <TextField
                    label="Password"
                    type="password"
                    autoComplete="new-password"
                    {...register('password')}
                    error={!!errors.password}
                    helperText={errors.password?.message ?? `At least ${limits.passwordMin} characters`}
                />
                <TextField
                    label="Confirm password"
                    type="password"
                    autoComplete="new-password"
                    {...register('confirmPassword')}
                    error={!!errors.confirmPassword}
                    helperText={errors.confirmPassword?.message}
                />

                <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
                    {isSubmitting ? 'Creating account…' : 'Register'}
                </Button>

                <Typography variant="body2" textAlign="center">
                    Already have an account?{' '}
                    <Link component={RouterLink} to="/login" state={loginState?.from ? { from: loginState.from } : undefined}>
                        Log in
                    </Link>
                </Typography>
            </Stack>
        </AuthCard>
    )
}
