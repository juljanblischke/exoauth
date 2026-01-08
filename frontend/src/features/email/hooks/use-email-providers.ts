import { useQuery } from '@tanstack/react-query'
import { emailApi } from '../api/email-api'

export const EMAIL_PROVIDERS_KEY = ['email', 'providers'] as const

export function useEmailProviders() {
  return useQuery({
    queryKey: EMAIL_PROVIDERS_KEY,
    queryFn: () => emailApi.getProviders(),
  })
}
