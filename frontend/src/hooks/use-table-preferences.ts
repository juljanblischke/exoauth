import { useCallback } from 'react'
import type { SortingState, VisibilityState } from '@tanstack/react-table'
import { useLocalStorage } from './use-local-storage'
import type { TablePreferences } from '@/types'

const STORAGE_PREFIX = 'exoauth-table-'

const defaultPreferences: TablePreferences = {
  sorting: [],
  columnVisibility: {},
  pageSize: 20,
}

export function useTablePreferences(tableId: string) {
  const [preferences, setPreferences, resetPreferences] =
    useLocalStorage<TablePreferences>(
      `${STORAGE_PREFIX}${tableId}`,
      defaultPreferences
    )

  const setSorting = useCallback(
    (sorting: SortingState) => {
      setPreferences((prev) => ({ ...prev, sorting }))
    },
    [setPreferences]
  )

  const setColumnVisibility = useCallback(
    (columnVisibility: VisibilityState) => {
      setPreferences((prev) => ({ ...prev, columnVisibility }))
    },
    [setPreferences]
  )

  const setPageSize = useCallback(
    (pageSize: number) => {
      setPreferences((prev) => ({ ...prev, pageSize }))
    },
    [setPreferences]
  )

  const reset = useCallback(() => {
    resetPreferences()
  }, [resetPreferences])

  return {
    preferences,
    sorting: preferences.sorting,
    columnVisibility: preferences.columnVisibility,
    pageSize: preferences.pageSize,
    setSorting,
    setColumnVisibility,
    setPageSize,
    reset,
  }
}
