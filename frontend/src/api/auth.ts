import { apiClient } from './client'
import type { AuthResponse, LoginRequest, RegisterRequest, UserResponse } from './types'

export async function login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/api/auth/login', request)
    return data
}

export async function register(request: RegisterRequest): Promise<UserResponse> {
    const { data } = await apiClient.post<UserResponse>('/api/auth/register', request)
    return data
}

/** Revokes the user's refresh tokens on the server. */
export async function logout(): Promise<void> {
    await apiClient.post('/api/auth/logout')
}
