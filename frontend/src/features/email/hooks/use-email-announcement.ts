import { useQuery } from '@tanstack/react-query'
import { emailApi } from '../api/email-api'
import { EMAIL_ANNOUNCEMENTS_KEY } from './use-email-announcements'

export function useEmailAnnouncement(id: string, enabled = true) {
  return useQuery({
    queryKey: [...EMAIL_ANNOUNCEMENTS_KEY, id],
    queryFn: () => emailApi.getAnnouncement(id),
    enabled: enabled && !!id,
  })
}
