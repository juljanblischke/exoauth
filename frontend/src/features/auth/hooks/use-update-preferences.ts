import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { preferencesApi } from '../api/preferences-api'
import type { User } from '@/types/auth'

const AUTH_QUERY_KEY = ['auth', 'me'] as const

export function useUpdatePreferences() {
  const queryClient = useQueryClient()
  const { t, i18n } = useTranslation()

  return useMutation({
    mutationFn: preferencesApi.updatePreferences,
    onSuccess: (_data, variables) => {
      // Update local i18n
      i18n.changeLanguage(variables.language)

      // Update the user in cache with new language preference
      queryClient.setQueryData<User>(AUTH_QUERY_KEY, (oldUser) => {
        if (!oldUser) return oldUser
        return {
          ...oldUser,
          preferredLanguage: variables.language,
        }
      })

      toast.success(t('settings:language.saved'))
    },
  })
}
