import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { EMAIL_PROVIDERS_KEY } from './use-email-providers'
import { getErrorMessage } from '@/lib/error-utils'

export function useResetCircuitBreaker() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => emailApi.resetCircuitBreaker(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMAIL_PROVIDERS_KEY })
      toast.success(t('email:providers.actions.resetSuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
