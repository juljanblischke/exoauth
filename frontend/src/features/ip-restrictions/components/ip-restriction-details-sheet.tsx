import { useTranslation } from 'react-i18next'
import { Clock, User, FileText, Calendar, Pencil, Trash2, Shield, Globe } from 'lucide-react'
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
import { IpRestrictionTypeBadge } from './ip-restriction-type-badge'
import { IpRestrictionSourceBadge } from './ip-restriction-source-badge'
import type { IpRestrictionDto } from '../types'

interface IpRestrictionDetailsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  restriction: IpRestrictionDto | null
  onEdit?: (restriction: IpRestrictionDto) => void
  onDelete?: (id: string) => void
  onUserClick?: (userId: string) => void
  isDeleting?: boolean
}

export function IpRestrictionDetailsSheet({
  open,
  onOpenChange,
  restriction,
  onEdit,
  onDelete,
  onUserClick,
  isDeleting,
}: IpRestrictionDetailsSheetProps) {
  const { t } = useTranslation()

  if (!restriction) return null

  const isExpired = restriction.expiresAt && new Date(restriction.expiresAt) < new Date()

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg flex flex-col p-0 overflow-hidden">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('ipRestrictions:details.title')}</SheetTitle>
          <SheetDescription>{restriction.ipAddress}</SheetDescription>
        </SheetHeader>

        {/* Header */}
        <div className="p-6 pb-4 border-b space-y-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <div>
              <Badge variant="outline" className="font-mono text-sm">
                {restriction.ipAddress}
              </Badge>
              <p className="text-sm text-muted-foreground mt-1">
                <RelativeTime date={restriction.createdAt} />
              </p>
            </div>
          </div>
          {/* Status badges */}
          <div className="flex flex-wrap gap-2">
            <IpRestrictionTypeBadge type={restriction.type} />
            <IpRestrictionSourceBadge source={restriction.source} />
            {isExpired && (
              <Badge variant="destructive">{t('ipRestrictions:expired')}</Badge>
            )}
          </div>
        </div>

        {/* Content */}
        <ScrollArea className="flex-1 min-h-0">
          <div className="p-6 space-y-6">
            {/* Created By Section */}
            <div className="space-y-3">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <User className="h-4 w-4" />
                {t('ipRestrictions:details.createdBy')}
              </div>
              {restriction.createdByUserId ? (
                <Button
                  variant="ghost"
                  className="w-full justify-start h-auto p-3 -ml-3"
                  onClick={() => onUserClick?.(restriction.createdByUserId!)}
                >
                  <div className="flex items-center gap-3">
                    <UserAvatar
                      name={restriction.createdByUserFullName || ''}
                      email={restriction.createdByUserEmail || ''}
                      size="sm"
                    />
                    <div className="text-left">
                      <p className="font-medium">{restriction.createdByUserFullName}</p>
                      <p className="text-sm text-muted-foreground">{restriction.createdByUserEmail}</p>
                    </div>
                  </div>
                </Button>
              ) : (
                <p className="text-sm text-muted-foreground pl-1">
                  {t('ipRestrictions:details.system')}
                </p>
              )}
            </div>

            {/* IP Address Section */}
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Globe className="h-4 w-4" />
                {t('ipRestrictions:details.ipAddress')}
              </div>
              <p className="text-sm font-mono pl-6">{restriction.ipAddress}</p>
            </div>

            {/* Reason Section */}
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <FileText className="h-4 w-4" />
                {t('ipRestrictions:details.reason')}
              </div>
              <p className="text-sm pl-6">{restriction.reason}</p>
            </div>

            {/* Expires At Section */}
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('ipRestrictions:details.expiresAt')}
              </div>
              <div className="pl-6">
                {restriction.expiresAt ? (
                  <div className="space-y-1">
                    <p className="text-sm">
                      {new Date(restriction.expiresAt).toLocaleString()}
                    </p>
                    {isExpired ? (
                      <p className="text-sm text-destructive">
                        ({t('ipRestrictions:expired')})
                      </p>
                    ) : (
                      <p className="text-sm text-muted-foreground">
                        (<RelativeTime date={restriction.expiresAt} />)
                      </p>
                    )}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    {t('ipRestrictions:details.never')}
                  </p>
                )}
              </div>
            </div>

            {/* Created At Section */}
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                <Calendar className="h-4 w-4" />
                {t('ipRestrictions:details.createdAt')}
              </div>
              <p className="text-sm pl-6">
                {new Date(restriction.createdAt).toLocaleString()}
              </p>
            </div>
          </div>
        </ScrollArea>

        {/* Footer with Actions */}
        {(onEdit || onDelete) && (
          <div className="p-6 pt-4 border-t flex gap-2">
            {onEdit && (
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => onEdit(restriction)}
              >
                <Pencil className="mr-2 h-4 w-4" />
                {t('common:actions.edit')}
              </Button>
            )}
            {onDelete && (
              <Button
                variant="destructive"
                className="flex-1"
                onClick={() => onDelete(restriction.id)}
                disabled={isDeleting}
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {isDeleting ? t('common:states.deleting') : t('ipRestrictions:delete.confirm')}
              </Button>
            )}
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
