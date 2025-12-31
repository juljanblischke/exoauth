import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  ShieldCheck,
  Loader2,
  LogOut,
  X,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared'
import { ConfirmDialog } from '@/components/shared/feedback'
import { SessionDetailsSheet } from '@/features/auth/components'
import { useUserSessions, useRevokeUserSession, useRevokeUserSessions } from '../hooks'
import type { DeviceSessionDto } from '@/features/auth/types'

interface UserSessionsSectionProps {
  userId: string
  onSessionsRevoked?: () => void
}

function DeviceIcon({ deviceType, className }: { deviceType: string | null; className?: string }) {
  switch (deviceType?.toLowerCase()) {
    case 'mobile':
      return <Smartphone className={className} />
    case 'tablet':
      return <Tablet className={className} />
    default:
      return <Monitor className={className} />
  }
}

interface SessionItemProps {
  session: DeviceSessionDto
  onRevoke: (sessionId: string) => void
  onClick?: (session: DeviceSessionDto) => void
  isRevoking: boolean
  revokingSessionId: string | null
}

function SessionItem({ session, onRevoke, onClick, isRevoking, revokingSessionId }: SessionItemProps) {
  const { t } = useTranslation()
  const isThisRevoking = isRevoking && revokingSessionId === session.id

  const handleClick = () => {
    if (onClick) {
      onClick(session)
    }
  }

  return (
    <div
      className={`flex items-start gap-3 p-3 rounded-lg border bg-card ${onClick ? 'cursor-pointer hover:bg-accent/50 transition-colors' : ''}`}
      onClick={handleClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && handleClick() : undefined}
    >
      <div className="flex-shrink-0 p-2 rounded-full bg-muted">
        <DeviceIcon deviceType={session.deviceType} className="h-4 w-4 text-muted-foreground" />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium text-sm truncate">
            {session.displayName}
          </span>
          {session.isCurrent && (
            <Badge variant="default" className="text-xs">
              {t('sessions:current')}
            </Badge>
          )}
          {session.isTrusted && (
            <Badge variant="secondary" className="text-xs">
              <ShieldCheck className="h-3 w-3 mr-1" />
              {t('sessions:trusted')}
            </Badge>
          )}
        </div>

        <div className="mt-1 text-xs text-muted-foreground space-y-0.5">
          {session.locationDisplay && (
            <div className="flex items-center gap-1.5">
              <Globe className="h-3 w-3" />
              <span>{session.locationDisplay}</span>
            </div>
          )}
          <div className="flex items-center gap-1.5">
            <span>{t('sessions:lastActive')}:</span>
            <RelativeTime date={session.lastActivityAt} />
          </div>
        </div>
      </div>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-muted-foreground hover:text-destructive"
            onClick={(e) => {
              e.stopPropagation()
              onRevoke(session.id)
            }}
            disabled={isRevoking}
          >
            {isThisRevoking ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <X className="h-4 w-4" />
            )}
          </Button>
        </TooltipTrigger>
        <TooltipContent>
          {t('users:admin.sessions.revokeSingle')}
        </TooltipContent>
      </Tooltip>
    </div>
  )
}

export function UserSessionsSection({ userId, onSessionsRevoked }: UserSessionsSectionProps) {
  const { t } = useTranslation()
  const [showRevokeAllDialog, setShowRevokeAllDialog] = useState(false)
  const [revokingSessionId, setRevokingSessionId] = useState<string | null>(null)
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  const { data: sessions, isLoading } = useUserSessions(userId)
  const { mutate: revokeSession, isPending: isRevokingSingle } = useRevokeUserSession()
  const { mutate: revokeSessions, isPending: isRevokingAll } = useRevokeUserSessions()

  // Derive selectedSession from sessions data at render time
  const selectedSession = selectedSessionId
    ? sessions?.find((s) => s.id === selectedSessionId) ?? null
    : null

  const handleSessionClick = (session: DeviceSessionDto) => {
    setSelectedSessionId(session.id)
    setSheetOpen(true)
  }

  const handleRevokeFromSheet = (sessionId: string) => {
    setRevokingSessionId(sessionId)
    revokeSession(
      { userId, sessionId },
      {
        onSuccess: () => {
          setSheetOpen(false)
        },
        onSettled: () => {
          setRevokingSessionId(null)
        },
      }
    )
  }

  const handleRevokeSingle = (sessionId: string) => {
    setRevokingSessionId(sessionId)
    revokeSession(
      { userId, sessionId },
      {
        onSettled: () => {
          setRevokingSessionId(null)
        },
      }
    )
  }

  const handleRevokeAll = () => {
    revokeSessions(userId, {
      onSuccess: () => {
        setShowRevokeAllDialog(false)
        onSessionsRevoked?.()
      },
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-8 w-24" />
        </div>
        <div className="space-y-2">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      </div>
    )
  }

  const sessionCount = sessions?.length ?? 0

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium">
          {t('users:admin.sessions.title')} ({sessionCount})
        </h4>
        {sessionCount > 1 && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowRevokeAllDialog(true)}
            disabled={isRevokingAll}
          >
            {isRevokingAll ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <LogOut className="h-4 w-4 mr-2" />
            )}
            {t('users:admin.sessions.revokeAll')}
          </Button>
        )}
      </div>

      {sessionCount === 0 ? (
        <p className="text-sm text-muted-foreground py-4 text-center">
          {t('users:admin.sessions.empty')}
        </p>
      ) : (
        <div className="space-y-2 max-h-64 overflow-y-auto">
          {sessions?.map((session) => (
            <SessionItem
              key={session.id}
              session={session}
              onRevoke={handleRevokeSingle}
              onClick={handleSessionClick}
              isRevoking={isRevokingSingle}
              revokingSessionId={revokingSessionId}
            />
          ))}
        </div>
      )}

      <ConfirmDialog
        open={showRevokeAllDialog}
        onOpenChange={setShowRevokeAllDialog}
        title={t('users:admin.sessions.revokeAllConfirm.title')}
        description={t('users:admin.sessions.revokeAllConfirm.description')}
        confirmLabel={t('users:admin.sessions.revokeAll')}
        variant="destructive"
        onConfirm={handleRevokeAll}
        isLoading={isRevokingAll}
      />

      <SessionDetailsSheet
        session={selectedSession}
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        onRevoke={handleRevokeFromSheet}
        isRevoking={isRevokingSingle && revokingSessionId === selectedSession?.id}
      />
    </div>
  )
}
