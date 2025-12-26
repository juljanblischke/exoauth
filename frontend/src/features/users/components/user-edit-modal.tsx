/* eslint-disable react-hooks/incompatible-library */
import { useMemo, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { FormModal } from '@/components/shared/form'
import { getErrorMessage } from '@/lib/error-utils'

import { useUpdateUser } from '../hooks'
import { createEditUserSchema, type EditUserFormData, type SystemUserDto } from '../types'

interface UserEditModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: SystemUserDto | null
}

export function UserEditModal({ open, onOpenChange, user }: UserEditModalProps) {
  const { t } = useTranslation()
  const { mutate: updateUser, isPending } = useUpdateUser()

  const editSchema = useMemo(() => createEditUserSchema(t), [t])

  const form = useForm<EditUserFormData>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      isActive: true,
    },
  })

  // Update form when user changes
  useEffect(() => {
    if (user) {
      form.reset({
        firstName: user.firstName,
        lastName: user.lastName,
        isActive: user.isActive,
      })
    }
  }, [user, form])

  // Reset form when modal closes
  useEffect(() => {
    if (!open) {
      form.reset()
    }
  }, [open, form])

  const handleSubmit = () => {
    const data = form.getValues()
    if (!user) return

    updateUser(
      {
        id: user.id,
        data: {
          firstName: data.firstName,
          lastName: data.lastName,
          isActive: data.isActive,
        },
      },
      {
        onSuccess: () => {
          toast.success(t('users:messages.updateSuccess'))
          onOpenChange(false)
        },
        onError: (error) => {
          toast.error(getErrorMessage(error, t))
        },
      }
    )
  }

  return (
    <FormModal
      open={open}
      onOpenChange={onOpenChange}
      title={t('users:editUser')}
      description={user?.email}
      onSubmit={form.handleSubmit(handleSubmit)}
      submitLabel={t('common:actions.save')}
      isSubmitting={isPending}
      isDirty={form.formState.isDirty}
      size="md"
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="firstName">{t('auth:register.firstName')}</Label>
            <Input id="firstName" {...form.register('firstName')} />
            {form.formState.errors.firstName && (
              <p className="text-sm text-destructive">
                {form.formState.errors.firstName.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName">{t('auth:register.lastName')}</Label>
            <Input id="lastName" {...form.register('lastName')} />
            {form.formState.errors.lastName && (
              <p className="text-sm text-destructive">
                {form.formState.errors.lastName.message}
              </p>
            )}
          </div>
        </div>

        <div className="flex items-center justify-between rounded-lg border p-4">
          <div className="space-y-0.5">
            <Label htmlFor="isActive">{t('users:fields.status')}</Label>
            <p className="text-sm text-muted-foreground">
              {form.watch('isActive')
                ? t('users:status.active')
                : t('users:status.inactive')}
            </p>
          </div>
          <Switch
            id="isActive"
            checked={form.watch('isActive')}
            onCheckedChange={(checked) => form.setValue('isActive', checked, { shouldDirty: true })}
          />
        </div>
      </div>
    </FormModal>
  )
}
