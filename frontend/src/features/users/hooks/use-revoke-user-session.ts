import { useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { USER_SESSIONS_KEY } from './use-user-sessions'
import { SYSTEM_USERS_KEY } from './use-system-users'

interface RevokeUserSessionParams {
  userId: string
  sessionId: string
}

export function useRevokeUserSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ userId, sessionId }: RevokeUserSessionParams) => {
      await apiClient.delete(`/system/users/${userId}/sessions?id=${sessionId}`)
    },
    onSuccess: (_, { userId }) => {
      queryClient.invalidateQueries({ queryKey: [...USER_SESSIONS_KEY, userId] })
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
    },
  })
}
