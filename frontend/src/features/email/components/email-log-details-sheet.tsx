import { useTranslation } from 'react-i18next'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Separator } from '@/components/ui/separator'
import { RelativeTime } from '@/components/shared/relative-time'
import { EmailStatusBadge } from './email-status-badge'
import type { EmailLogDetailDto } from '../types'

interface EmailLogDetailsSheetProps {
  log: EmailLogDetailDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function EmailLogDetailsSheet({
  log,
  open,
  onOpenChange,
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

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle>{t('email:logs.details.title')}</SheetTitle>
          <SheetDescription>{log.subject}</SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-6">
          {/* Status */}
          <div>
            <EmailStatusBadge status={log.status} />
          </div>

          <Separator />

          {/* Recipient Info */}
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.details.recipientEmail')}
                </p>
                <p className="text-sm">{log.recipientEmail}</p>
              </div>
              {log.recipientUserFullName && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('email:logs.details.recipientUser')}
                  </p>
                  <p className="text-sm">{log.recipientUserFullName}</p>
                </div>
              )}
            </div>
          </div>

          <Separator />

          {/* Email Details */}
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.columns.template')}
                </p>
                <p className="text-sm font-mono">{log.templateName}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.details.language')}
                </p>
                <p className="text-sm uppercase">{log.language}</p>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.columns.provider')}
                </p>
                <p className="text-sm">{log.sentViaProviderName ?? '-'}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.details.retryCount')}
                </p>
                <p className="text-sm">{log.retryCount}</p>
              </div>
            </div>
          </div>

          <Separator />

          {/* Timestamps */}
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:logs.columns.queuedAt')}
                </p>
                <RelativeTime date={log.queuedAt} />
              </div>
              {log.sentAt && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('email:logs.columns.sentAt')}
                  </p>
                  <RelativeTime date={log.sentAt} />
                </div>
              )}
              {log.failedAt && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('email:logs.columns.status')}
                  </p>
                  <RelativeTime date={log.failedAt} />
                </div>
              )}
              {log.movedToDlqAt && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">
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
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-2">
                  {t('email:logs.details.lastError')}
                </p>
                <div className="bg-destructive/10 text-destructive p-3 rounded-md text-sm font-mono whitespace-pre-wrap break-all">
                  {log.lastError}
                </div>
              </div>
            </>
          )}

          {/* Template Variables */}
          {templateVariables && Object.keys(templateVariables).length > 0 && (
            <>
              <Separator />
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-2">
                  {t('email:logs.details.templateVariables')}
                </p>
                <div className="bg-muted p-3 rounded-md">
                  <pre className="text-xs overflow-x-auto">
                    {JSON.stringify(templateVariables, null, 2)}
                  </pre>
                </div>
              </div>
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
