import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
    Alert,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    MenuItem,
    Stack,
    TextField,
    useMediaQuery,
    useTheme,
} from '@mui/material'
import { queryKeys } from '../api/queryKeys'
import { ROLES, type UserResponse } from '../api/types'
import { createUser } from '../api/users'
import { toApiError } from '../lib/errors'
import { applyFieldErrors } from '../lib/forms'
import { limits } from '../lib/limits'
import { emailField, nameField, newPasswordField } from '../lib/validation'

const createUserSchema = z.object({
    name: nameField,
    email: emailField,
    password: newPasswordField,
    role: z.enum(ROLES),
})

type CreateUserForm = z.infer<typeof createUserSchema>

const emptyForm: CreateUserForm = { name: '', email: '', password: '', role: 'User' }

interface CreateUserDialogProps {
    open: boolean
    onClose: () => void
    onCreated: (user: UserResponse) => void
}

export function CreateUserDialog({ open, onClose, onCreated }: CreateUserDialogProps) {
    const queryClient = useQueryClient()
    const theme = useTheme()
    const fullScreen = useMediaQuery(theme.breakpoints.down('sm'))
    const [formError, setFormError] = useState<string | null>(null)

    const {
        register,
        control,
        handleSubmit,
        setError,
        reset,
        formState: { errors },
    } = useForm<CreateUserForm>({ resolver: zodResolver(createUserSchema), defaultValues: emptyForm })

    const mutation = useMutation({
        mutationFn: createUser,
        onSuccess: (user) => {
            // The list is sorted oldest first, so the new user goes at the end; no refetch needed.
            queryClient.setQueryData<UserResponse[]>(queryKeys.users, (users) => (users ? [...users, user] : users))
            reset(emptyForm)
            setFormError(null)
            onCreated(user)
            onClose()
        },
        onError: (error) => {
            // The form keeps what the admin typed, so they can fix it and try again.
            const apiError = toApiError(error)
            setFormError(apiError.message)
            if (apiError.status === 409) setError('email', { type: 'server', message: 'This email is already in use' })
            applyFieldErrors(setError, apiError.fieldErrors, ['name', 'email', 'password', 'role'])
        },
    })

    const handleClose = () => {
        if (mutation.isPending) return
        setFormError(null)
        onClose()
    }

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm" fullScreen={fullScreen}>
            <form
                noValidate
                onSubmit={handleSubmit((values) => {
                    setFormError(null)
                    mutation.mutate(values)
                })}
            >
                <DialogTitle>Add user</DialogTitle>
                <DialogContent>
                    <Stack spacing={2} sx={{ pt: 1 }}>
                        {formError && <Alert severity="error">{formError}</Alert>}

                        <TextField
                            label="Name"
                            required
                            autoFocus
                            autoComplete="off"
                            {...register('name')}
                            error={!!errors.name}
                            helperText={errors.name?.message}
                        />
                        <TextField
                            label="Email"
                            type="email"
                            required
                            autoComplete="off"
                            {...register('email')}
                            error={!!errors.email}
                            helperText={errors.email?.message}
                        />
                        <TextField
                            label="Password"
                            type="password"
                            required
                            // Stops the browser from filling in the admin's own saved password.
                            autoComplete="new-password"
                            {...register('password')}
                            error={!!errors.password}
                            helperText={errors.password?.message ?? `At least ${limits.passwordMin} characters`}
                        />
                        <Controller
                            control={control}
                            name="role"
                            render={({ field }) => (
                                <TextField select label="Role" {...field} error={!!errors.role} helperText={errors.role?.message}>
                                    {ROLES.map((role) => (
                                        <MenuItem key={role} value={role}>
                                            {role}
                                        </MenuItem>
                                    ))}
                                </TextField>
                            )}
                        />
                    </Stack>
                </DialogContent>
                <DialogActions sx={{ px: 3, pb: 2 }}>
                    <Button onClick={handleClose} disabled={mutation.isPending}>
                        Cancel
                    </Button>
                    <Button type="submit" variant="contained" disabled={mutation.isPending}>
                        {mutation.isPending ? 'Adding…' : 'Add user'}
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    )
}
