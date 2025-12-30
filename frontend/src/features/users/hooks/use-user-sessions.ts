import { useQuery } from '@tanstack/react-query'
import { userAdminApi } from '../api/user-admin-api'

export const USER_SESSIONS_KEY = ['user-sessions'] as const

export function useUserSessions(userId: string | undefined) {
  return useQuery({
    queryKey: [...USER_SESSIONS_KEY, userId],
    queryFn: () => userAdminApi.getSessions(userId!),
    enabled: !!userId,
  })
}
