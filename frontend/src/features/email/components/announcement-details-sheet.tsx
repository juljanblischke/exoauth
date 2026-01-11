import { useTranslation } from 'react-i18next'
import {
  Megaphone,
  User,
  Users,
  Clock,
  FileText,
  TrendingUp,
  CheckCircle,
  XCircle,
} from 'lucide-react'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Progress } from '@/components/ui/progress'
import { UserAvatar } from '@/components/shared/user-avatar'
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
      <SheetContent className="w-full sm:max-w-lg flex flex-col p-0 overflow-hidden">
        <SheetHeader className="sr-only">
          <SheetTitle>{announcement.subject}</SheetTitle>
          <SheetDescription>{t('email:announcements.details.description')}</SheetDescription>
        </SheetHeader>

        {/* Header */}
        <div className="p-6 pb-4 border-b space-y-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Megaphone className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <h2 className="font-semibold text-lg truncate">{announcement.subject}</h2>
              <div className="flex items-center gap-2 mt-1">
                <AnnouncementStatusBadge status={announcement.status} />
                <AnnouncementTargetBadge
                  target={announcement.targetType}
                  permission={announcement.targetPermission}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1 min-h-0">
          <div className="p-6 space-y-6">
            {/* Created By Section */}
            {announcement.createdByUserId && (
              <div className="space-y-3">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <User className="h-4 w-4" />
                  {t('email:announcements.columns.createdBy')}
                </div>
                <div className="flex items-center gap-3 pl-6">
                  <UserAvatar
                    name={announcement.createdByUserFullName || ''}
                    email=""
                    size="sm"
                  />
                  <div>
                    <p className="font-medium">{announcement.createdByUserFullName}</p>
                  </div>
                </div>
              </div>
            )}

            {/* Progress (if not draft) */}
            {!isDraft && (
              <div className="space-y-3">
                <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                  <TrendingUp className="h-4 w-4" />
                  {t('email:announcements.columns.progress')}
                </div>
                <div className="pl-6 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span>
                      {announcement.sentCount} / {announcement.totalRecipients}
                    </span>
                    <span className="text-muted-foreground">
                      {announcement.progress.toFixed(0)}%
                    </span>
                  </div>
                  <Progress value={announcement.progress} />
                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <CheckCircle className="h-3 w-3 text-green-500" />
                      {t('email:announcements.stats.sent')}: {announcement.sentCount}
                    </span>
                    <span className="flex items-center gap-1">
                      <XCircle className="h-3 w-3 text-destructive" />
                      {t('email:announcements.stats.failed')}: {announcement.failedCount}
                    </span>
                  </div>
                </div>
              </div>
            )}

            {/* Recipients Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Users className="h-4 w-4" />
                {t('email:announcements.columns.recipients')}
              </div>
              <div className="pl-6">
                <p className="text-sm">
                  <span className="font-medium">{announcement.totalRecipients}</span>
                  {' '}{t('email:announcements.preview.recipients').toLowerCase()}
                </p>
              </div>
            </div>

            {/* Timestamps Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('email:announcements.details.timestamps')}
              </div>
              <div className="pl-6 space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{t('email:announcements.columns.createdAt')}</span>
                  <RelativeTime date={announcement.createdAt} />
                </div>
                {announcement.sentAt && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{t('email:announcements.columns.sentAt')}</span>
                    <RelativeTime date={announcement.sentAt} />
                  </div>
                )}
              </div>
            </div>

            {/* Email Body Preview */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <FileText className="h-4 w-4" />
                {t('email:announcements.preview.body')}
              </div>
              <div className="pl-6">
                <div
                  className="prose prose-sm dark:prose-invert max-w-none p-4 border rounded-md bg-muted/30"
                  dangerouslySetInnerHTML={{ __html: announcement.htmlBody }}
                />
              </div>
            </div>
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
