import { useQuery } from '@tanstack/react-query'
import { invitesApi } from '../api/invites-api'
import { SYSTEM_INVITES_KEY } from './use-system-invites'

export function useSystemInvite(id: string | null) {
  return useQuery({
    queryKey: [...SYSTEM_INVITES_KEY, id],
    queryFn: () => invitesApi.getById(id!),
    enabled: !!id,
  })
}
