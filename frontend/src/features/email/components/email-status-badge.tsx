import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { EmailStatus } from '../types'

interface EmailStatusBadgeProps {
  status: EmailStatus
  className?: string
}

export function EmailStatusBadge({ status, className }: EmailStatusBadgeProps) {
  const { t } = useTranslation('email')

  const config = getStatusConfig(status)

  return (
    <Badge variant="outline" className={cn(config.className, className)}>
      {t(config.label)}
    </Badge>
  )
}

function getStatusConfig(status: EmailStatus): { label: string; className: string } {
  switch (status) {
    case EmailStatus.Queued:
      return {
        label: 'logs.status.queued',
        className: 'border-slate-500 text-slate-700 dark:text-slate-300',
      }
    case EmailStatus.Sending:
      return {
        label: 'logs.status.sending',
        className: 'border-blue-500 text-blue-700 dark:text-blue-300',
      }
    case EmailStatus.Sent:
      return {
        label: 'logs.status.sent',
        className: 'border-green-500 text-green-700 dark:text-green-300',
      }
    case EmailStatus.Failed:
      return {
        label: 'logs.status.failed',
        className: 'border-red-500 text-red-700 dark:text-red-300',
      }
    case EmailStatus.InDlq:
      return {
        label: 'logs.status.inDlq',
        className: 'border-orange-500 text-orange-700 dark:text-orange-300',
      }
    case EmailStatus.RetriedFromDlq:
      return {
        label: 'logs.status.retriedFromDlq',
        className: 'border-purple-500 text-purple-700 dark:text-purple-300',
      }
    default:
      return {
        label: 'logs.status.queued',
        className: 'border-slate-500 text-slate-700 dark:text-slate-300',
      }
  }
}
