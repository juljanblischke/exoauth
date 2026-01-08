import { useState, useMemo, useEffect, useCallback } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2, Users } from 'lucide-react'
import { z } from 'zod'

import { Button } from '@/components/ui/button'
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { RichTextEditor } from '@/components/shared/form/rich-text-editor'
import { UserSelectModal } from './user-select-modal'
import { useSystemPermissions } from '@/features/permissions/hooks/use-system-permissions'
import { EmailAnnouncementTarget } from '../types'
import type {
  CreateAnnouncementRequest,
  UpdateAnnouncementRequest,
  EmailAnnouncementDetailDto,
} from '../types'

function createSchema(t: (key: string) => string) {
  return z.object({
    subject: z
      .string()
      .min(1, t('validation:required'))
      .max(200, t('validation:maxLength')),
    htmlBody: z.string().min(1, t('validation:required')),
    targetType: z.nativeEnum(EmailAnnouncementTarget),
    targetPermission: z.string().optional(),
    targetUserIds: z.array(z.string()).optional(),
  })
}

type FormData = z.infer<ReturnType<typeof createSchema>>

interface AnnouncementFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  announcement?: EmailAnnouncementDetailDto | null
  onSubmit: (data: CreateAnnouncementRequest | UpdateAnnouncementRequest) => void
  isLoading?: boolean
}

