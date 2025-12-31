import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  Shield,
  ShieldCheck,
  Clock,
  Cpu,
  Copy,
  Check,
  Pencil,
  LogOut,
  Loader2,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { RelativeTime } from '@/components/shared'
import { ConfirmDialog } from '@/components/shared/feedback'
import { useCopyToClipboard } from '@/hooks'
import type { DeviceSessionDto } from '../types'

interface SessionDetailsSheetProps {
  session: DeviceSessionDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onRevoke: (sessionId: string) => void
  onTrust?: (sessionId: string) => void
  onRename?: (sessionId: string, name: string) => void
  isRevoking?: boolean
  isTrusting?: boolean
  isRenaming?: boolean
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

function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

interface DetailRowProps {
  label: string
  value: string | null | undefined
  fallback?: string
  copyable?: boolean
}

function DetailRow({ label, value, fallback = '-', copyable }: DetailRowProps) {
  const { copy, copied } = useCopyToClipboard()
  const displayValue = value || fallback

  return (
    <div className="flex justify-between items-center py-2">
      <span className="text-sm text-muted-foreground">{label}</span>
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium">{displayValue}</span>
        {copyable && value && (
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => copy(value)}
          >
            {copied ? (
              <Check className="h-3 w-3 text-green-500" />
            ) : (
              <Copy className="h-3 w-3" />
            )}
          </Button>
        )}
      </div>
    </div>
  )
}

