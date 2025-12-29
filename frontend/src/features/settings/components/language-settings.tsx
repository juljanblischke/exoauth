import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'

import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { useUpdatePreferences } from '@/features/auth/hooks'

const languages = [
  { code: 'en-US', label: 'English', flag: '🇬🇧' },
  { code: 'de-DE', label: 'Deutsch', flag: '🇩🇪' },
] as const

export function LanguageSettings() {
  const { t, i18n } = useTranslation()
  const updatePreferences = useUpdatePreferences()

  const handleLanguageChange = (languageCode: string) => {
    if (languageCode === i18n.language) return
    updatePreferences.mutate({ language: languageCode })
  }

  return (
    <div className="rounded-lg border bg-card p-6">
      <h3 className="text-lg font-medium">{t('settings:language.title')}</h3>
      <p className="text-sm text-muted-foreground mt-1">
        {t('settings:language.description')}
      </p>

      <div className="mt-6 space-y-3">
        <p className="text-sm font-medium">{t('settings:language.select')}</p>
        <div className="flex flex-wrap gap-3">
          {languages.map((lang) => {
            const isSelected = i18n.language === lang.code
            return (
              <Button
                key={lang.code}
                variant={isSelected ? 'default' : 'outline'}
                className={cn(
                  'relative min-w-[140px] justify-start gap-3',
                  isSelected && 'pr-10'
                )}
                onClick={() => handleLanguageChange(lang.code)}
                disabled={updatePreferences.isPending}
              >
                <span className="text-lg">{lang.flag}</span>
                <span>{lang.label}</span>
                {isSelected && (
                  <Check className="absolute right-3 h-4 w-4" />
                )}
              </Button>
            )
          })}
        </div>
      </div>

      <div className="mt-6 pt-4 border-t">
        <p className="text-sm text-muted-foreground">
          {t('settings:language.current')}:{' '}
          <span className="font-medium text-foreground">
            {languages.find((l) => l.code === i18n.language)?.label || i18n.language}
          </span>
        </p>
      </div>
    </div>
  )
}
