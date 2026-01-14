import { useTranslation } from 'react-i18next'
import { Mail, User, Clock, AlertTriangle, Server, FileText } from 'lucide-react'

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
import { Separator } from '@/components/ui/separator'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared/relative-time'
import { UserAvatar } from '@/components/shared/user-avatar'
import { EmailStatusBadge } from './email-status-badge'
import type { EmailLogDetailDto } from '../types'

interface EmailLogDetailsSheetProps {
  log: EmailLogDetailDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onUserClick?: (userId: string) => void
}

export function EmailLogDetailsSheet({
  log,
  open,
  onOpenChange,
  onUserClick,
}: EmailLogDetailsSheetProps) {
  const { t } = useTranslation()

  if (!log) return null

  // Parse template variables if available
  let templateVariables: Record<string, unknown> | null = null
  if (log.templateVariables) {
    try {
      templateVariables = JSON.parse(log.templateVariables)
    } catch {
      // Ignore parse errors
    }
  }

  const hasDetails = templateVariables && (
    typeof templateVariables === 'object'
      ? Object.keys(templateVariables).length > 0
      : true
  )

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg flex flex-col p-0 overflow-hidden">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('email:logs.details.title')}</SheetTitle>
          <SheetDescription>{log.subject}</SheetDescription>
        </SheetHeader>

        {/* Header */}
        <div className="p-6 pb-4 border-b space-y-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Mail className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <Tooltip>
                <TooltipTrigger asChild>
                  <p className="font-medium truncate cursor-default">{log.subject}</p>
                </TooltipTrigger>
                <TooltipContent className="max-w-[350px]">
                  <p>{log.subject}</p>
                </TooltipContent>
              </Tooltip>
              <div className="flex items-center gap-2 mt-1">
                <EmailStatusBadge status={log.status} />
                <span className="text-sm text-muted-foreground">
                  <RelativeTime date={log.queuedAt} />
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1 min-h-0">
          <div className="p-6 space-y-6">
            {/* Recipient Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <User className="h-4 w-4" />
                {t('email:logs.details.recipientUser')}
              </div>
              {log.recipientUserId ? (
                <Button
                  variant="ghost"
                  className="w-full justify-start h-auto p-3 -ml-3"
                  onClick={() => onUserClick?.(log.recipientUserId!)}
                >
                  <div className="flex items-center gap-3">
                    <UserAvatar
                      name={log.recipientUserFullName || ''}
                      email={log.recipientEmail}
                      size="sm"
                    />
                    <div className="text-left min-w-0 flex-1">
                      <p className="font-medium">{log.recipientUserFullName || log.recipientEmail}</p>
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <p className="text-sm text-muted-foreground truncate max-w-[250px]">{log.recipientEmail}</p>
                        </TooltipTrigger>
                        <TooltipContent>
                          <p>{log.recipientEmail}</p>
                        </TooltipContent>
                      </Tooltip>
                    </div>
                  </div>
                </Button>
              ) : (
                <div className="flex items-center gap-3 p-3 -ml-3">
                  <UserAvatar
                    name=""
                    email={log.recipientEmail}
                    size="sm"
                  />
                  <div className="min-w-0 flex-1">
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <p className="font-medium truncate max-w-[250px]">{log.recipientEmail}</p>
                      </TooltipTrigger>
                      <TooltipContent>
                        <p>{log.recipientEmail}</p>
                      </TooltipContent>
                    </Tooltip>
                    <p className="text-sm text-muted-foreground">{t('email:logs.details.noUser')}</p>
                  </div>
                </div>
              )}
            </div>

            <Separator />

            {/* Email Details */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <FileText className="h-4 w-4" />
                {t('email:logs.details.emailInfo')}
              </div>
              <div className="grid grid-cols-2 gap-4 pl-1">
                <div>
                  <p className="text-xs text-muted-foreground">
                    {t('email:logs.columns.template')}
                  </p>
                  <Badge variant="outline" className="font-mono text-xs mt-1">
                    {log.templateName}
                  </Badge>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">
                    {t('email:logs.details.language')}
                  </p>
                  <p className="text-sm uppercase mt-1">{log.language}</p>
                </div>
              </div>
            </div>

            <Separator />

            {/* Provider */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Server className="h-4 w-4" />
                {t('email:logs.columns.provider')}
              </div>
              <div className="grid grid-cols-2 gap-4 pl-1">
                <div>
                  <p className="text-xs text-muted-foreground">
                    {t('email:logs.details.sentVia')}
                  </p>
                  <p className="text-sm mt-1">{log.sentViaProviderName ?? '-'}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">
                    {t('email:logs.details.retryCount')}
                  </p>
                  <p className="text-sm mt-1">{log.retryCount}</p>
                </div>
              </div>
            </div>

            <Separator />

            {/* Timestamps */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('email:logs.details.timeline')}
              </div>
              <div className="space-y-2 pl-1">
                <div className="flex justify-between items-center">
                  <p className="text-sm text-muted-foreground">
                    {t('email:logs.columns.queuedAt')}
                  </p>
                  <RelativeTime date={log.queuedAt} />
                </div>
                {log.sentAt && (
                  <div className="flex justify-between items-center">
                    <p className="text-sm text-muted-foreground">
                      {t('email:logs.columns.sentAt')}
                    </p>
                    <RelativeTime date={log.sentAt} />
                  </div>
                )}
                {log.failedAt && (
                  <div className="flex justify-between items-center">
                    <p className="text-sm text-muted-foreground">
                      {t('email:logs.details.failedAt')}
                    </p>
                    <RelativeTime date={log.failedAt} />
                  </div>
                )}
                {log.movedToDlqAt && (
                  <div className="flex justify-between items-center">
                    <p className="text-sm text-muted-foreground">
                      {t('email:dlq.columns.movedAt')}
                    </p>
                    <RelativeTime date={log.movedToDlqAt} />
                  </div>
                )}
              </div>
            </div>

            {/* Error */}
            {log.lastError && (
              <>
                <Separator />
                <div className="space-y-3">
                  <div className="flex items-center gap-2 text-sm font-medium text-destructive">
                    <AlertTriangle className="h-4 w-4" />
                    {t('email:logs.details.lastError')}
                  </div>
                  <div className="bg-destructive/10 text-destructive p-3 rounded-md overflow-hidden">
                    <p className="text-sm font-mono whitespace-pre-wrap break-all">
                      {log.lastError}
                    </p>
                  </div>
                </div>
              </>
            )}

            {/* Template Variables */}
            {hasDetails && (
              <>
                <Separator />
                <div className="space-y-3">
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('email:logs.details.templateVariables')}
                  </p>
                  <div className="bg-muted p-3 rounded-md overflow-hidden">
                    <pre className="text-xs overflow-x-auto whitespace-pre-wrap break-all">
                      {JSON.stringify(templateVariables, null, 2)}
                    </pre>
                  </div>
                </div>
              </>
            )}
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
