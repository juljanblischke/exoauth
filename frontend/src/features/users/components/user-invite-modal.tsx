import { useMemo, useState, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { FormModal } from '@/components/shared/form'
import { LoadingSpinner } from '@/components/shared/feedback'
import { getErrorMessage } from '@/lib/error-utils'

import { useInviteUser } from '../hooks'
import { useSystemPermissions } from '@/features/permissions'
import { createInviteUserSchema, type InviteUserFormData } from '../types'

interface UserInviteModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function UserInviteModal({ open, onOpenChange }: UserInviteModalProps) {
  const { t } = useTranslation()
  const { mutate: inviteUser, isPending } = useInviteUser()
  const { data: permissionGroups, isLoading: permissionsLoading } = useSystemPermissions()
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([])

  const inviteSchema = useMemo(() => createInviteUserSchema(t), [t])

  const form = useForm<InviteUserFormData>({
    resolver: zodResolver(inviteSchema),
    defaultValues: {
      email: '',
      firstName: '',
      lastName: '',
      permissionIds: [],
    },
  })

  // Handle open change with form reset
  const handleOpenChange = useCallback(
    (newOpen: boolean) => {
      if (!newOpen) {
        form.reset()
        setSelectedPermissions([])
      }
      onOpenChange(newOpen)
    },
    [form, onOpenChange]
  )

  const handlePermissionToggle = (permissionId: string) => {
    setSelectedPermissions((prev) =>
      prev.includes(permissionId)
        ? prev.filter((id) => id !== permissionId)
        : [...prev, permissionId]
    )
  }

  const handleSubmit = () => {
    const values = form.getValues()
    inviteUser(
      {
        email: values.email,
        firstName: values.firstName,
        lastName: values.lastName,
        permissionIds: selectedPermissions,
      },
      {
        onSuccess: () => {
          toast.success(t('users:messages.inviteSuccess'))
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
      onOpenChange={handleOpenChange}
      title={t('users:inviteUser')}
      description={t('users:invite.description', 'Send an invitation to join the team')}
      onSubmit={form.handleSubmit(handleSubmit)}
      submitLabel={t('users:invite.submit', 'Send Invitation')}
      isSubmitting={isPending}
      isDirty={form.formState.isDirty || selectedPermissions.length > 0}
      size="lg"
    >
      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">{t('users:fields.email')}</Label>
          <Input
            id="email"
            type="email"
            placeholder="name@example.com"
            {...form.register('email')}
          />
          {form.formState.errors.email && (
            <p className="text-sm text-destructive">
              {form.formState.errors.email.message}
            </p>
          )}
        </div>

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

        <div className="space-y-3">
          <Label>{t('users:permissions.title', 'Permissions')}</Label>
          {permissionsLoading ? (
            <LoadingSpinner size="sm" />
          ) : (
            <div className="space-y-4 max-h-64 overflow-y-auto rounded-md border p-4">
              {permissionGroups?.map((group) => (
                <div key={group.category} className="space-y-2">
                  <h4 className="text-sm font-medium capitalize">
                    {group.category}
                  </h4>
                  <div className="space-y-2 pl-2">
                    {group.permissions.map((permission) => (
                      <div
                        key={permission.id}
                        className="flex items-start gap-2"
                      >
                        <Checkbox
                          id={permission.id}
                          checked={selectedPermissions.includes(permission.id)}
                          onCheckedChange={() =>
                            handlePermissionToggle(permission.id)
                          }
                        />
                        <div className="flex flex-col">
                          <label
                            htmlFor={permission.id}
                            className="text-sm font-medium cursor-pointer"
                          >
                            {permission.name}
                          </label>
                          <span className="text-xs text-muted-foreground">
                            {permission.description}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </FormModal>
  )
}
