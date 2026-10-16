import { TASK_STATUSES, type TaskResponse } from '../api/types'

/** Router state the task list passes to a task's details page, so "Back" restores the filters. */
export interface TaskListLocationState {
    /** The list's query string, for example "?status=Done&q=report". */
    listSearch?: string
}

export const STATUS_FILTERS = ['All', ...TASK_STATUSES] as const

export type StatusFilter = (typeof STATUS_FILTERS)[number]

/** Reads a status from the URL; anything unknown means "All". */
export function parseStatusFilter(value: string | null): StatusFilter {
    return STATUS_FILTERS.find((filter) => filter === value) ?? 'All'
}

/** Keeps the tasks' order (the API's: priority first, then creation date). */
export function filterTasks(tasks: readonly TaskResponse[], status: StatusFilter, search: string): TaskResponse[] {
    const term = search.trim().toLowerCase()
    return tasks.filter(
        (task) => (status === 'All' || task.status === status) && (!term || task.title.toLowerCase().includes(term)),
    )
}

export function countByStatus(tasks: readonly TaskResponse[]): Record<StatusFilter, number> {
    const counts: Record<StatusFilter, number> = { All: tasks.length, Pending: 0, InProgress: 0, Done: 0 }
    for (const task of tasks) counts[task.status]++
    return counts
}
