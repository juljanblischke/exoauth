import { useTranslation } from 'react-i18next'
import { Separator } from '@/components/ui/separator'

export function Footer() {
  const { t } = useTranslation()
  const currentYear = new Date().getFullYear()

  return (
    <footer className="border-t bg-background px-6 py-4">
      <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
        {/* Copyright */}
        <p className="text-sm text-muted-foreground">
          &copy; {currentYear} {t('common:app.name')}. {t('common:app.copyright')}
        </p>

        {/* Legal Links */}
        <nav className="flex items-center gap-4 text-sm">
          <a
            href="/imprint"
            className="text-muted-foreground hover:text-foreground transition-colors"
          >
            {t('common:legal.imprint')}
          </a>
          <Separator orientation="vertical" className="h-4" />
          <a
            href="/privacy"
            className="text-muted-foreground hover:text-foreground transition-colors"
          >
            {t('common:legal.privacy')}
          </a>
          <Separator orientation="vertical" className="h-4" />
          <a
            href="/terms"
            className="text-muted-foreground hover:text-foreground transition-colors"
          >
            {t('common:legal.terms')}
          </a>
        </nav>
      </div>
    </footer>
  )
}
