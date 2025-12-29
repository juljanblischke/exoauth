import { useTranslation } from 'react-i18next'
import { Languages, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useUpdatePreferences } from '@/features/auth/hooks'

const languages = [
  { code: 'en-US', label: 'English', flag: '🇬🇧' },
  { code: 'de-DE', label: 'Deutsch', flag: '🇩🇪' },
]

export function LanguageSwitcher() {
  const { i18n, t } = useTranslation()
  const updatePreferences = useUpdatePreferences()

  const handleLanguageChange = (languageCode: string) => {
    if (languageCode === i18n.language) return
    updatePreferences.mutate({ language: languageCode })
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" disabled={updatePreferences.isPending}>
          {updatePreferences.isPending ? (
            <Loader2 className="h-5 w-5 animate-spin" />
          ) : (
            <Languages className="h-5 w-5" />
          )}
          <span className="sr-only">{t('navigation:language.change')}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {languages.map((lang) => (
          <DropdownMenuItem
            key={lang.code}
            onClick={() => handleLanguageChange(lang.code)}
            className={i18n.language === lang.code ? 'bg-accent' : ''}
            disabled={updatePreferences.isPending}
          >
            <span className="mr-2">{lang.flag}</span>
            {lang.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
