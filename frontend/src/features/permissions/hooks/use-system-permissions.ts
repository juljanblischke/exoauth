import { useQuery } from '@tanstack/react-query'
import { permissionsApi } from '../api/permissions-api'

export const SYSTEM_PERMISSIONS_KEY = ['system', 'permissions'] as const

export function useSystemPermissions() {
  return useQuery({
    queryKey: SYSTEM_PERMISSIONS_KEY,
    queryFn: () => permissionsApi.getAll(),
    staleTime: 5 * 60 * 1000, // 5 minutes - permissions don't change often
  })
}
