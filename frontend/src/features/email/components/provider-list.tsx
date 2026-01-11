import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Plus, Mail, AlertCircle, RefreshCw } from 'lucide-react'
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'

import { Button } from '@/components/ui/button'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { SortableProviderCard } from './provider-card'
import { ProviderFormDialog } from './provider-form-dialog'
import { ProviderDetailsSheet } from './provider-details-sheet'
import { TestEmailDialog } from './test-email-dialog'
import {
  useEmailProviders,
  useCreateEmailProvider,
  useUpdateEmailProvider,
  useDeleteEmailProvider,
  useTestEmailProvider,
  useResetCircuitBreaker,
  useReorderEmailProviders,
} from '../hooks'
import type { EmailProviderDto, CreateEmailProviderRequest, UpdateEmailProviderRequest } from '../types'
import { usePermissions } from '@/contexts/auth-context'

export function ProviderList() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const canManage = hasPermission('email:providers:manage')

  // State
  const [formOpen, setFormOpen] = useState(false)
  const [editingProvider, setEditingProvider] = useState<EmailProviderDto | null>(null)
  const [deletingProvider, setDeletingProvider] = useState<EmailProviderDto | null>(null)
  const [testingProvider, setTestingProvider] = useState<EmailProviderDto | null>(null)
  const [detailsProvider, setDetailsProvider] = useState<EmailProviderDto | null>(null)
  const [detailsSheetOpen, setDetailsSheetOpen] = useState(false)

  // Queries & mutations
  const { data: providers, isLoading, error, refetch, isRefetching } = useEmailProviders()
  const createProvider = useCreateEmailProvider()
  const updateProvider = useUpdateEmailProvider()
  const deleteProvider = useDeleteEmailProvider()
  const testProvider = useTestEmailProvider()
  const resetCircuitBreaker = useResetCircuitBreaker()
  const reorderProviders = useReorderEmailProviders()

  // DnD sensors
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

  // Handlers
  const handleRefresh = useCallback(async () => {
    await refetch()
    toast.success(t('email:providers.refreshed'))
  }, [refetch, t])

  const handleCreate = useCallback(() => {
    setEditingProvider(null)
    setFormOpen(true)
  }, [])

  const handleViewDetails = useCallback((provider: EmailProviderDto) => {
    setDetailsProvider(provider)
    setDetailsSheetOpen(true)
  }, [])

  const handleEdit = useCallback((provider: EmailProviderDto) => {
    setEditingProvider(provider)
    setFormOpen(true)
  }, [])

  const handleFormSubmit = useCallback(
    async (data: CreateEmailProviderRequest | UpdateEmailProviderRequest) => {
      if (editingProvider) {
        await updateProvider.mutateAsync({ id: editingProvider.id, request: data })
      } else {
        await createProvider.mutateAsync(data as CreateEmailProviderRequest)
      }
      setFormOpen(false)
      setEditingProvider(null)
    },
    [editingProvider, createProvider, updateProvider]
  )

  const handleDelete = useCallback((provider: EmailProviderDto) => {
    setDeletingProvider(provider)
  }, [])

  const handleConfirmDelete = useCallback(async () => {
    if (deletingProvider) {
      await deleteProvider.mutateAsync(deletingProvider.id)
      setDeletingProvider(null)
    }
  }, [deletingProvider, deleteProvider])

  const handleTest = useCallback((provider: EmailProviderDto) => {
    setTestingProvider(provider)
  }, [])

  const handleTestSubmit = useCallback(
    async (providerId: string, recipientEmail: string) => {
      await testProvider.mutateAsync({ providerId, recipientEmail })
      setTestingProvider(null)
    },
    [testProvider]
  )

  const handleResetCircuitBreaker = useCallback(
    async (provider: EmailProviderDto) => {
      await resetCircuitBreaker.mutateAsync(provider.id)
    },
    [resetCircuitBreaker]
  )

  const handleDragEnd = useCallback(
    async (event: DragEndEvent) => {
      const { active, over } = event

      if (!over || active.id === over.id || !providers) {
        return
      }

      const sortedProviders = [...providers].sort((a, b) => a.priority - b.priority)
      const oldIndex = sortedProviders.findIndex((p) => p.id === active.id)
      const newIndex = sortedProviders.findIndex((p) => p.id === over.id)

      if (oldIndex === -1 || newIndex === -1) {
        return
      }

      const newOrder = arrayMove(sortedProviders, oldIndex, newIndex)

      await reorderProviders.mutateAsync({
        providers: newOrder.map((p, i) => ({ providerId: p.id, priority: i })),
      })
    },
    [providers, reorderProviders]
  )

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-10 w-32" />
        </div>
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-24 w-full" />
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{t('common:error')}</AlertTitle>
        <AlertDescription>{t('email:providers.errors.loadFailed')}</AlertDescription>
      </Alert>
    )
  }

  const sortedProviders = providers?.slice().sort((a, b) => a.priority - b.priority) ?? []

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-lg font-semibold">{t('email:providers.title')}</h2>
          <p className="text-sm text-muted-foreground">
            {t('email:providers.description')}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={handleRefresh}
            disabled={isRefetching}
          >
            <RefreshCw className={`h-4 w-4 ${isRefetching ? 'animate-spin' : ''}`} />
            <span className="sr-only">{t('common:actions.refresh')}</span>
          </Button>
          {canManage && (
            <Button onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              {t('email:providers.actions.add')}
            </Button>
          )}
        </div>
      </div>

      {/* Empty state */}
      {sortedProviders.length === 0 && (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <Mail className="h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-lg font-semibold">{t('email:providers.empty.title')}</h3>
          <p className="text-sm text-muted-foreground mb-4">
            {t('email:providers.empty.description')}
          </p>
          {canManage && (
            <Button onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              {t('email:providers.actions.add')}
            </Button>
          )}
        </div>
      )}

      {/* Provider list with DnD */}
      {sortedProviders.length > 0 && (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <SortableContext
            items={sortedProviders.map((p) => p.id)}
            strategy={verticalListSortingStrategy}
          >
            <div className="space-y-3">
              {sortedProviders.map((provider) => (
                <SortableProviderCard
                  key={provider.id}
                  provider={provider}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onTest={handleTest}
                  onResetCircuitBreaker={handleResetCircuitBreaker}
                  onViewDetails={handleViewDetails}
                  canManage={canManage}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      {/* Create/Edit Dialog */}
      <ProviderFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        provider={editingProvider}
        onSubmit={handleFormSubmit}
        isLoading={createProvider.isPending || updateProvider.isPending}
      />

      {/* Test Email Dialog */}
      <TestEmailDialog
        open={!!testingProvider}
        onOpenChange={(open) => !open && setTestingProvider(null)}
        provider={testingProvider}
        onSubmit={handleTestSubmit}
        isLoading={testProvider.isPending}
      />

      {/* Provider Details Sheet */}
      <ProviderDetailsSheet
        provider={detailsProvider}
        open={detailsSheetOpen}
        onOpenChange={(open) => {
          setDetailsSheetOpen(open)
          if (!open) setDetailsProvider(null)
        }}
      />

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deletingProvider}
        onOpenChange={(open) => !open && setDeletingProvider(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('email:providers.delete.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('email:providers.delete.description', {
                name: deletingProvider?.name ?? '',
              })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common:actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('common:actions.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
