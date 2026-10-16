const dateTimeFormat = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
})

/** Shows a UTC timestamp from the API in the user's local time zone. */
export function formatDateTime(isoUtc: string): string {
  return dateTimeFormat.format(new Date(isoUtc))
}

/** "InProgress" -> "In progress" */
export function formatStatus(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/ (\w)/g, (_, c: string) => ' ' + c.toLowerCase())
}
