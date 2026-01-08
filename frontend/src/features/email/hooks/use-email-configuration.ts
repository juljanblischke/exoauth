import { useQuery } from '@tanstack/react-query'
import { emailApi } from '../api/email-api'

export const EMAIL_CONFIGURATION_KEY = ['email', 'configuration'] as const

export function useEmailConfiguration() {
  return useQuery({
    queryKey: EMAIL_CONFIGURATION_KEY,
    queryFn: emailApi.getConfiguration,
  })
}
