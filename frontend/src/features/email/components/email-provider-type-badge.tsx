import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { EmailProviderType } from '../types'
import { cn } from '@/lib/utils'

interface EmailProviderTypeBadgeProps {
  type: (typeof EmailProviderType)[keyof typeof EmailProviderType]
  className?: string
}

const typeStyleMap: Record<number, string> = {
  [EmailProviderType.Smtp]: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  [EmailProviderType.SendGrid]: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  [EmailProviderType.Mailgun]: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  [EmailProviderType.AmazonSes]: 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
  [EmailProviderType.Resend]: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
  [EmailProviderType.Postmark]: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
}

const typeKeyMap: Record<number, string> = {
  [EmailProviderType.Smtp]: 'smtp',
  [EmailProviderType.SendGrid]: 'sendGrid',
  [EmailProviderType.Mailgun]: 'mailgun',
  [EmailProviderType.AmazonSes]: 'amazonSes',
  [EmailProviderType.Resend]: 'resend',
  [EmailProviderType.Postmark]: 'postmark',
}

export function EmailProviderTypeBadge({ type, className }: EmailProviderTypeBadgeProps) {
  const { t } = useTranslation()

  const style = typeStyleMap[type] ?? typeStyleMap[EmailProviderType.Smtp]
  const label = t(`email:providers.types.${typeKeyMap[type] ?? 'smtp'}`)

  return (
    <Badge variant="outline" className={cn('font-medium', style, className)}>
      {label}
    </Badge>
  )
}
