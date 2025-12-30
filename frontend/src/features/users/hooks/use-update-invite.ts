import { useMutation, useQueryClient } from '@tanstack/react-query'
import { invitesApi } from '../api/invites-api'
import { SYSTEM_INVITES_KEY } from './use-system-invites'
import type { UpdateInviteRequest } from '../types'

interface UpdateInviteParams {
  id: string
  data: UpdateInviteRequest
}

export function useUpdateInvite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: UpdateInviteParams) => invitesApi.update(id, data),
    onSuccess: (_data, variables) => {
      // Invalidate both the list and the single invite query
      queryClient.invalidateQueries({ queryKey: SYSTEM_INVITES_KEY })
      queryClient.invalidateQueries({ queryKey: ['system-invite', variables.id] })
    },
  })
}
