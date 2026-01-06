import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { passkeysApi } from '../api/passkeys-api'
import { PASSKEYS_QUERY_KEY } from './use-passkeys'
import type { PasskeyDto, RenamePasskeyRequest } from '../types/passkey'

interface RenamePasskeyParams {
  id: string
  request: RenamePasskeyRequest
}

export function useRenamePasskey() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ id, request }: RenamePasskeyParams) =>
      passkeysApi.rename(id, request),
    onSuccess: (updatedPasskey) => {
      // Update the passkey in cache
      queryClient.setQueryData<PasskeyDto[]>(PASSKEYS_QUERY_KEY, (old) => {
        if (!old) return old
        return old.map((passkey) =>
          passkey.id === updatedPasskey.id ? updatedPasskey : passkey
        )
      })
      toast.success(t('auth:passkeys.rename.success'))
    },
  })
}
