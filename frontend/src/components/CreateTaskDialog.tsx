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
import { createTask } from '../api/tasks'
import { TASK_PRIORITIES, type TaskResponse } from '../api/types'
import { toApiError } from '../lib/errors'
import { applyFieldErrors } from '../lib/forms'
import { limits } from '../lib/limits'

const createTaskSchema = z.object({
    title: z
        .string()
        .trim()
        .min(1, 'Title is required')
        .max(limits.titleMax, `Title cannot be longer than ${limits.titleMax} characters`),
    description: z
        .string()
        .trim()
        .max(limits.descriptionMax, `Description cannot be longer than ${limits.descriptionMax} characters`),
    priority: z.enum(TASK_PRIORITIES),
})

type CreateTaskForm = z.infer<typeof createTaskSchema>

const emptyForm: CreateTaskForm = { title: '', description: '', priority: 'Medium' }

interface CreateTaskDialogProps {
    open: boolean
    onClose: () => void
    onCreated?: (task: TaskResponse) => void
}

export function CreateTaskDialog({ open, onClose, onCreated }: CreateTaskDialogProps) {
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
        watch,
        formState: { errors },
    } = useForm<CreateTaskForm>({ resolver: zodResolver(createTaskSchema), defaultValues: emptyForm })

    const mutation = useMutation({
        mutationFn: createTask,
        onSuccess: async (task) => {
            // Refetch the list so the new task appears in the position the API sorts it to.
            await queryClient.invalidateQueries({ queryKey: queryKeys.tasks })
            reset(emptyForm)
            setFormError(null)
            onCreated?.(task)
            onClose()
        },
        onError: (error) => {
            // The form keeps what the user typed, so they can fix it and try again.
            const apiError = toApiError(error)
            setFormError(apiError.message)
            if (apiError.status === 409) setError('title', { type: 'server', message: 'Choose a different title' })
            applyFieldErrors(setError, apiError.fieldErrors, ['title', 'description', 'priority'])
        },
    })

    const onSubmit = (values: CreateTaskForm) => {
        setFormError(null)
        mutation.mutate({
            title: values.title,
            description: values.description || undefined,
            priority: values.priority,
        })
    }

    const handleClose = () => {
        // Closing while the request runs would hide its result.
        if (mutation.isPending) return
        setFormError(null)
        onClose()
    }

    const descriptionLength = watch('description').length

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm" fullScreen={fullScreen}>
            <form noValidate onSubmit={handleSubmit(onSubmit)}>
                <DialogTitle>New task</DialogTitle>
                <DialogContent>
                    <Stack spacing={2} sx={{ pt: 1 }}>
                        {formError && <Alert severity="error">{formError}</Alert>}

                        <TextField
                            label="Title"
                            required
                            autoFocus
                            {...register('title')}
                            error={!!errors.title}
                            helperText={errors.title?.message}
                        />
                        <TextField
                            label="Description"
                            multiline
                            minRows={3}
                            {...register('description')}
                            error={!!errors.description}
                            helperText={errors.description?.message ?? `${descriptionLength} / ${limits.descriptionMax}`}
                        />
                        <Controller
                            control={control}
                            name="priority"
                            render={({ field }) => (
                                <TextField select label="Priority" {...field} error={!!errors.priority} helperText={errors.priority?.message}>
                                    {TASK_PRIORITIES.map((priority) => (
                                        <MenuItem key={priority} value={priority}>
                                            {priority}
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
                        {mutation.isPending ? 'Creating…' : 'Create task'}
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    )
}
