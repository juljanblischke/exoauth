import { useMutation, useQueryClient } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'
import { SYSTEM_USERS_KEY } from './use-system-users'

export function useDeleteUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => usersApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
    },
  })
}