export function AnnouncementFormModal({
  open,
  onOpenChange,
  announcement,
  onSubmit,
  isLoading,
}: AnnouncementFormModalProps) {
  const { t } = useTranslation()
  const schema = useMemo(() => createSchema(t), [t])
  const isEditing = !!announcement

  const [userSelectOpen, setUserSelectOpen] = useState(false)

  const { data: permissionGroups } = useSystemPermissions()
  
  // Flatten permission groups to get all individual permissions
  const allPermissions = useMemo(() => {
    if (!permissionGroups) return []
    return permissionGroups.flatMap(group => group.permissions)
  }, [permissionGroups])

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      subject: '',
      htmlBody: '',
      targetType: EmailAnnouncementTarget.AllUsers,
      targetPermission: undefined,
      targetUserIds: [],
    },
  })

  const targetType = form.watch('targetType')
  const targetUserIds = form.watch('targetUserIds') ?? []

  // Reset form when announcement changes
  useEffect(() => {
    if (announcement) {
      form.reset({
        subject: announcement.subject,
        htmlBody: announcement.htmlBody,
        targetType: announcement.targetType,
        targetPermission: announcement.targetPermission ?? undefined,
        targetUserIds: announcement.targetUserIds ?? [],
      })
    } else {
      form.reset({
        subject: '',
        htmlBody: '',
        targetType: EmailAnnouncementTarget.AllUsers,
        targetPermission: undefined,
        targetUserIds: [],
      })
    }
  }, [announcement, form])

  const handleSubmit = form.handleSubmit((data) => {
    const request: CreateAnnouncementRequest | UpdateAnnouncementRequest = {
      subject: data.subject,
      htmlBody: data.htmlBody,
      targetType: data.targetType,
      targetPermission:
        data.targetType === EmailAnnouncementTarget.ByPermission
          ? data.targetPermission
          : undefined,
      targetUserIds:
        data.targetType === EmailAnnouncementTarget.SelectedUsers
          ? data.targetUserIds
          : undefined,
    }
    onSubmit(request)
  })

  const handleClose = useCallback(() => {
    form.reset()
    onOpenChange(false)
  }, [form, onOpenChange])

  const handleUserSelectConfirm = useCallback(
    (userIds: string[]) => {
      form.setValue('targetUserIds', userIds)
    },
    [form]
  )

  return (
    <>
      <Dialog open={open} onOpenChange={handleClose}>
        <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
          <form onSubmit={handleSubmit}>
            <DialogHeader>
              <DialogTitle>
                {isEditing
                  ? t('email:announcements.form.edit')
                  : t('email:announcements.form.create')}
              </DialogTitle>
              <DialogDescription>
                {t('email:announcements.empty.description')}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-6 py-4">
              {/* Subject */}
              <div className="space-y-2">
                <Label htmlFor="subject">
                  {t('email:announcements.form.subject')}
                </Label>
                <Input
                  id="subject"
                  {...form.register('subject')}
                  placeholder={t('email:announcements.form.subjectPlaceholder')}
                  disabled={isLoading}
                />
                {form.formState.errors.subject && (
                  <p className="text-sm text-destructive">
                    {form.formState.errors.subject.message}
                  </p>
                )}
              </div>

              {/* Body */}
              <div className="space-y-2">
                <Label>{t('email:announcements.form.body')}</Label>
                <Controller
                  name="htmlBody"
                  control={form.control}
                  render={({ field }) => (
                    <RichTextEditor
                      value={field.value}
                      onChange={field.onChange}
                      placeholder={t('email:announcements.form.bodyPlaceholder')}
                      disabled={isLoading}
                    />
                  )}
                />
                {form.formState.errors.htmlBody && (
                  <p className="text-sm text-destructive">
                    {form.formState.errors.htmlBody.message}
                  </p>
                )}
              </div>

              {/* Target Type */}
              <div className="space-y-2">
                <Label>{t('email:announcements.form.target')}</Label>
                <Select
                  value={String(targetType)}
                  onValueChange={(value) =>
                    form.setValue('targetType', Number(value) as EmailAnnouncementTarget)
                  }
                  disabled={isLoading}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(EmailAnnouncementTarget.AllUsers)}>
                      {t('email:announcements.form.targetType.allUsers')}
                    </SelectItem>
                    <SelectItem value={String(EmailAnnouncementTarget.ByPermission)}>
                      {t('email:announcements.form.targetType.byPermission')}
                    </SelectItem>
                    <SelectItem value={String(EmailAnnouncementTarget.SelectedUsers)}>
                      {t('email:announcements.form.targetType.selectedUsers')}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Permission Selection (if ByPermission) */}
              {targetType === EmailAnnouncementTarget.ByPermission && (
                <div className="space-y-2">
                  <Label>{t('email:announcements.form.selectPermission')}</Label>
                  <Select
                    value={form.watch('targetPermission') ?? ''}
                    onValueChange={(value) =>
                      form.setValue('targetPermission', value)
                    }
                    disabled={isLoading}
                  >
                    <SelectTrigger>
                      <SelectValue
                        placeholder={t(
                          'email:announcements.form.selectPermissionPlaceholder'
                        )}
                      />
                    </SelectTrigger>
                    <SelectContent>
                      {allPermissions.map((permission) => (
                        <SelectItem key={permission.name} value={permission.name}>
                          {permission.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {/* User Selection (if SelectedUsers) */}
              {targetType === EmailAnnouncementTarget.SelectedUsers && (
                <div className="space-y-2">
                  <Label>{t('email:announcements.form.selectUsers')}</Label>
                  <div className="flex items-center gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setUserSelectOpen(true)}
                      disabled={isLoading}
                    >
                      <Users className="h-4 w-4 mr-2" />
                      {t('email:announcements.form.selectUsers')}
                    </Button>
                    <span className="text-sm text-muted-foreground">
                      {targetUserIds.length > 0
                        ? t('email:announcements.form.selectedCount', {
                            count: targetUserIds.length,
                          })
                        : t('email:announcements.form.noUsersSelected')}
                    </span>
                  </div>
                </div>
              )}
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={handleClose}
                disabled={isLoading}
              >
                {t('common:actions.cancel')}
              </Button>
              <Button type="submit" disabled={isLoading}>
                {isLoading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                {isEditing ? t('common:actions.save') : t('common:actions.create')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* User Select Modal */}
      <UserSelectModal
        open={userSelectOpen}
        onOpenChange={setUserSelectOpen}
        selectedUserIds={targetUserIds}
        onConfirm={handleUserSelectConfirm}
      />
    </>
  )
}
