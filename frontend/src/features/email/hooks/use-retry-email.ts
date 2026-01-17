import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { DLQ_KEY } from './use-dead-letter-queue'
import { EMAIL_LOGS_KEY } from './use-email-logs'
import { getErrorMessage } from '@/lib/error-utils'

export function useRetryEmail() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => emailApi.retryDlqEmail(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: DLQ_KEY })
      queryClient.invalidateQueries({ queryKey: EMAIL_LOGS_KEY })
      toast.success(t('email:logs.actions.retrySuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
