import type { ReactNode } from 'react'
import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, type ButtonProps } from '@mui/material'

interface ConfirmDialogProps {
    open: boolean
    title: string
    children: ReactNode
    confirmLabel: string
    confirmColor?: ButtonProps['color']
    /** True while the confirmed action runs: both buttons are disabled and the dialog stays open. */
    pending?: boolean
    onConfirm: () => void
    onCancel: () => void
}

/** In-page confirmation, used instead of the browser's window.confirm. */
export function ConfirmDialog({
    open,
    title,
    children,
    confirmLabel,
    confirmColor = 'primary',
    pending = false,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    return (
        <Dialog open={open} onClose={pending ? undefined : onCancel} maxWidth="xs" fullWidth>
            <DialogTitle>{title}</DialogTitle>
            <DialogContent>
                <DialogContentText component="div">{children}</DialogContentText>
            </DialogContent>
            <DialogActions sx={{ px: 3, pb: 2 }}>
                <Button onClick={onCancel} disabled={pending} autoFocus>
                    Cancel
                </Button>
                <Button onClick={onConfirm} variant="contained" color={confirmColor} disabled={pending}>
                    {pending ? 'Please wait…' : confirmLabel}
                </Button>
            </DialogActions>
        </Dialog>
    )
}
