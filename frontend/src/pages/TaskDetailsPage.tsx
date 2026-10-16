import { useState, type ReactNode } from 'react'
import { Link as RouterLink, useParams } from 'react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
    Alert,
    Box,
    Button,
    Divider,
    Paper,
    Skeleton,
    Stack,
    ToggleButton,
    ToggleButtonGroup,
    Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { queryKeys } from '../api/queryKeys'
import { getTask, updateTaskStatus } from '../api/tasks'
import { TASK_STATUSES, type TaskResponse, type TaskStatus } from '../api/types'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { EmptyState, ErrorState } from '../components/PageStates'
import { PriorityBadge, StatusBadge } from '../components/TaskBadges'
import { toApiError } from '../lib/errors'
import { formatDateTime, formatStatus } from '../lib/format'

// The API route only matches GUIDs, so anything else cannot be a task; no request is needed.
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export default function TaskDetailsPage() {
    const { id = '' } = useParams()
    const isValidId = GUID_PATTERN.test(id)

    const taskQuery = useQuery({
        queryKey: queryKeys.task(id),
        queryFn: () => getTask(id),
        enabled: isValidId,
    })

    const notFound = !isValidId || (taskQuery.isError && toApiError(taskQuery.error).status === 404)

    return (
        <Stack spacing={3}>
            <Box>
                <Button component={RouterLink} to="/tasks" startIcon={<ArrowBackIcon />}>
                    Back to tasks
                </Button>
            </Box>

            {notFound ? (
                // Also shown for another user's task: the API answers 404 so it does not reveal that the id exists.
                <EmptyState title="Task not found">
                    <Typography color="text.secondary">
                        This task does not exist, or it belongs to another user.
                    </Typography>
                </EmptyState>
            ) : taskQuery.isPending ? (
                <Skeleton variant="rounded" height={320} aria-label="Loading task" />
            ) : taskQuery.isError ? (
                <ErrorState
                    message={`Could not load this task. ${toApiError(taskQuery.error).message}`}
                    onRetry={() => void taskQuery.refetch()}
                />
            ) : (
                // Keyed by id so the status form starts fresh when moving to another task.
                <TaskDetails key={taskQuery.data.id} task={taskQuery.data} />
            )}
        </Stack>
    )
}

function TaskDetails({ task }: { task: TaskResponse }) {
    const queryClient = useQueryClient()
    const [confirmDone, setConfirmDone] = useState(false)

    const mutation = useMutation({
        mutationFn: (status: TaskStatus) => updateTaskStatus(task.id, status),
        onSuccess: (updated) => {
            // The response is the updated task, so it is written straight into both caches: the page
            // updates without another request, and the list already shows the new status when the
            // user goes back. A status change does not affect the list order (priority, then date).
            queryClient.setQueryData(queryKeys.task(updated.id), updated)
            queryClient.setQueryData<TaskResponse[]>(queryKeys.tasks, (tasks) =>
                tasks?.map((t) => (t.id === updated.id ? updated : t)),
            )
            setConfirmDone(false)
        },
        onError: () => {
            // The task may have changed elsewhere (for example set to Done in another tab),
            // so reload it to show what the server has now.
            setConfirmDone(false)
            void queryClient.invalidateQueries({ queryKey: queryKeys.task(task.id) })
        },
    })

    const isDone = task.status === 'Done'

    const handleStatusChange = (status: TaskStatus | null) => {
        if (!status || status === task.status) return
        mutation.reset()
        // Done is final, so it is confirmed first.
        if (status === 'Done') setConfirmDone(true)
        else mutation.mutate(status)
    }

    return (
        <Paper variant="outlined" sx={{ p: { xs: 2, sm: 4 } }}>
            <Stack spacing={3}>
                <Box>
                    <Typography variant="h4" component="h1" sx={{ fontSize: { xs: '1.5rem', sm: '2rem' }, overflowWrap: 'anywhere' }}>
                        {task.title}
                    </Typography>
                    <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                        <PriorityBadge priority={task.priority} />
                        <StatusBadge status={task.status} />
                    </Stack>
                </Box>

                <Box>
                    <Typography variant="overline" color="text.secondary" component="h2">
                        Description
                    </Typography>
                    {task.description ? (
                        <Typography sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{task.description}</Typography>
                    ) : (
                        <Typography color="text.secondary" fontStyle="italic">
                            No description
                        </Typography>
                    )}
                </Box>

                <Divider />

                <Stack spacing={1.5}>
                    <Field label="Status">{formatStatus(task.status)}</Field>
                    <Field label="Priority">{task.priority}</Field>
                    <Field label="Created">{formatDateTime(task.createdAt)}</Field>
                    <Field label="Processed">{task.processedAt ? formatDateTime(task.processedAt) : 'Not yet'}</Field>
                    <Field label="Task ID">
                        <Typography component="span" variant="body2" sx={{ fontFamily: 'monospace', overflowWrap: 'anywhere' }}>
                            {task.id}
                        </Typography>
                    </Field>
                </Stack>

                <Divider />

                <Box>
                    <Typography variant="overline" color="text.secondary" component="h2" id="status-label">
                        Change status
                    </Typography>
                    <ToggleButtonGroup
                        exclusive
                        color="primary"
                        value={task.status}
                        onChange={(_, value: TaskStatus | null) => handleStatusChange(value)}
                        disabled={isDone || mutation.isPending}
                        aria-labelledby="status-label"
                        sx={{ display: 'flex', mt: 1, maxWidth: 420 }}
                    >
                        {TASK_STATUSES.map((status) => (
                            <ToggleButton key={status} value={status} sx={{ flex: 1 }}>
                                {formatStatus(status)}
                            </ToggleButton>
                        ))}
                    </ToggleButtonGroup>

                    {isDone && (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                            This task is done. Done is a final status and cannot be changed.
                        </Typography>
                    )}
                    {mutation.isError && (
                        <Alert severity="error" sx={{ mt: 2 }}>
                            {toApiError(mutation.error).message}
                        </Alert>
                    )}
                    {mutation.isSuccess && (
                        <Alert severity="success" sx={{ mt: 2 }}>
                            Status changed to {formatStatus(mutation.data.status)}.
                        </Alert>
                    )}
                </Box>
            </Stack>

            <ConfirmDialog
                open={confirmDone}
                title="Mark this task as done?"
                confirmLabel="Mark as done"
                confirmColor="success"
                pending={mutation.isPending}
                onConfirm={() => mutation.mutate('Done')}
                onCancel={() => setConfirmDone(false)}
            >
                Done is a final status. After this, the status of “{task.title}” can no longer be changed.
            </ConfirmDialog>
        </Paper>
    )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
    return (
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={{ xs: 0, sm: 2 }}>
            <Typography variant="body2" color="text.secondary" sx={{ width: { sm: 120 }, flexShrink: 0 }}>
                {label}
            </Typography>
            <Typography component="div" variant="body2">
                {children}
            </Typography>
        </Stack>
    )
}
