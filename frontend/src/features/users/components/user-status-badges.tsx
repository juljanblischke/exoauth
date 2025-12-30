import { useTranslation } from 'react-i18next'
import { ShieldCheck, ShieldOff, Lock, UserX } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared/relative-time'
import { cn } from '@/lib/utils'
import type { SystemUserDto } from '../types'

interface UserStatusBadgesProps {
  user: Pick<SystemUserDto, 'mfaEnabled' | 'isLocked' | 'lockedUntil' | 'isAnonymized'>
  showMfa?: boolean
  showLocked?: boolean
  showAnonymized?: boolean
  className?: string
}

export function UserStatusBadges({
  user,
  showMfa = true,
  showLocked = true,
  showAnonymized = true,
  className,
}: UserStatusBadgesProps) {
  const { t } = useTranslation()

  return (
    <div className={cn('flex flex-wrap items-center gap-1.5', className)}>
      {showMfa && (
        <MfaBadge enabled={user.mfaEnabled} />
      )}
      {showLocked && user.isLocked && (
        <LockedBadge lockedUntil={user.lockedUntil} />
      )}
      {showAnonymized && user.isAnonymized && (
        <AnonymizedBadge />
      )}
    </div>
  )
}

interface MfaBadgeProps {
  enabled: boolean
}

export function MfaBadge({ enabled }: MfaBadgeProps) {
  const { t } = useTranslation()

  if (enabled) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="outline" className="gap-1 text-emerald-600 border-emerald-200 bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:bg-emerald-950">
            <ShieldCheck className="h-3 w-3" />
            <span className="text-xs">2FA</span>
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <p>{t('users:security.mfaEnabled')}</p>
        </TooltipContent>
      </Tooltip>
    )
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Badge variant="outline" className="gap-1 text-muted-foreground border-muted">
          <ShieldOff className="h-3 w-3" />
          <span className="text-xs">2FA</span>
        </Badge>
      </TooltipTrigger>
      <TooltipContent>
        <p>{t('users:security.mfaDisabled')}</p>
      </TooltipContent>
    </Tooltip>
  )
}

interface LockedBadgeProps {
  lockedUntil: string | null
}

export function LockedBadge({ lockedUntil }: LockedBadgeProps) {
  const { t } = useTranslation()

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Badge variant="outline" className="gap-1 text-red-600 border-red-200 bg-red-50 dark:text-red-400 dark:border-red-800 dark:bg-red-950">
          <Lock className="h-3 w-3" />
          <span className="text-xs">{t('users:security.locked')}</span>
        </Badge>
      </TooltipTrigger>
      <TooltipContent>
        <div className="space-y-1">
          <p>{t('users:security.accountLocked')}</p>
          {lockedUntil && (
            <p className="text-xs text-muted-foreground">
              {t('users:security.lockedUntil')}: <RelativeTime date={lockedUntil} />
            </p>
          )}
        </div>
      </TooltipContent>
    </Tooltip>
  )
}

export function AnonymizedBadge() {
  const { t } = useTranslation()

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Badge variant="outline" className="gap-1 text-slate-600 border-slate-200 bg-slate-50 dark:text-slate-400 dark:border-slate-700 dark:bg-slate-900">
          <UserX className="h-3 w-3" />
          <span className="text-xs">{t('users:security.anonymized')}</span>
        </Badge>
      </TooltipTrigger>
      <TooltipContent>
        <p>{t('users:security.userAnonymized')}</p>
      </TooltipContent>
    </Tooltip>
  )
}
