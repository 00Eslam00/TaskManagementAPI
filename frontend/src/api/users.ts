import { apiClient } from './client'
import type { UserResponse } from './types'

export async function getCurrentUser(): Promise<UserResponse> {
    const { data } = await apiClient.get<UserResponse>('/api/users/me')
    return data
}
