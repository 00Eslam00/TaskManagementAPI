import { apiClient } from './client'
import type { CreateTaskRequest, TaskResponse, TaskStatus } from './types'

/** The caller's tasks, already sorted by the API (priority first, then creation date). */
export async function getTasks(): Promise<TaskResponse[]> {
    const { data } = await apiClient.get<TaskResponse[]>('/api/tasks')
    return data
}

export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
    const { data } = await apiClient.post<TaskResponse>('/api/tasks', request)
    return data
}

/** Served from the API's Redis cache after the first request. 404 for unknown ids and other users' tasks. */
export async function getTask(id: string): Promise<TaskResponse> {
    const { data } = await apiClient.get<TaskResponse>(`/api/tasks/${encodeURIComponent(id)}`)
    return data
}

/** 400 when the task is already Done, because Done is a final state. */
export async function updateTaskStatus(id: string, status: TaskStatus): Promise<TaskResponse> {
    const { data } = await apiClient.patch<TaskResponse>(`/api/tasks/${encodeURIComponent(id)}/status`, { status })
    return data
}
