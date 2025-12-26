// API Response wrapper from backend
export interface ApiResponse<T> {
  status: 'success' | 'error'
  statusCode: number
  message: string
  data: T
  meta?: ApiResponseMeta
  errors?: ApiError[]
}

export interface ApiResponseMeta {
  timestamp: string
  requestId: string
  pagination?: PaginationMeta
}

export interface ApiError {
  field?: string
  code: string
  message: string
}

export interface PaginationMeta {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface CursorPaginationMeta {
  cursor: string | null
  nextCursor: string | null
  hasMore: boolean
  totalCount?: number
}

export interface PaginationParams {
  page?: number
  pageSize?: number
}

export interface CursorPaginationParams {
  cursor?: string
  limit?: number
}

export interface SortParams {
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface FilterParams {
  [key: string]: string | string[] | boolean | number | undefined
}

export interface QueryParams extends PaginationParams, SortParams {
  search?: string
  filters?: FilterParams
}

export interface CursorQueryParams extends CursorPaginationParams, SortParams {
  search?: string
  filters?: FilterParams
}

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

export interface RequestConfig {
  method: HttpMethod
  url: string
  data?: unknown
  params?: Record<string, unknown>
  headers?: Record<string, string>
}
