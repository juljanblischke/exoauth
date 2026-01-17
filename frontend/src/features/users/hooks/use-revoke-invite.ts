import { useMutation, useQueryClient } from '@tanstack/react-query'
import { invitesApi } from '../api/invites-api'
import { SYSTEM_INVITES_KEY } from './use-system-invites'

export function useRevokeInvite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => invitesApi.revoke(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_INVITES_KEY })
    },
  })
}
