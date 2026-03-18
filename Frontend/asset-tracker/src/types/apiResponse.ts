export type Result<T> = { ok: true; data: T } | { ok: false; message: string }

export interface ValidationErrorResponse {
  type: string
  title: string
  status: number
  errors: Record<string, string[]>
  traceId: string
}
