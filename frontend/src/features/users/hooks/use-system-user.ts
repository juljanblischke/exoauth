import { useQuery } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'

export const SYSTEM_USER_KEY = ['system', 'user'] as const

export function useSystemUser(id: string | undefined) {
  return useQuery({
    queryKey: [...SYSTEM_USER_KEY, id],
    queryFn: () => usersApi.getById(id!),
    enabled: !!id,
  })
}
