import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ipRestrictionsApi } from '../api/ip-restrictions-api'
import { IP_RESTRICTIONS_KEY } from './use-ip-restrictions'
import { getErrorMessage } from '@/lib/error-utils'

export function useDeleteIpRestriction() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => ipRestrictionsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: IP_RESTRICTIONS_KEY })
      toast.success(t('ipRestrictions:delete.success'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
