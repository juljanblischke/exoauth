import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  MoreVertical,
  LogOut,
  Pencil,
  Loader2,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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
import type { DeviceSessionDto } from '../types'

interface SessionCardProps {
  session: DeviceSessionDto
  onRevoke: (sessionId: string) => void
  onRename: (sessionId: string, name: string) => void
  onClick?: (session: DeviceSessionDto) => void
  isRevoking?: boolean
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

export function SessionCard({
  session,
  onRevoke,
  onRename,
  onClick,
  isRevoking,
  isRenaming,
}: SessionCardProps) {
  const { t } = useTranslation()
  const [showRenameDialog, setShowRenameDialog] = useState(false)
  const [newName, setNewName] = useState(session.deviceName || '')

  const isLoading = isRevoking || isRenaming

  const handleRename = () => {
    if (newName.trim()) {
      onRename(session.id, newName.trim())
      setShowRenameDialog(false)
    }
  }

  const handleCardClick = () => {
    if (onClick) {
      onClick(session)
    }
  }

  return (
    <>
      <div
        className={`flex items-start gap-4 p-4 rounded-lg border bg-card ${onClick ? 'cursor-pointer hover:bg-accent/50 transition-colors' : ''}`}
        onClick={handleCardClick}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
        onKeyDown={onClick ? (e) => e.key === 'Enter' && handleCardClick() : undefined}
      >
        <div className="flex-shrink-0 p-2 rounded-full bg-muted">
          <DeviceIcon deviceType={session.deviceType} className="h-5 w-5 text-muted-foreground" />
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h4 className="font-medium truncate">
              {session.displayName}
            </h4>
            {session.isCurrent && (
              <Badge variant="default" className="text-xs">
                {t('sessions:current')}
              </Badge>
            )}
          </div>

          <div className="mt-1 text-sm text-muted-foreground space-y-0.5">
            {session.locationDisplay && (
              <div className="flex items-center gap-1.5">
                <Globe className="h-3.5 w-3.5" />
                <span>{session.locationDisplay}</span>
              </div>
            )}
            <div className="flex items-center gap-1.5">
              <span>{t('sessions:lastActive')}:</span>
              <RelativeTime date={session.lastActivityAt} />
            </div>
          </div>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              disabled={isLoading}
              onClick={(e) => e.stopPropagation()}
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <MoreVertical className="h-4 w-4" />
              )}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); setShowRenameDialog(true) }}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('sessions:rename.button')}
            </DropdownMenuItem>
            {!session.isCurrent && (
              <DropdownMenuItem
                onClick={(e) => { e.stopPropagation(); onRevoke(session.id) }}
                className="text-destructive focus:text-destructive"
              >
                <LogOut className="h-4 w-4 mr-2" />
                {t('sessions:revoke.button')}
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

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
            <Button onClick={handleRename} disabled={!newName.trim()}>
              {t('common:actions.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