export function SessionDetailsSheet({
  session,
  open,
  onOpenChange,
  onRevoke,
  onTrust,
  onRename,
  isRevoking,
  isTrusting,
  isRenaming,
}: SessionDetailsSheetProps) {
  const { t } = useTranslation()
  const [showRenameDialog, setShowRenameDialog] = useState(false)
  const [showRevokeDialog, setShowRevokeDialog] = useState(false)
  const [newName, setNewName] = useState('')

  if (!session) return null

  const isLoading = isRevoking || isTrusting || isRenaming

  const handleRename = () => {
    if (newName.trim() && onRename) {
      onRename(session.id, newName.trim())
      setShowRenameDialog(false)
      setNewName('')
    }
  }

  const handleRevoke = () => {
    onRevoke(session.id)
    setShowRevokeDialog(false)
    onOpenChange(false)
  }

  const handleOpenRenameDialog = () => {
    setNewName(session.deviceName || '')
    setShowRenameDialog(true)
  }

  // Build browser string
  const browserString = session.browser
    ? session.browserVersion
      ? `${session.browser} ${session.browserVersion}`
      : session.browser
    : null

  // Build OS string
  const osString = session.operatingSystem
    ? session.osVersion
      ? `${session.operatingSystem} ${session.osVersion}`
      : session.operatingSystem
    : null

  // Device type display
  const deviceTypeDisplay = session.deviceType
    ? session.deviceType.charAt(0).toUpperCase() + session.deviceType.slice(1)
    : null

  // Check if we have any actions to show
  const hasActions = onRename || (onTrust && !session.isCurrent && !session.isTrusted) || !session.isCurrent

  return (
    <>
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent className="w-full sm:max-w-md flex flex-col p-0">
          <SheetHeader className="sr-only">
            <SheetTitle>{t('sessions:details.title')}</SheetTitle>
            <SheetDescription>
              {t('sessions:details.description')}
            </SheetDescription>
          </SheetHeader>

          {/* Fixed top section */}
          <div className="space-y-6 p-6 pb-0 shrink-0">
            {/* Session Header */}
            <div className="flex items-start gap-4">
              <div className="flex-shrink-0 p-3 rounded-full bg-muted">
                <DeviceIcon deviceType={session.deviceType} className="h-6 w-6 text-muted-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <h4 className="font-medium text-lg">
                  {session.deviceName || session.displayName}
                </h4>
                {session.deviceName && (
                  <p className="text-sm text-muted-foreground">
                    {session.displayName}
                  </p>
                )}
                <div className="flex items-center gap-2 mt-2 flex-wrap">
                  {session.isCurrent && (
                    <Badge variant="default">
                      {t('sessions:current')}
                    </Badge>
                  )}
                  {session.isTrusted && (
                    <Badge variant="secondary">
                      <ShieldCheck className="h-3 w-3 mr-1" />
                      {t('sessions:trusted')}
                    </Badge>
                  )}
                </div>
              </div>
            </div>

            {/* Device Information */}
            <div className="space-y-4 rounded-lg border p-4">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Cpu className="h-4 w-4 text-muted-foreground" />
                {t('sessions:details.deviceInfo')}
              </div>
              <div className="space-y-1">
                <DetailRow
                  label={t('sessions:details.browser')}
                  value={browserString}
                />
                <DetailRow
                  label={t('sessions:details.os')}
                  value={osString}
                />
                <DetailRow
                  label={t('sessions:details.deviceType')}
                  value={deviceTypeDisplay}
                />
              </div>
            </div>
          </div>

          {/* Scrollable content section */}
          <div className="flex-1 overflow-y-auto px-6 py-4 min-h-0 space-y-4">
            {/* Location */}
            <div className="space-y-4 rounded-lg border p-4">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Globe className="h-4 w-4 text-muted-foreground" />
                {t('sessions:details.location')}
              </div>
              <div className="space-y-1">
                <DetailRow
                  label={t('sessions:details.ipAddress')}
                  value={session.ipAddress}
                  copyable
                />
                <DetailRow
                  label={t('sessions:details.locationLabel')}
                  value={session.locationDisplay}
                />
              </div>
            </div>

            {/* Timeline */}
            <div className="space-y-4 rounded-lg border p-4">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Clock className="h-4 w-4 text-muted-foreground" />
                {t('sessions:details.timeline')}
              </div>
              <div className="space-y-1">
                <div className="flex justify-between items-center py-2">
                  <span className="text-sm text-muted-foreground">
                    {t('sessions:created')}
                  </span>
                  <span className="text-sm font-medium">
                    {formatDateTime(session.createdAt)}
                  </span>
                </div>
                <div className="flex justify-between items-center py-2">
                  <span className="text-sm text-muted-foreground">
                    {t('sessions:lastActive')}
                  </span>
                  <span className="text-sm font-medium">
                    <RelativeTime date={session.lastActivityAt} />
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Fixed bottom actions */}
          {hasActions && (
            <div className="shrink-0 border-t p-6 flex flex-col gap-2">
              {onRename && (
                <Button
                  variant="outline"
                  onClick={handleOpenRenameDialog}
                  disabled={isLoading}
                >
                  <Pencil className="h-4 w-4 mr-2" />
                  {t('sessions:rename.button')}
                </Button>
              )}

              {onTrust && !session.isCurrent && !session.isTrusted && (
                <Button
                  variant="outline"
                  onClick={() => onTrust(session.id)}
                  disabled={isLoading}
                >
                  {isTrusting ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : (
                    <Shield className="h-4 w-4 mr-2" />
                  )}
                  {t('sessions:trust.button')}
                </Button>
              )}

              {!session.isCurrent && (
                <Button
                  variant="destructive"
                  onClick={() => setShowRevokeDialog(true)}
                  disabled={isLoading}
                >
                  {isRevoking ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : (
                    <LogOut className="h-4 w-4 mr-2" />
                  )}
                  {t('sessions:revoke.button')}
                </Button>
              )}
            </div>
          )}
        </SheetContent>
      </Sheet>

      {/* Rename Dialog */}
      {onRename && (
        <Dialog open={showRenameDialog} onOpenChange={setShowRenameDialog}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{t('sessions:rename.title')}</DialogTitle>
              <DialogDescription>
                {session.displayName}
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-2">
              <Label htmlFor="session-name">{t('sessions:rename.placeholder')}</Label>
              <Input
                id="session-name"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder={session.displayName}
              />
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowRenameDialog(false)}>
                {t('common:actions.cancel')}
              </Button>
              <Button onClick={handleRename} disabled={!newName.trim() || isRenaming}>
                {isRenaming ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : null}
                {t('common:actions.save')}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Revoke Confirm Dialog */}
      <ConfirmDialog
        open={showRevokeDialog}
        onOpenChange={setShowRevokeDialog}
        title={t('sessions:revoke.title')}
        description={t('sessions:revoke.description')}
        confirmLabel={t('sessions:revoke.button')}
        variant="destructive"
        onConfirm={handleRevoke}
        isLoading={isRevoking}
      />
    </>
  )
}
