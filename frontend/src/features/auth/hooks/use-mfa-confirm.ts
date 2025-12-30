import { useMutation, useQueryClient } from '@tanstack/react-query'
import { mfaApi } from '../api/mfa-api'
import type { User } from '@/types/auth'
import type { MfaConfirmRequest } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const

interface MfaConfirmVariables extends MfaConfirmRequest {
  setupToken?: string
}

export interface UseMfaConfirmOptions {
  /**
   * When true, don't set user in cache on success.
   * Used by setupToken flows (login/register/invite) where the form
   * handles auth after showing backup codes.
   */
  skipCache?: boolean
}

export function useMfaConfirm(options?: UseMfaConfirmOptions) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ setupToken, ...request }: MfaConfirmVariables) =>
      mfaApi.confirm(request, setupToken),
    onSuccess: (data) => {
      // Skip caching for setupToken flows - form handles auth after backup codes
      if (options?.skipCache) {
        return
      }

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
