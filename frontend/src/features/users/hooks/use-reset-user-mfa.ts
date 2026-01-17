import { useMutation, useQueryClient } from '@tanstack/react-query'
import { userAdminApi, type AdminActionRequest } from '../api/user-admin-api'
import { SYSTEM_USERS_KEY } from './use-system-users'
import { SYSTEM_USER_KEY } from './use-system-user'

interface ResetUserMfaParams {
  userId: string
  request?: AdminActionRequest
}

export function useResetUserMfa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, request }: ResetUserMfaParams) =>
      userAdminApi.resetMfa(userId, request),
    onSuccess: (_, { userId }) => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      queryClient.invalidateQueries({ queryKey: [...SYSTEM_USER_KEY, userId] })
    },
  })
}
