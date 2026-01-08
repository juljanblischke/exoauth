import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import type { SendTestEmailRequest } from '../types'
import { getErrorMessage } from '@/lib/error-utils'

export function useSendTestEmail() {
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (request: SendTestEmailRequest) => emailApi.testProvider(request),
    onSuccess: () => {
      toast.success(t('email:configuration.actions.testSent'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
