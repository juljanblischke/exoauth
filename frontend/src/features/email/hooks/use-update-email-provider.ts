import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { EMAIL_PROVIDERS_KEY } from './use-email-providers'
import { getErrorMessage } from '@/lib/error-utils'
import type { UpdateEmailProviderRequest } from '../types'

interface UpdateEmailProviderParams {
  id: string
  request: UpdateEmailProviderRequest
}

export function useUpdateEmailProvider() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ id, request }: UpdateEmailProviderParams) =>
      emailApi.updateProvider(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMAIL_PROVIDERS_KEY })
      toast.success(t('email:providers.updateSuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
