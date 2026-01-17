import { useInfiniteQuery } from '@tanstack/react-query'
import { emailApi, type EmailLogsResponse } from '../api/email-api'
import type { EmailStatus } from '../types'

export const EMAIL_LOGS_KEY = ['email', 'logs'] as const

export interface UseEmailLogsOptions {
  search?: string
  status?: EmailStatus
  templateName?: string
  recipientUserId?: string
  announcementId?: string
  fromDate?: string
  toDate?: string
  enabled?: boolean
}

export function useEmailLogs(options: UseEmailLogsOptions = {}) {
  const { enabled = true, ...filters } = options

  return useInfiniteQuery<EmailLogsResponse>({
    queryKey: [...EMAIL_LOGS_KEY, filters],
    queryFn: ({ pageParam }) =>
      emailApi.getLogs({
        ...filters,
        cursor: pageParam as string | undefined,
      }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
    enabled,
  })
}
