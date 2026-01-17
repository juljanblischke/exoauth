import { useState, useEffect, useRef } from 'react'
import {
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type SortingState,
  type VisibilityState,
  type RowSelectionState,
} from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Checkbox } from '@/components/ui/checkbox'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useMediaQuery } from '@/hooks/use-media-query'
import { DataTableToolbar } from './data-table-toolbar'
import { DataTablePagination } from './data-table-pagination'
import { DataTableBulkActions } from './data-table-bulk-actions'
import { DataTableCard } from './data-table-card'
import { EmptyState } from '../feedback/empty-state'
import type { BulkAction, TableFilter, ActiveFilter, RowAction } from '@/types/table'

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  isLoading?: boolean
  isFetching?: boolean
  onLoadMore?: () => void
  hasMore?: boolean
  searchPlaceholder?: string
  onSearch?: (value: string) => void
  searchValue?: string
  filters?: TableFilter[]
  activeFilters?: ActiveFilter[]
  onFilterChange?: (filters: ActiveFilter[]) => void
  toolbarContent?: React.ReactNode
  toolbarActions?: React.ReactNode
  onRefresh?: () => void
  isRefreshing?: boolean
  emptyState?: {
    title: string
    description?: string
    action?: { label: string; onClick: () => void }
  }
  enableRowSelection?: boolean
  onRowSelectionChange?: (selectedRows: TData[]) => void
  bulkActions?: BulkAction<TData>[]
  rowActions?: RowAction<TData>[]
  mobileCard?: {
    primaryField: keyof TData
    secondaryField?: keyof TData | ((row: TData) => string)
    tertiaryFields?: Array<{
      key: keyof TData
      label?: string
      render?: (value: unknown, row: TData) => React.ReactNode
    }>
    avatar?: (row: TData) => { name?: string; email?: string; imageUrl?: string }
    /** Custom icon element to display instead of avatar (e.g., for non-user entities) */
    icon?: (row: TData) => React.ReactNode
  }
  tableId?: string
  initialSorting?: SortingState
  initialColumnVisibility?: VisibilityState
  onSortingChange?: (sorting: SortingState) => void
  onColumnVisibilityChange?: (visibility: VisibilityState) => void
  onRowClick?: (row: TData) => void
}

