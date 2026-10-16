import { isAxiosError } from 'axios'
import type { ProblemDetails } from '../api/types'

/** An API failure turned into something the UI can show directly. */
export interface ApiError {
  /** HTTP status, or undefined when the server could not be reached. */
  status?: number
  /** A sentence that can be shown to the user as is. */
  message: string
  /** Validation messages per form field, with camelCase keys ("title", "email"). */
  fieldErrors: Record<string, string>
}

const fallbackMessages: Record<number, string> = {
  400: 'The request was not valid. Please check the form and try again.',
  401: 'Your session has expired. Please log in again.',
  403: 'You do not have permission to do this.',
  404: 'The item was not found.',
  409: 'This conflicts with existing data.',
}

export function toApiError(error: unknown): ApiError {
  if (!isAxiosError<ProblemDetails>(error)) {
    return { message: 'Something went wrong. Please try again.', fieldErrors: {} }
  }

  if (!error.response) {
    return {
      message: 'Cannot reach the server. Check your connection and try again.',
      fieldErrors: {},
    }
  }

  const { status, data } = error.response
  const fieldErrors = toFieldErrors(data?.errors)

  // The API puts the readable reason in "detail".
  // Validation failures have no detail, only per-field messages.
  const message =
    data?.detail ??
    (Object.keys(fieldErrors).length > 0
      ? 'Please fix the highlighted fields.'
      : (fallbackMessages[status] ?? 'Something went wrong. Please try again later.'))

  return { status, message, fieldErrors }
}

// ASP.NET Core names validation keys after the C# properties ("Title", "Password").
// The forms use camelCase, so the first letter is lowered. "$.priority" style keys
// from JSON parsing errors are reduced to the property name as well.
function toFieldErrors(errors: ProblemDetails['errors']): Record<string, string> {
  const result: Record<string, string> = {}
  if (!errors) return result

  for (const [key, messages] of Object.entries(errors)) {
    const name = key.replace(/^\$\./, '')
    if (!name || messages.length === 0) continue
    result[name.charAt(0).toLowerCase() + name.slice(1)] = messages[0]
  }

  return result
}
