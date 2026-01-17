import { useQuery } from '@tanstack/react-query'
import { authApi } from '../api/auth-api'

const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function useCurrentUser() {
  return useQuery({
    queryKey: AUTH_QUERY_KEY,
    queryFn: () => authApi.getCurrentUser(),
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}
