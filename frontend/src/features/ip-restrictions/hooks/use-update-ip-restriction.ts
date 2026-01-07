import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ipRestrictionsApi } from '../api/ip-restrictions-api'
import { IP_RESTRICTIONS_KEY } from './use-ip-restrictions'
import { getErrorMessage } from '@/lib/error-utils'
import type { UpdateIpRestrictionRequest } from '../types'

interface UpdateIpRestrictionParams {
  id: string
  request: UpdateIpRestrictionRequest
}

export function useUpdateIpRestriction() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ id, request }: UpdateIpRestrictionParams) =>
      ipRestrictionsApi.update(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: IP_RESTRICTIONS_KEY })
      toast.success(t('ipRestrictions:update.success'))
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, t))
    },
  })
}
