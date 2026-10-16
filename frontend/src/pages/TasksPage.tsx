import { useState } from 'react'
import { Link as RouterLink } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Box, Button, CardActionArea, Paper, Skeleton, Stack, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { queryKeys } from '../api/queryKeys'
import { getTasks } from '../api/tasks'
import type { TaskResponse } from '../api/types'
import { CreateTaskDialog } from '../components/CreateTaskDialog'
import { EmptyState, ErrorState } from '../components/PageStates'
import { PriorityBadge, StatusBadge } from '../components/TaskBadges'
import { toApiError } from '../lib/errors'
import { formatDateTime } from '../lib/format'

export default function TasksPage() {
    const [createOpen, setCreateOpen] = useState(false)
    const tasksQuery = useQuery({ queryKey: queryKeys.tasks, queryFn: getTasks })

    return (
        <Stack spacing={3}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                <Typography variant="h4" component="h1" sx={{ fontSize: { xs: '1.75rem', sm: '2.125rem' } }}>
                    My tasks
                </Typography>
                <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
                    New task
                </Button>
            </Box>

            {tasksQuery.isPending ? (
                <TaskListSkeleton />
            ) : tasksQuery.isError ? (
                <ErrorState
                    message={`Could not load your tasks. ${toApiError(tasksQuery.error).message}`}
                    onRetry={() => void tasksQuery.refetch()}
                />
            ) : tasksQuery.data.length === 0 ? (
                <EmptyState title="You have no tasks yet">
                    <Typography color="text.secondary" sx={{ mb: 2 }}>
                        Create your first task to get started.
                    </Typography>
                    <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
                        New task
                    </Button>
                </EmptyState>
            ) : (
                // Shown in the order the API returns: priority first, then creation date.
                <Stack component="ul" spacing={1.5} sx={{ listStyle: 'none', p: 0, m: 0 }}>
                    {tasksQuery.data.map((task) => (
                        <TaskRow key={task.id} task={task} />
                    ))}
                </Stack>
            )}

            <CreateTaskDialog open={createOpen} onClose={() => setCreateOpen(false)} />
        </Stack>
    )
}

function TaskRow({ task }: { task: TaskResponse }) {
    return (
        <Paper component="li" variant="outlined" sx={{ overflow: 'hidden' }}>
            {/* The whole row is one link to the details page, reachable with Tab and Enter. */}
            <CardActionArea component={RouterLink} to={`/tasks/${task.id}`} sx={{ p: 2 }}>
                <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={{ xs: 1, sm: 2 }}
                    sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between' }}
                >
                    <Box sx={{ minWidth: 0 }}>
                        <Typography variant="subtitle1" component="h2" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
                            {task.title}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                            Created {formatDateTime(task.createdAt)}
                        </Typography>
                    </Box>
                    <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
                        <PriorityBadge priority={task.priority} />
                        <StatusBadge status={task.status} />
                    </Stack>
                </Stack>
            </CardActionArea>
        </Paper>
    )
}

function TaskListSkeleton() {
    return (
        <Stack spacing={1.5} aria-label="Loading tasks">
            {[0, 1, 2].map((i) => (
                <Skeleton key={i} variant="rounded" height={76} />
            ))}
        </Stack>
    )
}
