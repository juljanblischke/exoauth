import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { sessionsApi } from '../api/sessions-api'
import { SESSIONS_QUERY_KEY } from './use-sessions'
import type { DeviceSessionDto, UpdateSessionRequest } from '../types'

export function useUpdateSession() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ sessionId, request }: { sessionId: string; request: UpdateSessionRequest }) =>
      sessionsApi.updateSession(sessionId, request),
    onSuccess: (updatedSession) => {
      // Update the session in cache
      queryClient.setQueryData<DeviceSessionDto[]>(SESSIONS_QUERY_KEY, (old) => {
        if (!old) return old
        return old.map((session) =>
          session.id === updatedSession.id ? updatedSession : session
        )
      })
      toast.success(t('sessions:rename.success'))
    },
  })
}
