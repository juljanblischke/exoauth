import { useMutation, useQueryClient } from '@tanstack/react-query'
import { userAdminApi } from '../api/user-admin-api'
import { SYSTEM_USERS_KEY } from './use-system-users'
import { SYSTEM_USER_KEY } from './use-system-user'

export function useActivateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => userAdminApi.activate(userId),
    onSuccess: (_, userId) => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      queryClient.invalidateQueries({ queryKey: [...SYSTEM_USER_KEY, userId] })
    },
  })
}
