import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { mfaApi } from '../api/mfa-api'
import { isDeviceApprovalRequired, type DeviceApprovalRequiredResponse } from '../types'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

interface UseMfaVerifyOptions {
  onDeviceApprovalRequired?: (response: DeviceApprovalRequiredResponse) => void
}

export function useMfaVerify(options?: UseMfaVerifyOptions) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: mfaApi.verify,
    onSuccess: (response) => {
      // Check if device approval is required (risk-based authentication)
      if (isDeviceApprovalRequired(response)) {
        options?.onDeviceApprovalRequired?.(response)
        return
      }

      // Normal MFA verify success - set session and navigate
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      queryClient.setQueryData(AUTH_QUERY_KEY, response.user)
      navigate({ to: '/dashboard' })
    },
  })
}
