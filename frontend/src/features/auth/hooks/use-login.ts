import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'
import type { LoginRequest, AuthResponse } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export interface UseLoginOptions {
  onMfaRequired?: (response: AuthResponse) => void
  onMfaSetupRequired?: (response: AuthResponse) => void
}

export function useLogin(options?: UseLoginOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      // Check if MFA verification is required
      if (response.mfaRequired && response.mfaToken) {
        options?.onMfaRequired?.(response)
        return
      }

      // Check if MFA setup is required (for users with system permissions)
      if (response.mfaSetupRequired && response.setupToken) {
        options?.onMfaSetupRequired?.(response)
        return
      }

      // Normal login success - set session and navigate
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      queryClient.setQueryData(AUTH_QUERY_KEY, response.user)
      navigate({ to: '/dashboard' })
    },
  })
}
