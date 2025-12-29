import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { mfaApi } from '../api/mfa-api'
import type { User } from '@/types/auth'

const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function useMfaDisable() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: mfaApi.disable,
    onSuccess: () => {
      // Update user cache to reflect MFA is now disabled
      queryClient.setQueryData<User>(AUTH_QUERY_KEY, (oldUser) => {
        if (!oldUser) return oldUser
        return {
          ...oldUser,
          mfaEnabled: false,
        }
      })
      toast.success(t('mfa:disable.success'))
    },
  })
}
