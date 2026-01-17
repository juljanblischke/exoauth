import { useTranslation } from 'react-i18next'
import { Fingerprint, Zap, Shield, Brain, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface PasskeyEmptyStateProps {
  onAddPasskey: () => void
}

export function PasskeyEmptyState({ onAddPasskey }: PasskeyEmptyStateProps) {
  const { t } = useTranslation()

  const benefits = [
    {
      icon: Zap,
      text: t('auth:passkeys.empty.benefits.fast'),
    },
    {
      icon: Shield,
      text: t('auth:passkeys.empty.benefits.secure'),
    },
    {
      icon: Brain,
      text: t('auth:passkeys.empty.benefits.easy'),
    },
  ]

  return (
    <div className="flex flex-col items-center text-center py-8 px-4">
      <div className="p-4 rounded-full bg-muted mb-4">
        <Fingerprint className="h-10 w-10 text-muted-foreground" />
      </div>
      
      <h4 className="text-lg font-medium mb-2">
        {t('auth:passkeys.empty.title')}
      </h4>
      
      <p className="text-sm text-muted-foreground mb-6 max-w-md">
        {t('auth:passkeys.empty.description')}
      </p>
      
      <div className="flex flex-wrap justify-center gap-4 mb-6">
        {benefits.map((benefit, index) => (
          <div
            key={index}
            className="flex items-center gap-2 text-sm text-muted-foreground"
          >
            <benefit.icon className="h-4 w-4 text-primary" />
            <span>{benefit.text}</span>
          </div>
        ))}
      </div>

      <Button onClick={onAddPasskey}>
        <Plus className="h-4 w-4 mr-2" />
        {t('auth:passkeys.addButton')}
      </Button>
    </div>
  )
}
