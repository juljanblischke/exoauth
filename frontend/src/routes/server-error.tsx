import { useTranslation } from 'react-i18next'
import { ServerCrash, RefreshCw, Home } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function ServerErrorPage() {
  const { t } = useTranslation('errors')

  const handleRefresh = () => {
    window.location.reload()
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-background px-4">
      <div className="text-center">
        <div className="mb-8 flex justify-center">
          <div className="rounded-full bg-destructive/10 p-6">
            <ServerCrash className="h-16 w-16 text-destructive" />
          </div>
        </div>
        <h1 className="mb-2 text-6xl font-bold text-foreground">500</h1>
        <h2 className="mb-4 text-2xl font-semibold text-foreground">
          {t('serverError.title')}
        </h2>
        <p className="mb-8 max-w-md text-muted-foreground">
          {t('general.message')}
        </p>
        <div className="flex flex-col gap-4 sm:flex-row sm:justify-center">
          <Button onClick={handleRefresh}>
            <RefreshCw className="mr-2 h-4 w-4" />
            {t('general.retry')}
          </Button>
          <Button variant="outline" asChild>
            <a href="/system/dashboard">
              <Home className="mr-2 h-4 w-4" />
              {t('general.goHome')}
            </a>
          </Button>
        </div>
      </div>
    </div>
  )
}
