import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, LogOut } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { SessionCard } from './session-card'
import { SessionDetailsSheet } from './session-details-sheet'
import {
  useSessions,
  useRevokeSession,
  useRevokeAllSessions,
  useTrustSession,
  useUpdateSession,
} from '../hooks'
import type { DeviceSessionDto } from '../types'

export function SessionsList() {
  const { t } = useTranslation()
  const [selectedSession, setSelectedSession] = useState<DeviceSessionDto | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  const { data: sessions, isLoading, error } = useSessions()
  const revokeSession = useRevokeSession()
  const revokeAllSessions = useRevokeAllSessions()
  const trustSession = useTrustSession()
  const updateSession = useUpdateSession()

  const handleSessionClick = (session: DeviceSessionDto) => {
    setSelectedSession(session)
    setSheetOpen(true)
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        <span className="ml-2 text-muted-foreground">{t('sessions:loading')}</span>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-8 text-destructive">
        {t('sessions:errors.loadFailed')}
      </div>
    )
  }

  if (!sessions || sessions.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        {t('sessions:empty')}
      </div>
    )
  }

  // Sort sessions: current first, then by last activity
  const sortedSessions = [...sessions].sort((a, b) => {
    if (a.isCurrent) return -1
    if (b.isCurrent) return 1
    return new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime()
  })

  const otherSessionsCount = sessions.filter((s) => !s.isCurrent).length

  return (
    <div className="space-y-4">
      <div className="space-y-3">
        {sortedSessions.map((session) => (
          <SessionCard
            key={session.id}
            session={session}
            onRevoke={(id) => revokeSession.mutate(id)}
            onTrust={(id) => trustSession.mutate(id)}
            onRename={(id, name) => updateSession.mutate({ sessionId: id, request: { name } })}
            onClick={handleSessionClick}
            isRevoking={revokeSession.isPending && revokeSession.variables === session.id}
            isTrusting={trustSession.isPending && trustSession.variables === session.id}
            isRenaming={updateSession.isPending && updateSession.variables?.sessionId === session.id}
          />
        ))}
      </div>

      {otherSessionsCount > 0 && (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button
              variant="outline"
              className="w-full"
              disabled={revokeAllSessions.isPending}
            >
              {revokeAllSessions.isPending ? (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <LogOut className="h-4 w-4 mr-2" />
              )}
              {t('sessions:revokeAll.button')}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('sessions:revokeAll.title')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('sessions:revokeAll.description')}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
              <AlertDialogAction
                onClick={() => revokeAllSessions.mutate()}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                {t('sessions:revokeAll.button')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}

      <SessionDetailsSheet
        session={selectedSession}
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        onRevoke={(id) => revokeSession.mutate(id)}
        onTrust={(id) => trustSession.mutate(id)}
        onRename={(id, name) => updateSession.mutate({ sessionId: id, request: { name } })}
        isRevoking={revokeSession.isPending && revokeSession.variables === selectedSession?.id}
        isTrusting={trustSession.isPending && trustSession.variables === selectedSession?.id}
        isRenaming={updateSession.isPending && updateSession.variables?.sessionId === selectedSession?.id}
      />
    </div>
  )
}
