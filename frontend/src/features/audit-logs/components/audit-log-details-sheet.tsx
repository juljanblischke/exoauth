import { useTranslation } from 'react-i18next'
import { Clock, User, Activity, Globe, Monitor, FileJson, Box } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { UserAvatar } from '@/components/shared/user-avatar'
import { RelativeTime } from '@/components/shared/relative-time'
import type { SystemAuditLogDto } from '../types'

interface AuditLogDetailsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  log: SystemAuditLogDto | null
  onUserClick?: (userId: string) => void
}

export function AuditLogDetailsSheet({
  open,
  onOpenChange,
  log,
  onUserClick,
}: AuditLogDetailsSheetProps) {
  const { t } = useTranslation()

  if (!log) return null

  const hasDetails = log.details && Object.keys(log.details).length > 0

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg flex flex-col p-0">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('auditLogs:details.title')}</SheetTitle>
          <SheetDescription>{t('auditLogs:details.description')}</SheetDescription>
        </SheetHeader>

        {/* Header */}
        <div className="p-6 pb-4 border-b space-y-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Activity className="h-5 w-5 text-primary" />
            </div>
            <div>
              <Badge variant="outline" className="font-mono text-sm">
                {log.action}
              </Badge>
              <p className="text-sm text-muted-foreground mt-1">
                <RelativeTime date={log.createdAt} />
              </p>
            </div>
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1">
          <div className="p-6 space-y-6">
            {/* User Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <User className="h-4 w-4" />
                {t('auditLogs:fields.user')}
              </div>
              {log.userId ? (
                <Button
                  variant="ghost"
                  className="w-full justify-start h-auto p-3 -ml-3"
                  onClick={() => onUserClick?.(log.userId!)}
                >
                  <div className="flex items-center gap-3">
                    <UserAvatar
                      name={log.userFullName || ''}
                      email={log.userEmail || ''}
                      size="sm"
                    />
                    <div className="text-left">
                      <p className="font-medium">{log.userFullName}</p>
                      <p className="text-sm text-muted-foreground">{log.userEmail}</p>
                    </div>
                  </div>
                </Button>
              ) : (
                <p className="text-sm text-muted-foreground pl-1">
                  {t('auditLogs:system')}
                </p>
              )}
            </div>

            {/* Time Section */}
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('auditLogs:fields.time')}
              </div>
              <p className="text-sm pl-6">
                {new Date(log.createdAt).toLocaleString()}
              </p>
            </div>

            {/* Entity Section */}
            {(log.entityType || log.entityId) && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <Box className="h-4 w-4" />
                  {t('auditLogs:fields.entity')}
                </div>
                <div className="pl-6 space-y-1">
                  {log.entityType && (
                    <p className="text-sm">
                      <span className="text-muted-foreground">{t('auditLogs:details.type')}:</span>{' '}
                      <span className="font-medium">{log.entityType}</span>
                    </p>
                  )}
                  {log.entityId && (
                    <p className="text-sm font-mono text-xs text-muted-foreground">
                      ID: {log.entityId}
                    </p>
                  )}
                </div>
              </div>
            )}

            {/* IP Address Section */}
            {log.ipAddress && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <Globe className="h-4 w-4" />
                  {t('auditLogs:fields.ipAddress')}
                </div>
                <p className="text-sm font-mono pl-6">{log.ipAddress}</p>
              </div>
            )}

            {/* User Agent Section */}
            {log.userAgent && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <Monitor className="h-4 w-4" />
                  {t('auditLogs:fields.userAgent')}
                </div>
                <p className="text-sm text-muted-foreground pl-6 break-all">
                  {log.userAgent}
                </p>
              </div>
            )}

            {/* Details Section */}
            {hasDetails && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <FileJson className="h-4 w-4" />
                  {t('auditLogs:fields.details')}
                </div>
                <pre className="text-xs bg-muted p-3 rounded-md overflow-x-auto">
                  {JSON.stringify(log.details, null, 2)}
                </pre>
              </div>
            )}
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
