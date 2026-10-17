import { QueryClient } from '@tanstack/react-query'

/**
 * How long the task list and the users list count as fresh. Every change this app makes is
 * written into those caches (or invalidates them), so going back to a list does not need to ask
 * the API again. Another tab or another admin's changes show up after at most this long.
 */
export const LIST_STALE_TIME_MS = 30_000

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // A failed request is shown to the user straight away instead of being retried silently.
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})
