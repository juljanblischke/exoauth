import { useState, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Check, Loader2 } from 'lucide-react'
import { useInView } from 'react-intersection-observer'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { UserAvatar } from '@/components/shared/user-avatar'
import { useDebounce } from '@/hooks/use-debounce'
import { useSystemUsers } from '@/features/users/hooks'

interface UserSelectModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  selectedUserIds: string[]
  onConfirm: (userIds: string[]) => void
}

export function UserSelectModal({
  open,
  onOpenChange,
  selectedUserIds,
  onConfirm,
}: UserSelectModalProps) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const [localSelectedIds, setLocalSelectedIds] = useState<Set<string>>(
    new Set(selectedUserIds)
  )

  const debouncedSearch = useDebounce(search, 300)

  const {
    data,
    isLoading,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useSystemUsers({
    search: debouncedSearch || undefined,
    limit: 20,
  })

  const users = useMemo(
    () => data?.pages.flatMap((page) => page.users) ?? [],
    [data]
  )

  const { ref: loadMoreRef } = useInView({
    onChange: (inView) => {
      if (inView && hasNextPage && !isFetchingNextPage) {
        fetchNextPage()
      }
    },
  })

  // Reset local selection when modal opens
  const handleOpenChange = useCallback(
    (newOpen: boolean) => {
      if (newOpen) {
        setLocalSelectedIds(new Set(selectedUserIds))
        setSearch('')
      }
      onOpenChange(newOpen)
    },
    [selectedUserIds, onOpenChange]
  )

  const toggleUser = useCallback((userId: string) => {
    setLocalSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(userId)) {
        next.delete(userId)
      } else {
        next.add(userId)
      }
      return next
    })
  }, [])

  const handleSelectAll = useCallback(() => {
    setLocalSelectedIds((prev) => {
      const next = new Set(prev)
      users.forEach((user) => next.add(user.id))
      return next
    })
  }, [users])

  const handleDeselectAll = useCallback(() => {
    setLocalSelectedIds(new Set())
  }, [])

  const handleConfirm = useCallback(() => {
    onConfirm(Array.from(localSelectedIds))
    onOpenChange(false)
  }, [localSelectedIds, onConfirm, onOpenChange])

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('email:userSelect.title')}</DialogTitle>
          <DialogDescription>
            {t('email:userSelect.selected', { count: localSelectedIds.size })}
          </DialogDescription>
        </DialogHeader>

        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder={t('email:userSelect.search')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>

        {/* Select/Deselect All */}
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleSelectAll}
            disabled={users.length === 0}
          >
            {t('email:userSelect.selectAll')}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleDeselectAll}
            disabled={localSelectedIds.size === 0}
          >
            {t('email:userSelect.deselectAll')}
          </Button>
        </div>

        {/* User List */}
        <ScrollArea className="h-[300px] border rounded-md">
          {isLoading && users.length === 0 ? (
            <div className="p-4 space-y-3">
              {[1, 2, 3, 4, 5].map((i) => (
                <div key={i} className="flex items-center gap-3">
                  <Skeleton className="h-4 w-4" />
                  <Skeleton className="h-8 w-8 rounded-full" />
                  <div className="space-y-1 flex-1">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-48" />
                  </div>
                </div>
              ))}
            </div>
          ) : users.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              {t('email:userSelect.noResults')}
            </div>
          ) : (
            <div className="p-2">
              {users.map((user) => (
                <div
                  key={user.id}
                  className="flex items-center gap-3 p-2 rounded-md hover:bg-muted cursor-pointer"
                  onClick={() => toggleUser(user.id)}
                >
                  <Checkbox
                    checked={localSelectedIds.has(user.id)}
                    onCheckedChange={() => toggleUser(user.id)}
                  />
                  <UserAvatar
                    name={user.fullName ?? user.email}
                    size="sm"
                  />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">
                      {user.fullName ?? user.email}
                    </p>
                    {user.fullName && (
                      <p className="text-xs text-muted-foreground truncate">
                        {user.email}
                      </p>
                    )}
                  </div>
                  {localSelectedIds.has(user.id) && (
                    <Check className="h-4 w-4 text-primary" />
                  )}
                </div>
              ))}

              {/* Load more trigger */}
              {(hasNextPage || isFetchingNextPage) && (
                <div ref={loadMoreRef} className="py-4 flex justify-center">
                  {isFetchingNextPage ? (
                    <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                  ) : (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => fetchNextPage()}
                    >
                      {t('email:userSelect.loadMore')}
                    </Button>
                  )}
                </div>
              )}
            </div>
          )}
        </ScrollArea>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
          >
            {t('common:actions.cancel')}
          </Button>
          <Button type="button" onClick={handleConfirm}>
            {t('common:actions.confirm')} ({localSelectedIds.size})
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
