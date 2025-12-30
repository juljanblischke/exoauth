import { useMutation } from '@tanstack/react-query'
import { passwordResetApi } from '../api/password-reset-api'
import type { ResetPasswordRequest } from '@/types/auth'

export function useResetPassword() {
  return useMutation({
    mutationFn: (request: ResetPasswordRequest) =>
      passwordResetApi.resetPassword(request),
  })
}
