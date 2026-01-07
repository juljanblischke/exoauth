import { useTranslation } from 'react-i18next'
import { StatusBadge } from '@/components/shared'
import { IpRestrictionType } from '../types'

interface IpRestrictionTypeBadgeProps {
  type: IpRestrictionType
  className?: string
}

const typeStyleMap: Record<IpRestrictionType, 'success' | 'error'> = {
  [IpRestrictionType.Whitelist]: 'success',
  [IpRestrictionType.Blacklist]: 'error',
}

const typeKeyMap: Record<IpRestrictionType, string> = {
  [IpRestrictionType.Whitelist]: 'whitelist',
  [IpRestrictionType.Blacklist]: 'blacklist',
}

export function IpRestrictionTypeBadge({ type, className }: IpRestrictionTypeBadgeProps) {
  const { t } = useTranslation()

  const statusType = typeStyleMap[type]
  const label = t(`ipRestrictions:type.${typeKeyMap[type]}`)

  return (
    <StatusBadge status={statusType} label={label} className={className} />
  )
}