export function DataTable<TData, TValue>({
  columns,
  data,
  isLoading = false,
  isFetching = false,
  onLoadMore,
  hasMore = false,
  searchPlaceholder,
  onSearch,
  searchValue = '',
  filters,
  activeFilters = [],
  onFilterChange,
  toolbarContent,
  toolbarActions,
  onRefresh,
  isRefreshing = false,
  emptyState,
  enableRowSelection = false,
  onRowSelectionChange,
  bulkActions = [],
  rowActions = [],
  mobileCard,
  // tableId reserved for future table preferences feature
  tableId: _tableId,
  initialSorting = [],
  initialColumnVisibility = {},
  onSortingChange,
  onColumnVisibilityChange,
  onRowClick,
}: DataTableProps<TData, TValue>) {
  const { t } = useTranslation('common')
  const isMobile = useMediaQuery('(max-width: 768px)')

  // Silence unused variable warning - tableId reserved for future table preferences
  void _tableId
  const [sorting, setSorting] = useState<SortingState>(initialSorting)
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>(initialColumnVisibility)
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})

  const selectionColumn: ColumnDef<TData, TValue> = {
    id: 'select',
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected() ||
          (table.getIsSomePageRowsSelected() && 'indeterminate')
        }
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label={t('table.selectAll')}
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label={t('table.selectRow')}
      />
    ),
    enableSorting: false,
    enableHiding: false,
    size: 40,
  }

  const tableColumns = enableRowSelection
    ? [selectionColumn, ...columns]
    : columns

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data,
    columns: tableColumns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    enableMultiSort: true,
    enableSortingRemoval: true,
    onSortingChange: (updater) => {
      const newSorting = typeof updater === 'function' ? updater(sorting) : updater
      setSorting(newSorting)
      onSortingChange?.(newSorting)
    },
    onColumnVisibilityChange: (updater) => {
      const newVisibility = typeof updater === 'function' ? updater(columnVisibility) : updater
      setColumnVisibility(newVisibility)
      onColumnVisibilityChange?.(newVisibility)
    },
    onRowSelectionChange: setRowSelection,
    state: {
      sorting,
      columnVisibility,
      rowSelection,
    },
    enableRowSelection,
  })

  const selectedRows = table.getFilteredSelectedRowModel().rows.map((row) => row.original)

  // Use ref to store callback to avoid stale closures and infinite loops
  const onRowSelectionChangeRef = useRef(onRowSelectionChange)
  useEffect(() => {
    onRowSelectionChangeRef.current = onRowSelectionChange
  }, [onRowSelectionChange])

  useEffect(() => {
    const rows = table.getFilteredSelectedRowModel().rows.map((row) => row.original)
    onRowSelectionChangeRef.current?.(rows)
  }, [rowSelection, table])

  const clearSelection = () => {
    setRowSelection({})
  }

  if (isMobile && mobileCard) {
    return (
      <div className="space-y-4">
        <DataTableToolbar
          searchValue={searchValue}
          onSearch={onSearch}
          searchPlaceholder={searchPlaceholder}
          filters={filters}
          activeFilters={activeFilters}
          onFilterChange={onFilterChange}
          table={table}
          content={toolbarContent}
          actions={toolbarActions}
          onRefresh={onRefresh}
          isRefreshing={isRefreshing}
        />

        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-24 w-full rounded-lg" />
            ))}
          </div>
        ) : data.length === 0 ? (
          emptyState ? (
            <EmptyState
              title={emptyState.title}
              description={emptyState.description}
              action={emptyState.action}
            />
          ) : (
            <EmptyState title={t('table.noData')} />
          )
        ) : (
          <div className="space-y-3">
            {data.map((row, index) => (
              <DataTableCard
                key={index}
                data={row}
                primaryField={mobileCard.primaryField}
                secondaryField={mobileCard.secondaryField}
                tertiaryFields={mobileCard.tertiaryFields}
                avatar={mobileCard.avatar?.(row)}
                icon={mobileCard.icon?.(row)}
                actions={rowActions}
                isSelected={rowSelection[index] ?? false}
                onSelect={
                  enableRowSelection
                    ? () => {
                        setRowSelection((prev) => ({
                          ...prev,
                          [index]: !prev[index],
                        }))
                      }
                    : undefined
                }
                onClick={onRowClick ? () => onRowClick(row) : undefined}
              />
            ))}
          </div>
        )}

        <DataTablePagination
          hasMore={hasMore}
          onLoadMore={onLoadMore}
          isLoading={isFetching}
        />

        {selectedRows.length > 0 && bulkActions.length > 0 && (
          <DataTableBulkActions
            selectedCount={selectedRows.length}
            actions={bulkActions}
            selectedRows={selectedRows}
            onClearSelection={clearSelection}
          />
        )}
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <DataTableToolbar
        searchValue={searchValue}
        onSearch={onSearch}
        searchPlaceholder={searchPlaceholder}
        filters={filters}
        activeFilters={activeFilters}
        onFilterChange={onFilterChange}
        table={table}
        content={toolbarContent}
        actions={toolbarActions}
        onRefresh={onRefresh}
        isRefreshing={isRefreshing}
      />

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead
                    key={header.id}
                    style={{ width: header.getSize() }}
                  >
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 10 }).map((_, i) => (
                <TableRow key={i}>
                  {tableColumns.map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={tableColumns.length}
                  className="h-24 text-center"
                >
                  {emptyState ? (
                    <EmptyState
                      title={emptyState.title}
                      description={emptyState.description}
                      action={emptyState.action}
                      className="py-8"
                    />
                  ) : (
                    <span className="text-muted-foreground">
                      {t('table.noData')}
                    </span>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && 'selected'}
                  className={cn(
                    row.getIsSelected() && 'bg-muted/50',
                    onRowClick && 'cursor-pointer hover:bg-muted/50'
                  )}
                  onClick={() => onRowClick?.(row.original)}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <DataTablePagination
        hasMore={hasMore}
        onLoadMore={onLoadMore}
        isLoading={isFetching}
        totalSelected={selectedRows.length}
        totalRows={data.length}
      />

      {selectedRows.length > 0 && bulkActions.length > 0 && (
        <DataTableBulkActions
          selectedCount={selectedRows.length}
          actions={bulkActions}
          selectedRows={selectedRows}
          onClearSelection={clearSelection}
        />
      )}
    </div>
  )
}
