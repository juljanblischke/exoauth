import { useQuery } from '@tanstack/react-query'
import { sessionsApi } from '../api/sessions-api'

export const SESSIONS_QUERY_KEY = ['auth', 'sessions'] as const

export function useSessions() {
  return useQuery({
    queryKey: SESSIONS_QUERY_KEY,
    queryFn: sessionsApi.getSessions,
  })
}
