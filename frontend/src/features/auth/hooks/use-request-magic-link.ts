import { useMutation } from '@tanstack/react-query'
import { authApi } from '../api/auth-api'
import type { RequestMagicLinkRequest } from '@/types/auth'

export function useRequestMagicLink() {
  return useMutation({
    mutationFn: (request: RequestMagicLinkRequest) =>
      authApi.requestMagicLink(request),
  })
}
