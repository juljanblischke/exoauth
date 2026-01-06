import { useTranslation } from 'react-i18next'
import { StatusBadge } from '@/components/shared'
import { normalizeDeviceStatus, type DeviceStatus } from '../types/device'

interface DeviceStatusBadgeProps {
  status: DeviceStatus
  className?: string
}

const statusStyleMap: Record<'Trusted' | 'PendingApproval' | 'Revoked', 'success' | 'warning' | 'neutral'> = {
  Trusted: 'success',
  PendingApproval: 'warning',
  Revoked: 'neutral',
}

const i18nKeyMap: Record<'Trusted' | 'PendingApproval' | 'Revoked', string> = {
  Trusted: 'trusted',
  PendingApproval: 'pending',
  Revoked: 'revoked',
}

export function DeviceStatusBadge({ status, className }: DeviceStatusBadgeProps) {
  const { t } = useTranslation()

  // Normalize numeric status to string
  const normalizedStatus = normalizeDeviceStatus(status)
  const statusType = statusStyleMap[normalizedStatus]
  const label = t(`auth:devices.status.${i18nKeyMap[normalizedStatus]}`)

  return (
    <StatusBadge status={statusType} label={label} className={className} />
  )
}
