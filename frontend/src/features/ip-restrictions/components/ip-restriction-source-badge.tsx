import { useTranslation } from 'react-i18next'
import { StatusBadge } from '@/components/shared'
import { IpRestrictionSource } from '../types'

interface IpRestrictionSourceBadgeProps {
  source: IpRestrictionSource
  className?: string
}

const sourceStyleMap: Record<IpRestrictionSource, 'info' | 'neutral'> = {
  [IpRestrictionSource.Manual]: 'info',
  [IpRestrictionSource.Auto]: 'neutral',
}

const sourceKeyMap: Record<IpRestrictionSource, string> = {
  [IpRestrictionSource.Manual]: 'manual',
  [IpRestrictionSource.Auto]: 'auto',
}

export function IpRestrictionSourceBadge({ source, className }: IpRestrictionSourceBadgeProps) {
  const { t } = useTranslation()

  const statusType = sourceStyleMap[source]
  const label = t(`ipRestrictions:source.${sourceKeyMap[source]}`)

  return (
    <StatusBadge status={statusType} label={label} showDot={false} className={className} />
  )
}
