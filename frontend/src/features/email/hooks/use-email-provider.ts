import { useQuery } from '@tanstack/react-query'
import { emailApi } from '../api/email-api'
import { EMAIL_PROVIDERS_KEY } from './use-email-providers'

export function useEmailProvider(id: string | undefined) {
  return useQuery({
    queryKey: [...EMAIL_PROVIDERS_KEY, id],
    queryFn: () => emailApi.getProvider(id!),
    enabled: !!id,
  })
}
