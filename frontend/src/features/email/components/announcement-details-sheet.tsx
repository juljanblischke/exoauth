import { useTranslation } from 'react-i18next'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Separator } from '@/components/ui/separator'
import { Progress } from '@/components/ui/progress'
import { RelativeTime } from '@/components/shared/relative-time'
import { AnnouncementStatusBadge } from './announcement-status-badge'
import { AnnouncementTargetBadge } from './announcement-target-badge'
import { EmailAnnouncementStatus, type EmailAnnouncementDetailDto } from '../types'

interface AnnouncementDetailsSheetProps {
  announcement: EmailAnnouncementDetailDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AnnouncementDetailsSheet({
  announcement,
  open,
  onOpenChange,
}: AnnouncementDetailsSheetProps) {
  const { t } = useTranslation()

  if (!announcement) return null

  const isDraft = announcement.status === EmailAnnouncementStatus.Draft

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle>{announcement.subject}</SheetTitle>
          <SheetDescription>
            {t('email:announcements.columns.createdBy')}:{' '}
            {announcement.createdByUserFullName ?? '-'}
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-6">
          {/* Status & Target */}
          <div className="flex flex-wrap gap-2">
            <AnnouncementStatusBadge status={announcement.status} />
            <AnnouncementTargetBadge
              target={announcement.targetType}
              permission={announcement.targetPermission}
            />
          </div>

          <Separator />

          {/* Progress (if not draft) */}
          {!isDraft && (
            <>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">
                    {t('email:announcements.columns.progress')}
                  </span>
                  <span>
                    {announcement.sentCount} / {announcement.totalRecipients}
                  </span>
                </div>
                <Progress value={announcement.progress} />
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>
                    {t('email:announcements.stats.sent')}: {announcement.sentCount}
                  </span>
                  <span>
                    {t('email:announcements.stats.failed')}: {announcement.failedCount}
                  </span>
                </div>
              </div>
              <Separator />
            </>
          )}

          {/* Timestamps */}
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  {t('email:announcements.columns.createdAt')}
                </p>
                <RelativeTime date={announcement.createdAt} />
              </div>
              {announcement.sentAt && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('email:announcements.columns.sentAt')}
                  </p>
                  <RelativeTime date={announcement.sentAt} />
                </div>
              )}
            </div>
          </div>

          <Separator />

          {/* Email Body Preview */}
          <div>
            <p className="text-sm font-medium text-muted-foreground mb-2">
              {t('email:announcements.preview.body')}
            </p>
            <div
              className="prose prose-sm dark:prose-invert max-w-none p-4 border rounded-md bg-muted/30"
              dangerouslySetInnerHTML={{ __html: announcement.htmlBody }}
            />
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
