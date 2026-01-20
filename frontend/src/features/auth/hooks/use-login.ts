import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'
import type { LoginRequest, AuthResponse, DeviceApprovalRequiredResponse } from '../types'
import { isDeviceApprovalRequired } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export interface UseLoginOptions {
  onMfaRequired?: (response: AuthResponse) => void
  onMfaSetupRequired?: (response: AuthResponse) => void
  onDeviceApprovalRequired?: (response: DeviceApprovalRequiredResponse) => void
  onCaptchaRequired?: () => void
  onCaptchaExpired?: () => void
}

export function useLogin(options?: UseLoginOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
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
    onError: (error) => {
      const errorCode = (error as { code?: string })?.code?.toLowerCase()
      // Check if CAPTCHA is required
      if (errorCode === 'auth_captcha_required') {
        options?.onCaptchaRequired?.()
      }
      // Check if CAPTCHA token expired
      if (errorCode === 'auth_captcha_expired' || errorCode === 'auth_captcha_invalid') {
        options?.onCaptchaExpired?.()
      }
    },
  })
}
