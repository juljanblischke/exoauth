import { useInfiniteQuery } from '@tanstack/react-query'
import { invitesApi } from '../api/invites-api'
import type { InviteStatus, SystemInvitesQueryParams } from '../types'

export const SYSTEM_INVITES_KEY = ['system', 'invites'] as const

interface UseSystemInvitesOptions {
  search?: string
  statuses?: InviteStatus[]
  sort?: string
  includeExpired?: boolean
  includeRevoked?: boolean
  limit?: number
}

export function useSystemInvites(options: UseSystemInvitesOptions = {}) {
  const { search, statuses, sort, includeExpired, includeRevoked, limit = 20 } = options

  return useInfiniteQuery({
    queryKey: [...SYSTEM_INVITES_KEY, { search, statuses, sort, includeExpired, includeRevoked }],
    queryFn: async ({ pageParam }) => {
      const params: SystemInvitesQueryParams = {
        cursor: pageParam as string | undefined,
        limit,
        search,
        statuses,
        sort,
        includeExpired,
        includeRevoked,
      }
      return invitesApi.getAll(params)
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
  })
}
