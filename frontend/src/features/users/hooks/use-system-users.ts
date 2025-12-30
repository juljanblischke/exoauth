import { useInfiniteQuery } from '@tanstack/react-query'
import { usersApi } from '../api/users-api'
import type { SystemUsersQueryParams } from '../types'

export const SYSTEM_USERS_KEY = ['system', 'users'] as const

interface UseSystemUsersOptions {
  search?: string
  sort?: string
  limit?: number
  permissionIds?: string[]
  // User status filters
  isActive?: boolean
  isAnonymized?: boolean
  isLocked?: boolean
  mfaEnabled?: boolean
}

export function useSystemUsers(options: UseSystemUsersOptions = {}) {
  const {
    search,
    sort,
    limit = 20,
    permissionIds,
    isActive,
    isAnonymized,
    isLocked,
    mfaEnabled,
  } = options

  return useInfiniteQuery({
    queryKey: [
      ...SYSTEM_USERS_KEY,
      { search, sort, permissionIds, isActive, isAnonymized, isLocked, mfaEnabled },
    ],
    queryFn: async ({ pageParam }) => {
      const params: SystemUsersQueryParams = {
        cursor: pageParam as string | undefined,
        limit,
        search,
        sort,
        permissionIds,
        isActive,
        isAnonymized,
        isLocked,
        mfaEnabled,
      }
      return usersApi.getAll(params)
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
  })
}
