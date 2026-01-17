import { useInfiniteQuery } from '@tanstack/react-query'
import { emailApi, type DlqEmailsResponse } from '../api/email-api'

export const DLQ_KEY = ['email', 'dlq'] as const

export interface UseDeadLetterQueueOptions {
  search?: string
  enabled?: boolean
}

export function useDeadLetterQueue(options: UseDeadLetterQueueOptions = {}) {
  const { enabled = true, ...filters } = options

  return useInfiniteQuery<DlqEmailsResponse>({
    queryKey: [...DLQ_KEY, filters],
    queryFn: ({ pageParam }) =>
      emailApi.getDlqEmails({
        ...filters,
        cursor: pageParam as string | undefined,
      }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
    enabled,
  })
}
