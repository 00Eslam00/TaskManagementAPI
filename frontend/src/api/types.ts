// TypeScript mirrors of the API's DTOs.

export const ROLES = ['User', 'Admin'] as const
export const TASK_STATUSES = ['Pending', 'InProgress', 'Done'] as const
export const TASK_PRIORITIES = ['Low', 'Medium', 'High'] as const

export type Role = (typeof ROLES)[number]
export type TaskStatus = (typeof TASK_STATUSES)[number]
export type TaskPriority = (typeof TASK_PRIORITIES)[number]

export interface UserResponse {
    id: string
    name: string
    email: string
    role: Role
    createdAt: string
}

export interface LoginRequest {
    email: string
    password: string
}

export interface RegisterRequest {
    name: string
    email: string
    password: string
}

export interface AuthResponse {
    accessToken: string
    expiresAt: string
    refreshToken: string
    refreshTokenExpiresAt: string
    tokenType: 'Bearer'
    user: UserResponse
}

export interface TaskResponse {
    id: string
    title: string
    description: string | null
    status: TaskStatus
    priority: TaskPriority
    createdAt: string
    userId: string
    processedAt: string | null
}

export interface CreateTaskRequest {
    title: string
    description?: string
    priority: TaskPriority
}


export interface ProblemDetails {
    type?: string
    title?: string
    status?: number
    detail?: string
    traceId?: string
    errors?: Record<string, string[]>
}
