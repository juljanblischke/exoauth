import { useEffect, useRef, useCallback } from 'react'
import { useInView } from 'react-intersection-observer'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface DataTablePaginationProps {
  hasMore: boolean
  onLoadMore?: () => void
  isLoading?: boolean
  totalSelected?: number
  totalRows?: number
}

export function DataTablePagination({
  hasMore,
  onLoadMore,
  isLoading = false,
  totalSelected = 0,
  totalRows = 0,
}: DataTablePaginationProps) {
  const { t } = useTranslation('common')
  const { ref, inView } = useInView({
    threshold: 0,
    rootMargin: '100px',
  })

  // Store latest callback in ref to avoid stale closures
  const loadMoreRef = useRef(onLoadMore)
  useEffect(() => {
    loadMoreRef.current = onLoadMore
  }, [onLoadMore])

  // Stable callback that uses the ref
  const handleLoadMore = useCallback(() => {
    loadMoreRef.current?.()
  }, [])

  useEffect(() => {
    if (inView && hasMore && !isLoading) {
      handleLoadMore()
    }
  }, [inView, hasMore, isLoading, handleLoadMore])

  const showInfo = totalSelected > 0 || totalRows > 0

  if (!showInfo && !hasMore) {
    return null
  }

  return (
    <div className="border-t">
      <div className="flex items-center justify-center gap-4 px-4 py-3">
        {showInfo && (
          <div className="text-sm text-muted-foreground">
            {totalSelected > 0 ? (
              <span>{t('table.selected', { count: totalSelected })}</span>
            ) : (
              <span>{t('table.totalRows', { count: totalRows })}</span>
            )}
          </div>
        )}

        {hasMore && (
          <div ref={ref} className="flex items-center gap-2">
            {isLoading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                {t('actions.loading')}
              </div>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={onLoadMore}
                disabled={isLoading}
              >
                {t('actions.showMore')}
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
