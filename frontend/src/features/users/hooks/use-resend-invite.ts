import { useMutation, useQueryClient } from '@tanstack/react-query'
import { invitesApi } from '../api/invites-api'
import { SYSTEM_INVITES_KEY } from './use-system-invites'

export function useResendInvite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => invitesApi.resend(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SYSTEM_INVITES_KEY })
    },
  })
}
