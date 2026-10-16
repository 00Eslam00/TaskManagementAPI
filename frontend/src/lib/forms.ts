import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'

/**
 * Shows the API's validation messages under the matching form fields.
 * Messages for fields the form does not have are left to the general error alert.
 */
export function applyFieldErrors<T extends FieldValues>(
    setError: UseFormSetError<T>,
    fieldErrors: Record<string, string>,
    fields: readonly Path<T>[],
): void {
    for (const field of fields) {
        const message = fieldErrors[field]
        if (message) setError(field, { type: 'server', message })
    }
}
