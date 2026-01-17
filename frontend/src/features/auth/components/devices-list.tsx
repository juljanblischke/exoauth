import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { LoadingSpinner, EmptyState, ErrorState } from '@/components/shared/feedback'
import { ConfirmDialog } from '@/components/shared/feedback'
import { DeviceCard } from './device-card'
import { DeviceDetailsSheet } from './device-details-sheet'
import { RenameDeviceModal } from './rename-device-modal'
import { useDevices, useRevokeDevice, useRenameDevice, useApproveDeviceFromSession } from '../hooks'
import { normalizeDeviceStatus, type DeviceDto } from '../types/device'

interface DevicesListProps {
  showApprove?: boolean
  showRename?: boolean
}

export function DevicesList({ showApprove = true, showRename = true }: DevicesListProps) {
  const { t } = useTranslation()
  const { data: devices, isLoading, error, refetch } = useDevices()
  const { mutate: revokeDevice, isPending: isRevoking, variables: revokingId } = useRevokeDevice()
  const { mutate: renameDevice, isPending: isRenaming } = useRenameDevice()
  const { mutate: approveDevice, isPending: isApproving, variables: approvingId } = useApproveDeviceFromSession()

  const [deviceToRename, setDeviceToRename] = useState<DeviceDto | null>(null)
  const [deviceToRevoke, setDeviceToRevoke] = useState<string | null>(null)
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null)

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorState message={error.message} onRetry={refetch} />
  if (!devices?.length) {
    return (
      <EmptyState
        title={t('auth:devices.empty.title')}
        description={t('auth:devices.empty.description')}
      />
    )
  }

  // Sort: current first, then trusted, then pending, then revoked
  const sortedDevices = [...devices].sort((a, b) => {
    if (a.isCurrent && !b.isCurrent) return -1
    if (!a.isCurrent && b.isCurrent) return 1
    const statusOrder = { Trusted: 0, PendingApproval: 1, Revoked: 2 }
    const aStatus = normalizeDeviceStatus(a.status)
    const bStatus = normalizeDeviceStatus(b.status)
    return statusOrder[aStatus] - statusOrder[bStatus]
  })

  const handleRevoke = (deviceId: string) => {
    setDeviceToRevoke(deviceId)
  }

  const handleConfirmRevoke = () => {
    if (deviceToRevoke) {
      revokeDevice(deviceToRevoke)
      setDeviceToRevoke(null)
    }
  }

  const handleRename = (device: DeviceDto) => {
    setDeviceToRename(device)
  }

  const handleConfirmRename = (name: string) => {
    if (deviceToRename) {
      renameDevice(
        { deviceId: deviceToRename.id, request: { name } },
        { onSuccess: () => setDeviceToRename(null) }
      )
    }
  }

  const handleApprove = (deviceId: string) => {
    approveDevice(deviceId)
  }

  return (
    <>
      <div className="space-y-3">
        {sortedDevices.map((device) => (
          <DeviceCard
            key={device.id}
            device={device}
            onClick={setSelectedDevice}
            onRename={showRename ? handleRename : undefined}
            onRevoke={handleRevoke}
            onApprove={showApprove ? handleApprove : undefined}
            isRevoking={isRevoking && revokingId === device.id}
            isApproving={isApproving && approvingId === device.id}
            showRename={showRename}
            showApprove={showApprove}
          />
        ))}
      </div>

      <RenameDeviceModal
        open={!!deviceToRename}
        onOpenChange={(open) => !open && setDeviceToRename(null)}
        device={deviceToRename}
        onConfirm={handleConfirmRename}
        isLoading={isRenaming}
      />

      <ConfirmDialog
        open={!!deviceToRevoke}
        onOpenChange={(open) => !open && setDeviceToRevoke(null)}
        title={t('auth:devices.revoke.title')}
        description={t('auth:devices.revoke.description')}
        confirmLabel={t('auth:devices.actions.revoke')}
        onConfirm={handleConfirmRevoke}
        variant="destructive"
      />

      <DeviceDetailsSheet
        device={selectedDevice}
        open={!!selectedDevice}
        onOpenChange={(open) => !open && setSelectedDevice(null)}
        onRename={showRename ? (device) => {
          setSelectedDevice(null)
          handleRename(device)
        } : undefined}
        onRevoke={(device) => {
          setSelectedDevice(null)
          handleRevoke(device.id)
        }}
        onApprove={showApprove ? (device) => {
          setSelectedDevice(null)
          handleApprove(device.id)
        } : undefined}
      />
    </>
  )
}
