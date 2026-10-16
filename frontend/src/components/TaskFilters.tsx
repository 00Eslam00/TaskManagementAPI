import { Chip, IconButton, InputAdornment, Stack, TextField } from '@mui/material'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import { formatStatus } from '../lib/format'
import { STATUS_FILTERS, type StatusFilter } from '../lib/taskFilters'

interface TaskFiltersProps {
    status: StatusFilter
    search: string
    /** Number of tasks per status, shown on each chip. */
    counts: Record<StatusFilter, number>
    onStatusChange: (status: StatusFilter) => void
    onSearchChange: (search: string) => void
}

export function TaskFilters({ status, search, counts, onStatusChange, onSearchChange }: TaskFiltersProps) {
    return (
        <Stack spacing={2}>
            <TextField
                label="Search by title"
                type="search"
                size="small"
                value={search}
                onChange={(event) => onSearchChange(event.target.value)}
                slotProps={{
                    input: {
                        startAdornment: (
                            <InputAdornment position="start">
                                <SearchIcon />
                            </InputAdornment>
                        ),
                        endAdornment: search && (
                            <InputAdornment position="end">
                                <IconButton aria-label="Clear search" size="small" edge="end" onClick={() => onSearchChange('')}>
                                    <ClearIcon fontSize="small" />
                                </IconButton>
                            </InputAdornment>
                        ),
                    },
                }}
            />

            {/* Chips wrap onto a second line on narrow screens instead of overflowing. */}
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }} role="group" aria-label="Filter by status">
                {STATUS_FILTERS.map((filter) => {
                    const selected = filter === status
                    return (
                        <Chip
                            key={filter}
                            label={`${filter === 'All' ? 'All' : formatStatus(filter)} (${counts[filter]})`}
                            color={selected ? 'primary' : 'default'}
                            variant={selected ? 'filled' : 'outlined'}
                            onClick={() => onStatusChange(filter)}
                            aria-pressed={selected}
                        />
                    )
                })}
            </Stack>
        </Stack>
    )
}
