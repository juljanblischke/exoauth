import { useTranslation } from 'react-i18next'
import { Users, Shield, UserCheck } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { EmailAnnouncementTarget } from '../types'

interface AnnouncementTargetBadgeProps {
  target: EmailAnnouncementTarget
  permission?: string | null
  className?: string
}

export function AnnouncementTargetBadge({
  target,
  permission,
  className,
}: AnnouncementTargetBadgeProps) {
  const { t } = useTranslation('email')

  const config = getTargetConfig(target)
  const Icon = config.icon

  return (
    <Badge variant="secondary" className={cn('gap-1', className)}>
      <Icon className="h-3 w-3" />
      {t(config.label)}
      {target === EmailAnnouncementTarget.ByPermission && permission && (
        <span className="text-xs font-mono ml-1">({permission})</span>
      )}
    </Badge>
  )
}

function getTargetConfig(target: EmailAnnouncementTarget): {
  label: string
  icon: typeof Users
} {
  switch (target) {
    case EmailAnnouncementTarget.AllUsers:
      return {
        label: 'announcements.target.allUsers',
        icon: Users,
      }
    case EmailAnnouncementTarget.ByPermission:
      return {
        label: 'announcements.target.byPermission',
        icon: Shield,
      }
    case EmailAnnouncementTarget.SelectedUsers:
      return {
        label: 'announcements.target.selectedUsers',
        icon: UserCheck,
      }
    default:
      return {
        label: 'announcements.target.allUsers',
        icon: Users,
      }
  }
}
