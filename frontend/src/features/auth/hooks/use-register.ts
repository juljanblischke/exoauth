import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'
import type { RegisterRequest, AuthResponse } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export interface UseRegisterOptions {
  onMfaSetupRequired?: (response: AuthResponse) => void
}

export function useRegister(options?: UseRegisterOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: (response) => {
      // Check if MFA setup is required (first user with system permissions)
      if (response.mfaSetupRequired && response.setupToken) {
        options?.onMfaSetupRequired?.(response)
        return
      }

      // Normal register success - set session and navigate
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      queryClient.setQueryData(AUTH_QUERY_KEY, response.user)
      navigate({ to: '/dashboard' })
    },
  })
}
