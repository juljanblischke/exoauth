import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { EMAIL_ANNOUNCEMENTS_KEY } from './use-email-announcements'
import { getErrorMessage } from '@/lib/error-utils'

export function useSendEmailAnnouncement() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => emailApi.sendAnnouncement(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMAIL_ANNOUNCEMENTS_KEY })
      toast.success(t('email:announcements.actions.sendSuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
