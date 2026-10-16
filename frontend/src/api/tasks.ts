import { apiClient } from './client'
import type { CreateTaskRequest, TaskResponse } from './types'

/** The caller's tasks, already sorted by the API (priority first, then creation date). */
export async function getTasks(): Promise<TaskResponse[]> {
    const { data } = await apiClient.get<TaskResponse[]>('/api/tasks')
    return data
}

export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
    const { data } = await apiClient.post<TaskResponse>('/api/tasks', request)
    return data
}
