import { QueryClient } from '@tanstack/react-query';

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: (failureCount, error) => {
          const status = (error as { status?: number })?.status;
          if (status && status >= 400 && status < 500 && status !== 408 && status !== 429) {
            return false;
          }
          return failureCount < 2;
        },
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnReconnect: true,
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: 0,
      },
    },
  });
}
