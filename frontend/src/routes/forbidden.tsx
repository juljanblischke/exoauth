import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ShieldX, Home, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function ForbiddenPage() {
  const { t } = useTranslation('errors')

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-background px-4">
      <div className="text-center">
        <div className="mb-8 flex justify-center">
          <div className="rounded-full bg-destructive/10 p-6">
            <ShieldX className="h-16 w-16 text-destructive" />
          </div>
        </div>
        <h1 className="mb-2 text-6xl font-bold text-foreground">403</h1>
        <h2 className="mb-4 text-2xl font-semibold text-foreground">
          {t('forbidden.title')}
        </h2>
        <p className="mb-8 max-w-md text-muted-foreground">
          {t('forbidden.message')}
        </p>
        <div className="flex flex-col gap-4 sm:flex-row sm:justify-center">
          <Button asChild>
            <Link to="/dashboard">
              <Home className="mr-2 h-4 w-4" />
              {t('general.goHome')}
            </Link>
          </Button>
          <Button variant="outline" onClick={() => window.history.back()}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Go Back
          </Button>
        </div>
      </div>
    </div>
  )
}
