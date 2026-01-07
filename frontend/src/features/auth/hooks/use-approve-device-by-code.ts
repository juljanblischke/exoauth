import { useMutation } from '@tanstack/react-query'
import { deviceApprovalApi } from '../api/device-approval-api'
import type { ApproveDeviceByCodeRequest } from '../types'

interface UseApproveDeviceByCodeOptions {
  onCaptchaRequired?: () => void
  onCaptchaExpired?: () => void
  onError?: (error: unknown) => void
}

export function useApproveDeviceByCode(options?: UseApproveDeviceByCodeOptions) {
  return useMutation({
    mutationFn: (data: ApproveDeviceByCodeRequest) =>
      deviceApprovalApi.approveByCode(data),
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
      options?.onError?.(error)
    },
  })
}
