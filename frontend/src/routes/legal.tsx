import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, FileText, Shield, ScrollText } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface LegalPageProps {
  type: 'imprint' | 'privacy' | 'terms'
}

const icons = {
  imprint: FileText,
  privacy: Shield,
  terms: ScrollText,
}

export function LegalPage({ type }: LegalPageProps) {
  const { t } = useTranslation()
  const Icon = icons[type]

  const pageKey = `${type}Page` as const
  const title = t(`common:legal.${pageKey}.title`)
  const subtitle = t(`common:legal.${pageKey}.subtitle`)
  const placeholder = t(`common:legal.${pageKey}.placeholder`)

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto max-w-3xl px-4 py-8">
        <div className="mb-8">
          <Button variant="ghost" size="sm" asChild>
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('common:legal.backToHome')}
            </Link>
          </Button>
        </div>

        <div className="rounded-lg border bg-card text-card-foreground shadow-sm">
          <div className="p-6 border-b">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <Icon className="h-5 w-5 text-primary" />
              </div>
              <div>
                <h1 className="text-2xl font-semibold leading-none tracking-tight">{title}</h1>
                <p className="text-sm text-muted-foreground mt-1">{subtitle}</p>
              </div>
            </div>
          </div>
          <div className="p-6">
            <div className="rounded-lg border border-dashed border-muted-foreground/25 bg-muted/50 p-6 text-center">
              <p className="text-muted-foreground">{placeholder}</p>
            </div>
          </div>
        </div>

        <div className="mt-8 text-center text-sm text-muted-foreground">
          <p>{t('common:legal.lastUpdated')}: {new Date().toLocaleDateString()}</p>
        </div>
      </div>
    </div>
  )
}

export function ImprintPage() {
  return <LegalPage type="imprint" />
}

export function PrivacyPage() {
  return <LegalPage type="privacy" />
}

export function TermsPage() {
  return <LegalPage type="terms" />
}
