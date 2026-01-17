import { useMutation } from '@tanstack/react-query'
import { passwordResetApi } from '../api/password-reset-api'
import type { ForgotPasswordRequest } from '@/types/auth'

export function useForgotPassword() {
  return useMutation({
    mutationFn: (request: ForgotPasswordRequest) =>
      passwordResetApi.forgotPassword(request),
  })
}
