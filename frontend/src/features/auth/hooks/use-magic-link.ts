import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'
import type { MagicLinkLoginRequest, AuthResponse, DeviceApprovalRequiredResponse } from '../types'
import { isDeviceApprovalRequired } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export interface UseMagicLinkLoginOptions {
  onMfaRequired?: (response: AuthResponse) => void
  onMfaSetupRequired?: (response: AuthResponse) => void
  onDeviceApprovalRequired?: (response: DeviceApprovalRequiredResponse) => void
}

export function useMagicLinkLogin(options?: UseMagicLinkLoginOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: MagicLinkLoginRequest) => authApi.magicLinkLogin(data),
    onSuccess: (response) => {
      // Check if device approval is required (risk-based authentication)
      if (isDeviceApprovalRequired(response)) {
        options?.onDeviceApprovalRequired?.(response)
        return
      }

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
      navigate({ to: '/system/dashboard' })
    },
  })
}
