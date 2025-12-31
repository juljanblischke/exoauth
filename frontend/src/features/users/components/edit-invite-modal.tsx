import { useMemo, useState, useCallback, useEffect, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { z } from 'zod'

import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { FormModal } from '@/components/shared/form'
import { LoadingSpinner } from '@/components/shared/feedback'
import { getErrorMessage } from '@/lib/error-utils'

import { useUpdateInvite, useSystemInvite } from '../hooks'
import { useSystemPermissions } from '@/features/permissions'
import type { SystemInviteListDto } from '../types'

interface EditInviteModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invite: SystemInviteListDto | null
}

// Create schema with translations
function createEditInviteSchema(t: (key: string) => string) {
  return z.object({
    firstName: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
    lastName: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
  })
}

type EditInviteFormData = z.infer<ReturnType<typeof createEditInviteSchema>>

export function EditInviteModal({ open, onOpenChange, invite }: EditInviteModalProps) {
  const { t } = useTranslation()
  const { mutate: updateInvite, isPending } = useUpdateInvite()
  const { data: inviteDetails, isLoading: detailsLoading } = useSystemInvite(open ? invite?.id ?? null : null)
  const { data: permissionGroups, isLoading: permissionsLoading } = useSystemPermissions()
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([])

  const editInviteSchema = useMemo(() => createEditInviteSchema(t), [t])

  const form = useForm<EditInviteFormData>({
    resolver: zodResolver(editInviteSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
    },
  })

  // Use fetched details when available, fall back to prop for basic data
  const displayInvite = inviteDetails ?? invite

  // Compute matched permission IDs from invite details
  const matchedPermissionIds = useMemo(() => {
    if (!inviteDetails?.permissions || !permissionGroups) return []
    const invitePermissionNames = inviteDetails.permissions.map((p) => p.name)
    const matchedIds: string[] = []
    permissionGroups.forEach((group) => {
      group.permissions.forEach((permission) => {
        if (invitePermissionNames.includes(permission.name)) {
          matchedIds.push(permission.id)
        }
      })
    })
    return matchedIds
  }, [inviteDetails, permissionGroups])

  // Track which invite we've initialized for to avoid re-initializing
  const initializedInviteIdRef = useRef<string | null>(null)

  // Sync form and permissions when modal opens with new data
  useEffect(() => {
    if (displayInvite && open && initializedInviteIdRef.current !== displayInvite.id) {
      // Reset form with invite data
      form.reset({
        firstName: displayInvite.firstName,
        lastName: displayInvite.lastName,
      })
      initializedInviteIdRef.current = displayInvite.id
    }
    if (!open) {
      initializedInviteIdRef.current = null
    }
  }, [displayInvite, open, form])

  // Sync permissions separately when computed IDs change and modal is open
  // This is a legitimate modal initialization pattern - we need to sync state
  // from async data (invite details) when the modal opens
  const prevMatchedIdsRef = useRef<string[]>([])
  useEffect(() => {
    if (open && matchedPermissionIds.length > 0) {
      const prevIds = prevMatchedIdsRef.current
      const hasChanged = matchedPermissionIds.length !== prevIds.length ||
        matchedPermissionIds.some((id, i) => id !== prevIds[i])
      if (hasChanged) {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- Modal init from async data
        setSelectedPermissions(matchedPermissionIds)
        prevMatchedIdsRef.current = matchedPermissionIds
      }
    }
    if (!open) {
      prevMatchedIdsRef.current = []
    }
  }, [open, matchedPermissionIds])

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
    if (!invite) return

    const values = form.getValues()
    updateInvite(
      {
        id: invite.id,
        data: {
          firstName: values.firstName,
          lastName: values.lastName,
          permissionIds: selectedPermissions,
        },
      },
      {
        onSuccess: () => {
          toast.success(t('users:invites.edit.success'))
          onOpenChange(false)
        },
        onError: (error) => {
          toast.error(getErrorMessage(error, t))
        },
      }
    )
  }

  if (!invite) return null

  return (
    <FormModal
      open={open}
      onOpenChange={handleOpenChange}
      title={t('users:invites.edit.title')}
      description={t('users:invites.edit.description')}
      onSubmit={form.handleSubmit(handleSubmit)}
      submitLabel={t('common:actions.save')}
      isSubmitting={isPending}
      isDirty={form.formState.isDirty || selectedPermissions.length > 0}
      size="lg"
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

        <div className="space-y-3">
          <Label>{t('users:permissions.title')}</Label>
          {detailsLoading || permissionsLoading ? (
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
                          id={`edit-${permission.id}`}
                          checked={selectedPermissions.includes(permission.id)}
                          onCheckedChange={() =>
                            handlePermissionToggle(permission.id)
                          }
                        />
                        <div className="flex flex-col">
                          <label
                            htmlFor={`edit-${permission.id}`}
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
