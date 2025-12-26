import { useEffect, useRef } from 'react'
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

  const loadMoreRef = useRef(onLoadMore)
  loadMoreRef.current = onLoadMore

  useEffect(() => {
    if (inView && hasMore && !isLoading && loadMoreRef.current) {
      loadMoreRef.current()
    }
  }, [inView, hasMore, isLoading])

  return (
    <div className="flex items-center justify-between px-2 py-4">
      <div className="flex-1 text-sm text-muted-foreground">
        {totalSelected > 0 ? (
          <span>
            {t('table.selected', { count: totalSelected })}
          </span>
        ) : totalRows > 0 ? (
          <span>
            {totalRows} {totalRows === 1 ? 'row' : 'rows'}
          </span>
        ) : null}
      </div>

      <div className="flex items-center gap-2">
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
