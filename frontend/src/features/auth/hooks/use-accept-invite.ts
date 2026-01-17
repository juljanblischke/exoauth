import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'
import type { AcceptInviteRequest, AuthResponse } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export interface UseAcceptInviteOptions {
  onMfaSetupRequired?: (response: AuthResponse) => void
}

export function useAcceptInvite(options?: UseAcceptInviteOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: AcceptInviteRequest) => authApi.acceptInvite(data),
    onSuccess: (response) => {
      // Check if MFA setup is required (invited user with system permissions)
      if (response.mfaSetupRequired && response.setupToken) {
        options?.onMfaSetupRequired?.(response)
        return
      }

      // Set session flag for page refresh detection
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      // Update the auth cache with the user data (auto-login)
      queryClient.setQueryData(AUTH_QUERY_KEY, response.user)
      // Navigate to dashboard
      navigate({ to: '/dashboard' })
    },
  })
}
