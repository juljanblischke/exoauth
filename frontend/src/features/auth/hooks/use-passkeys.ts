import { useQuery } from '@tanstack/react-query'
import { passkeysApi } from '../api/passkeys-api'

export const PASSKEYS_QUERY_KEY = ['auth', 'passkeys'] as const

export function usePasskeys() {
  return useQuery({
    queryKey: PASSKEYS_QUERY_KEY,
    queryFn: passkeysApi.getAll,
  })
}
