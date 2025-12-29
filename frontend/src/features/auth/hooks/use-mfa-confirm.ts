import { useMutation, useQueryClient } from '@tanstack/react-query'
import { mfaApi } from '../api/mfa-api'
import type { User } from '@/types/auth'
import type { MfaConfirmRequest } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const

interface MfaConfirmVariables extends MfaConfirmRequest {
  setupToken?: string
}

export function useMfaConfirm() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ setupToken, ...request }: MfaConfirmVariables) =>
      mfaApi.confirm(request, setupToken),
    onSuccess: (data) => {
      // If we got user data back (setupToken flow), set it in cache
      if (data.user) {
        queryClient.setQueryData<User>(AUTH_QUERY_KEY, data.user)
      } else {
        // Regular flow - update existing user cache to reflect MFA is now enabled
        queryClient.setQueryData<User>(AUTH_QUERY_KEY, (oldUser) => {
          if (!oldUser) return oldUser
          return {
            ...oldUser,
            mfaEnabled: true,
          }
        })
      }
    },
  })
}
