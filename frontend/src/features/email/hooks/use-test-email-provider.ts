import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { getErrorMessage } from '@/lib/error-utils'
import type { SendTestEmailRequest } from '../types'

export function useTestEmailProvider() {
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (request: SendTestEmailRequest) => emailApi.testProvider(request),
    onSuccess: (result) => {
      if (result.success) {
        toast.success(t('email:providers.actions.testSuccess'))
      } else {
        toast.error(t('email:providers.actions.testFailed', { error: result.error }))
      }
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
