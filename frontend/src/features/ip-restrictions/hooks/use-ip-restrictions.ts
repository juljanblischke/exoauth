import { useInfiniteQuery } from '@tanstack/react-query'
import { ipRestrictionsApi } from '../api/ip-restrictions-api'
import type { IpRestrictionType, IpRestrictionSource } from '../types'

export const IP_RESTRICTIONS_KEY = ['system', 'ip-restrictions'] as const

interface UseIpRestrictionsOptions {
  sort?: string
  search?: string
  type?: IpRestrictionType
  source?: IpRestrictionSource
  includeExpired?: boolean
  limit?: number
}

export function useIpRestrictions(options: UseIpRestrictionsOptions = {}) {
  const { sort, search, type, source, includeExpired, limit = 20 } = options

  return useInfiniteQuery({
    queryKey: [...IP_RESTRICTIONS_KEY, { sort, search, type, source, includeExpired }],
    queryFn: async ({ pageParam }) => {
      return ipRestrictionsApi.getAll({
        cursor: pageParam as string | undefined,
        limit,
        sort,
        search,
        type,
        source,
        includeExpired,
      })
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
  })
}
