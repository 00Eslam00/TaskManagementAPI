import { apiClient } from './client'
import type { CreateUserRequest, UserResponse } from './types'

export async function getCurrentUser(): Promise<UserResponse> {
    const { data } = await apiClient.get<UserResponse>('/api/users/me')
    return data
}

// Admin-only endpoints. A normal user gets 403.

/** Active users, oldest first. */
export async function getUsers(): Promise<UserResponse[]> {
    const { data } = await apiClient.get<UserResponse[]>('/api/admin/users')
    return data
}

/** 409 when the email is already used by an active user. */
export async function createUser(request: CreateUserRequest): Promise<UserResponse> {
    const { data } = await apiClient.post<UserResponse>('/api/admin/users', request)
    return data
}

/** Soft delete. 400 when an admin tries to delete their own account, 404 when already deleted. */
export async function deleteUser(id: string): Promise<void> {
    await apiClient.delete(`/api/admin/users/${encodeURIComponent(id)}`)
}
