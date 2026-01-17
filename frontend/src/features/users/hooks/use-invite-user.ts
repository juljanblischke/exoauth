import { useMutation, useQueryClient } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'
import { SYSTEM_USERS_KEY } from './use-system-users'
import { SYSTEM_INVITES_KEY } from './use-system-invites'
import type { InviteUserRequest } from '../types'

export function useInviteUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: InviteUserRequest) => usersApi.invite(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      queryClient.invalidateQueries({ queryKey: SYSTEM_INVITES_KEY })
    },
  })
}
