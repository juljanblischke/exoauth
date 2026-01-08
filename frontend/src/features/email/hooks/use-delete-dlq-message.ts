import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { DLQ_KEY } from './use-dead-letter-queue'
import { getErrorMessage } from '@/lib/error-utils'

export function useDeleteDlqMessage() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => emailApi.deleteDlqEmail(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: DLQ_KEY })
      toast.success(t('email:dlq.actions.deleteSuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
