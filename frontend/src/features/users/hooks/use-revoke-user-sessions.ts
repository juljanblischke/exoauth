import { useMutation, useQueryClient } from '@tanstack/react-query'
import { userAdminApi } from '../api/user-admin-api'
import { USER_SESSIONS_KEY } from './use-user-sessions'
import { SYSTEM_USERS_KEY } from './use-system-users'
import { SYSTEM_USER_KEY } from './use-system-user'

export function useRevokeUserSessions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => userAdminApi.revokeSessions(userId),
    onSuccess: (_, userId) => {
      queryClient.invalidateQueries({ queryKey: [...USER_SESSIONS_KEY, userId] })
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      queryClient.invalidateQueries({ queryKey: [...SYSTEM_USER_KEY, userId] })
    },
  })
}
