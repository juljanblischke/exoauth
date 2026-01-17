import { useInfiniteQuery } from '@tanstack/react-query'
import { emailApi, type AnnouncementsResponse } from '../api/email-api'
import type { EmailAnnouncementStatus } from '../types'

export const EMAIL_ANNOUNCEMENTS_KEY = ['email', 'announcements'] as const

export interface UseEmailAnnouncementsOptions {
  status?: EmailAnnouncementStatus
  search?: string
  enabled?: boolean
}

export function useEmailAnnouncements(options: UseEmailAnnouncementsOptions = {}) {
  const { enabled = true, ...filters } = options

  return useInfiniteQuery<AnnouncementsResponse>({
    queryKey: [...EMAIL_ANNOUNCEMENTS_KEY, filters],
    queryFn: ({ pageParam }) =>
      emailApi.getAnnouncements({
        ...filters,
        cursor: pageParam as string | undefined,
      }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
    enabled,
  })
}
