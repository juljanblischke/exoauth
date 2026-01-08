import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { emailApi } from '../api/email-api'
import { EMAIL_CONFIGURATION_KEY } from './use-email-configuration'
import type { UpdateEmailConfigurationRequest } from '../types'
import { getErrorMessage } from '@/lib/error-utils'

export function useUpdateEmailConfiguration() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (request: UpdateEmailConfigurationRequest) =>
      emailApi.updateConfiguration(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMAIL_CONFIGURATION_KEY })
      toast.success(t('email:configuration.actions.updateSuccess'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
