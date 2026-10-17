// Characters that take no space on screen: zero-width space, word joiner, byte order mark.
const INVISIBLE_CHARACTERS = /[\u200B\u2060\uFEFF]/g

// Any run of whitespace, including tabs, line breaks and non-breaking spaces.
const WHITESPACE_RUNS = /\s+/g

/**
 * The same rule as TaskItem.NormalizeTitle in the API: invisible characters removed, every run
 * of whitespace turned into one space, ends trimmed. The form sends exactly what will be stored,
 * so its length check and the API's duplicate check see the same title.
 */
export function normalizeTitle(value: string): string {
  return value.replace(INVISIBLE_CHARACTERS, '').replace(WHITESPACE_RUNS, ' ').trim()
}
