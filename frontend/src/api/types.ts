// TypeScript mirrors of the API's DTOs (backend/.../Application/*/…Dtos.cs).
// Enums travel as their names because the API uses JsonStringEnumConverter.
// Dates are ISO 8601 strings in UTC.

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

/** Used by an admin to add a user. Unlike self-registration, the role can be chosen. */
export interface CreateUserRequest {
    name: string
    email: string
    password: string
    role: Role
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
    /** Set by the background worker once it has processed the task. */
    processedAt: string | null
}

export interface CreateTaskRequest {
    title: string
    description?: string
    priority: TaskPriority
}

/** RFC 7807 body the API returns for every error. */
export interface ProblemDetails {
    type?: string
    title?: string
    status?: number
    detail?: string
    traceId?: string
    /** Only present on 400 validation errors: field name -> messages. */
    errors?: Record<string, string[]>
}
