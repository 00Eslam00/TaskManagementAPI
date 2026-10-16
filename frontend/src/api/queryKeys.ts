// One place for TanStack Query keys, so invalidation always targets the right cache entries.
export const queryKeys = {
    me: ['me'] as const,
    tasks: ['tasks'] as const,
    task: (id: string) => ['tasks', id] as const,
    users: ['admin', 'users'] as const,
}
