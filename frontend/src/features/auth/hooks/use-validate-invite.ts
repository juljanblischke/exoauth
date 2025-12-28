import { useQuery } from '@tanstack/react-query'
import { authApi } from '../api/auth-api'

const INVITE_VALIDATION_KEY = ['auth', 'invite'] as const

export function useValidateInvite(token: string | undefined) {
  return useQuery({
    queryKey: [...INVITE_VALIDATION_KEY, token],
    queryFn: () => authApi.validateInvite(token!),
    enabled: !!token,
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}
