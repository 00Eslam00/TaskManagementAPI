import type { ReactElement } from 'react'
import { Chip, CircularProgress, type ChipProps } from '@mui/material'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward'
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import RemoveIcon from '@mui/icons-material/Remove'
import ScheduleIcon from '@mui/icons-material/Schedule'
import SyncIcon from '@mui/icons-material/Sync'
import TaskAltIcon from '@mui/icons-material/TaskAlt'
import type { TaskPriority, TaskResponse, TaskStatus } from '../api/types'
import { formatStatus } from '../lib/format'
import { isProcessing } from '../lib/processing'

// Each badge has an icon as well as a color, so it can be told apart without relying on color alone.

const statusStyles: Record<TaskStatus, { color: ChipProps['color']; icon: ReactElement }> = {
    Pending: { color: 'default', icon: <ScheduleIcon /> },
    InProgress: { color: 'info', icon: <SyncIcon /> },
    Done: { color: 'success', icon: <CheckCircleIcon /> },
}

const priorityStyles: Record<TaskPriority, { color: ChipProps['color']; icon: ReactElement }> = {
    Low: { color: 'default', icon: <ArrowDownwardIcon /> },
    Medium: { color: 'warning', icon: <RemoveIcon /> },
    High: { color: 'error', icon: <ArrowUpwardIcon /> },
}

export function StatusBadge({ status }: { status: TaskStatus }) {
    const { color, icon } = statusStyles[status]
    return <Chip size="small" variant="filled" color={color} icon={icon} label={formatStatus(status)} />
}

export function PriorityBadge({ priority }: { priority: TaskPriority }) {
    const { color, icon } = priorityStyles[priority]
    return <Chip size="small" variant="outlined" color={color} icon={icon} label={`${priority} priority`} />
}

/** Shows whether the API's background worker has processed the task yet. */
export function ProcessingBadge({ task }: { task: TaskResponse }) {
    return isProcessing(task) ? (
        <Chip
            size="small"
            variant="outlined"
            color="info"
            icon={<CircularProgress size={14} color="inherit" aria-hidden />}
            label="Processing…"
        />
    ) : (
        <Chip size="small" variant="outlined" color="success" icon={<TaskAltIcon />} label="Processed" />
    )
}
