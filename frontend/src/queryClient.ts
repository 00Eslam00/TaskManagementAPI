import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // A failed request is shown to the user straight away.
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})
