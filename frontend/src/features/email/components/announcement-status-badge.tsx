import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { EmailAnnouncementStatus } from '../types'

interface AnnouncementStatusBadgeProps {
  status: EmailAnnouncementStatus
  className?: string
}

export function AnnouncementStatusBadge({
  status,
  className,
}: AnnouncementStatusBadgeProps) {
  const { t } = useTranslation('email')

  const config = getStatusConfig(status)

  return (
    <Badge variant="outline" className={cn(config.className, className)}>
      {t(config.label)}
    </Badge>
  )
}

function getStatusConfig(
  status: EmailAnnouncementStatus
): { label: string; className: string } {
  switch (status) {
    case EmailAnnouncementStatus.Draft:
      return {
        label: 'announcements.status.draft',
        className: 'border-slate-500 text-slate-700 dark:text-slate-300',
      }
    case EmailAnnouncementStatus.Sending:
      return {
        label: 'announcements.status.sending',
        className: 'border-blue-500 text-blue-700 dark:text-blue-300',
      }
    case EmailAnnouncementStatus.Sent:
      return {
        label: 'announcements.status.sent',
        className: 'border-green-500 text-green-700 dark:text-green-300',
      }
    case EmailAnnouncementStatus.PartiallyFailed:
      return {
        label: 'announcements.status.partiallyFailed',
        className: 'border-orange-500 text-orange-700 dark:text-orange-300',
      }
    default:
      return {
        label: 'announcements.status.draft',
        className: 'border-slate-500 text-slate-700 dark:text-slate-300',
      }
  }
}
