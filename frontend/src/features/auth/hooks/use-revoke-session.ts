import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { sessionsApi } from '../api/sessions-api'
import { SESSIONS_QUERY_KEY } from './use-sessions'
import type { DeviceSessionDto } from '../types'

export function useRevokeSession() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: sessionsApi.revokeSession,
    onSuccess: (_, sessionId) => {
      // Remove the revoked session from cache
      queryClient.setQueryData<DeviceSessionDto[]>(SESSIONS_QUERY_KEY, (old) => {
        if (!old) return old
        return old.filter((session) => session.id !== sessionId)
      })
      toast.success(t('sessions:revoke.success'))
    },
  })
}
