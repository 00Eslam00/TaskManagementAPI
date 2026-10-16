import type { TaskResponse } from '../api/types'

// The API's background worker picks up every new task and sets processedAt when it is done
// (about 5 seconds per task). The worker never changes the status, so the UI watches
// processedAt, not "status === Pending": a Pending task can already be processed.

/** How often to ask the API again while a task is being processed. */
export const PROCESSING_POLL_INTERVAL_MS = 2000

/**
 * Stop polling for a task that is still unprocessed this long after it was created.
 * Something is wrong with the worker then (it re-queues such tasks on its next start),
 * and polling forever would only load the API.
 */
const MAX_POLL_AGE_MS = 5 * 60 * 1000

export function isProcessing(task: TaskResponse): boolean {
    return task.processedAt === null
}

export function shouldPoll(task: TaskResponse): boolean {
    return isProcessing(task) && Date.now() - Date.parse(task.createdAt) < MAX_POLL_AGE_MS
}

/** refetchInterval for TanStack Query: poll only while at least one task needs it. */
export function pollWhileProcessing(tasks: TaskResponse[] | TaskResponse | undefined): number | false {
    if (!tasks) return false
    const list = Array.isArray(tasks) ? tasks : [tasks]
    return list.some(shouldPoll) ? PROCESSING_POLL_INTERVAL_MS : false
}
