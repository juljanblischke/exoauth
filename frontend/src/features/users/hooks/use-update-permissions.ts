import { useMutation, useQueryClient } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'
import { SYSTEM_USERS_KEY } from './use-system-users'
import { SYSTEM_USER_KEY } from './use-system-user'
import type { UpdatePermissionsRequest } from '../types'

interface UpdatePermissionsParams {
  id: string
  data: UpdatePermissionsRequest
}

export function useUpdatePermissions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: UpdatePermissionsParams) =>
      usersApi.updatePermissions(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      queryClient.invalidateQueries({ queryKey: [...SYSTEM_USER_KEY, id] })
    },
  })
}
