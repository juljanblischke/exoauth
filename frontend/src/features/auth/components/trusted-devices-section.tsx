import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, Trash2, Smartphone } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { LoadingSpinner, EmptyState } from '@/components/shared/feedback'
import { ConfirmDialog } from '@/components/shared/feedback'
import { TrustedDeviceCard } from './trusted-device-card'
import { RenameDeviceModal } from './rename-device-modal'
import {
  useTrustedDevices,
  useRemoveTrustedDevice,
  useRenameTrustedDevice,
  useRemoveAllOtherDevices,
} from '../hooks'
import type { TrustedDeviceDto } from '../types/trusted-device'

export function TrustedDevicesSection() {
  const { t } = useTranslation()
  const [deviceToRename, setDeviceToRename] = useState<TrustedDeviceDto | null>(
    null
  )
  const [deviceToRemove, setDeviceToRemove] = useState<string | null>(null)

  const { data: devices, isLoading, error } = useTrustedDevices()
  const removeTrustedDevice = useRemoveTrustedDevice()
  const renameTrustedDevice = useRenameTrustedDevice()
  const removeAllOtherDevices = useRemoveAllOtherDevices()

  const handleRename = (deviceId: string, name: string) => {
    renameTrustedDevice.mutate(
      { deviceId, request: { name } },
      {
        onSuccess: () => setDeviceToRename(null),
      }
    )
  }

  const handleRemove = () => {
    if (deviceToRemove) {
      removeTrustedDevice.mutate(deviceToRemove, {
        onSuccess: () => setDeviceToRemove(null),
      })
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-medium">
              {t('auth:trustedDevices.title')}
            </h3>
            <p className="text-sm text-muted-foreground">
              {t('auth:trustedDevices.description')}
            </p>
          </div>
        </div>
        <LoadingSpinner />
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-medium">
              {t('auth:trustedDevices.title')}
            </h3>
            <p className="text-sm text-muted-foreground">
              {t('auth:trustedDevices.description')}
            </p>
          </div>
        </div>
        <div className="text-center py-8 text-destructive">
          {t('errors:general.message')}
        </div>
      </div>
    )
  }

  const sortedDevices = [...(devices || [])].sort((a, b) => {
    if (a.isCurrent) return -1
    if (b.isCurrent) return 1
    const dateA = a.lastUsedAt ? new Date(a.lastUsedAt).getTime() : 0
    const dateB = b.lastUsedAt ? new Date(b.lastUsedAt).getTime() : 0
    return dateB - dateA
  })

  const otherDevicesCount = sortedDevices.filter((d) => !d.isCurrent).length

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-medium">
            {t('auth:trustedDevices.title')}
          </h3>
          <p className="text-sm text-muted-foreground">
            {t('auth:trustedDevices.description')}
          </p>
        </div>
      </div>

      {sortedDevices.length === 0 ? (
        <EmptyState
          title={t('auth:trustedDevices.noDevices')}
          icon={Smartphone}
        />
      ) : (
        <>
          <div className="space-y-3">
            {sortedDevices.map((device) => (
              <TrustedDeviceCard
                key={device.id}
                device={device}
                onRename={setDeviceToRename}
                onRemove={(id) => setDeviceToRemove(id)}
                isRemoving={
                  removeTrustedDevice.isPending &&
                  removeTrustedDevice.variables === device.id
                }
              />
            ))}
          </div>

          {otherDevicesCount > 0 && (
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button
                  variant="outline"
                  className="w-full"
                  disabled={removeAllOtherDevices.isPending}
                >
                  {removeAllOtherDevices.isPending ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : (
                    <Trash2 className="h-4 w-4 mr-2" />
                  )}
                  {t('auth:trustedDevices.removeOther')}
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>
                    {t('auth:trustedDevices.removeOther')}
                  </AlertDialogTitle>
                  <AlertDialogDescription>
                    {t('auth:trustedDevices.removeOtherConfirm')}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>
                    {t('common:actions.cancel')}
                  </AlertDialogCancel>
                  <AlertDialogAction
                    onClick={() => removeAllOtherDevices.mutate()}
                    className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  >
                    {t('auth:trustedDevices.removeOther')}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}
        </>
      )}

      <RenameDeviceModal
        device={deviceToRename}
        open={!!deviceToRename}
        onOpenChange={(open) => !open && setDeviceToRename(null)}
        onRename={handleRename}
        isRenaming={renameTrustedDevice.isPending}
      />

      <ConfirmDialog
        open={!!deviceToRemove}
        onOpenChange={(open) => !open && setDeviceToRemove(null)}
        title={t('auth:trustedDevices.remove')}
        description={t('auth:trustedDevices.removeConfirm')}
        confirmLabel={t('auth:trustedDevices.remove')}
        variant="destructive"
        onConfirm={handleRemove}
        isLoading={removeTrustedDevice.isPending}
      />
    </div>
  )
}
