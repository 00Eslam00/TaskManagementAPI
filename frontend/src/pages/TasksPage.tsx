import { useMemo, useState } from 'react'
import { Link as RouterLink, useLocation, useSearchParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Box, Button, CardActionArea, Paper, Skeleton, Stack, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { queryKeys } from '../api/queryKeys'
import { getTasks } from '../api/tasks'
import type { TaskResponse } from '../api/types'
import { CreateTaskDialog } from '../components/CreateTaskDialog'
import { EmptyState, ErrorState } from '../components/PageStates'
import { PriorityBadge, ProcessingBadge, StatusBadge } from '../components/TaskBadges'
import { TaskFilters } from '../components/TaskFilters'
import { toApiError } from '../lib/errors'
import { formatDateTime } from '../lib/format'
import { isProcessing, pollWhileProcessing } from '../lib/processing'
import {
    countByStatus,
    filterTasks,
    parseStatusFilter,
    type StatusFilter,
    type TaskListLocationState,
} from '../lib/taskFilters'
import { LIST_STALE_TIME_MS } from '../queryClient'

export default function TasksPage() {
    const [createOpen, setCreateOpen] = useState(false)
    const tasksQuery = useQuery({
        queryKey: queryKeys.tasks,
        queryFn: getTasks,
        // While any task is still waiting for the background worker, the list is reloaded every
        // 2 seconds so its "Processing…" badge changes without a page reload. Polling stops by
        // itself once every task is processed, and pauses while the browser tab is hidden.
        refetchInterval: (query) => pollWhileProcessing(query.state.data),
        // Coming back to the list reuses the cache instead of asking the API again, unless a task
        // is still processing: then the cached copy may be out of date (the details page may have
        // seen it finish), so it is reloaded at once.
        staleTime: (query) => (query.state.data?.some(isProcessing) ? 0 : LIST_STALE_TIME_MS),
    })

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
                <FilteredTaskList tasks={tasksQuery.data} />
            )}

            <CreateTaskDialog open={createOpen} onClose={() => setCreateOpen(false)} />
        </Stack>
    )
}

/**
 * Filters in the browser, as the task allows; the API already returns every task of the user.
 * The filter and search live in the URL (?status=Done&q=report), so they survive going to a
 * task's details page and back, a page refresh, and can be shared as a link.
 */
function FilteredTaskList({ tasks }: { tasks: TaskResponse[] }) {
    const [searchParams, setSearchParams] = useSearchParams()
    const status = parseStatusFilter(searchParams.get('status'))
    const search = searchParams.get('q') ?? ''

    const updateParams = (changes: { status?: StatusFilter; q?: string }) => {
        const next = new URLSearchParams(searchParams)
        for (const [key, value] of Object.entries(changes)) {
            if (!value || value === 'All') next.delete(key)
            else next.set(key, value)
        }
        // replace: typing in the search box should not add a history entry per keystroke.
        setSearchParams(next, { replace: true })
    }

    const counts = useMemo(() => countByStatus(tasks), [tasks])
    const visibleTasks = useMemo(() => filterTasks(tasks, status, search), [tasks, status, search])

    return (
        <Stack spacing={2}>
            <TaskFilters
                status={status}
                search={search}
                counts={counts}
                onStatusChange={(value) => updateParams({ status: value })}
                onSearchChange={(value) => updateParams({ q: value })}
            />

            {visibleTasks.length === 0 ? (
                <EmptyState title="No tasks match your filters">
                    <Button onClick={() => setSearchParams({}, { replace: true })}>Clear filters</Button>
                </EmptyState>
            ) : (
                <Stack component="ul" spacing={1.5} sx={{ listStyle: 'none', p: 0, m: 0 }} aria-live="polite">
                    {visibleTasks.map((task) => (
                        <TaskRow key={task.id} task={task} />
                    ))}
                </Stack>
            )}
        </Stack>
    )
}

function TaskRow({ task }: { task: TaskResponse }) {
    const { search } = useLocation()
    const state: TaskListLocationState = { listSearch: search }

    return (
        <Paper component="li" variant="outlined" sx={{ overflow: 'hidden' }}>
            {/* The whole row is one link to the details page, reachable with Tab and Enter. */}
            <CardActionArea component={RouterLink} to={`/tasks/${task.id}`} state={state} sx={{ p: 2 }}>
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
                    <Stack direction="row" spacing={1} useFlexGap sx={{ flexShrink: 0, flexWrap: 'wrap' }}>
                        <PriorityBadge priority={task.priority} />
                        <StatusBadge status={task.status} />
                        <ProcessingBadge task={task} />
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
