import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { passkeysApi } from '../api/passkeys-api'
import { PASSKEYS_QUERY_KEY } from './use-passkeys'
import type { PasskeyRegisterRequest } from '../types/passkey'

export function usePasskeyRegister() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (data: PasskeyRegisterRequest) => passkeysApi.register(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: PASSKEYS_QUERY_KEY })
      toast.success(t('auth:passkeys.register.success'))
    },
  })
}
