import type { ColumnDef, SortingState, VisibilityState } from '@tanstack/react-table'

export interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  isLoading?: boolean
  onLoadMore?: () => void
  hasMore?: boolean
  searchPlaceholder?: string
  onSearch?: (value: string) => void
  searchValue?: string
  emptyState?: React.ReactNode
  enableRowSelection?: boolean
  onRowSelectionChange?: (selectedRows: TData[]) => void
  bulkActions?: BulkAction<TData>[]
}

export interface BulkAction<TData> {
  label: string
  icon?: React.ReactNode
  variant?: 'default' | 'destructive' | 'outline'
  onClick: (selectedRows: TData[]) => void
}

export interface RowAction<TData> {
  label: string
  icon?: React.ReactNode
  onClick: (row: TData) => void
  variant?: 'default' | 'destructive'
  disabled?: boolean | ((row: TData) => boolean)
  hidden?: boolean | ((row: TData) => boolean)
  separator?: boolean
}

export interface TableFilter {
  id: string
  label: string
  type: 'select' | 'multi-select' | 'date-range' | 'boolean'
  options?: FilterOption[]
}

export interface FilterOption {
  label: string
  value: string
  icon?: React.ReactNode
}

export interface ActiveFilter {
  id: string
  value: string | string[] | boolean | DateRange
}

export interface DateRange {
  from: Date | undefined
  to: Date | undefined
}

export interface TablePreferences {
  sorting: SortingState
  columnVisibility: VisibilityState
  pageSize: number
}

export interface TableColumn<TData> {
  id: string
  header: string
  accessorKey?: keyof TData
  accessorFn?: (row: TData) => unknown
  cell?: (value: unknown, row: TData) => React.ReactNode
  enableSorting?: boolean
  enableHiding?: boolean
  size?: number
  minSize?: number
  maxSize?: number
}

export interface MobileCardProps<TData> {
  data: TData
  primaryField: keyof TData
  secondaryField?: keyof TData | ((row: TData) => string)
  avatar?: (row: TData) => { name: string; email?: string }
  tertiaryFields?: Array<{
    key: keyof TData
    label?: string
    render?: (value: unknown, row: TData) => React.ReactNode
  }>
  statusField?: keyof TData
  statusVariant?: (value: unknown) => 'default' | 'success' | 'warning' | 'destructive'
  actions?: RowAction<TData>[]
  onSelect?: () => void
  isSelected?: boolean
}
