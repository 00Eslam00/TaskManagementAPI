import type { ReactNode } from 'react'
import { Alert, Box, Button, CircularProgress, Paper, Typography } from '@mui/material'

export function FullPageLoader() {
    return (
        <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}>
            <CircularProgress aria-label="Loading" />
        </Box>
    )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
    return (
        <Alert
            severity="error"
            action={
                onRetry && (
                    <Button color="inherit" size="small" onClick={onRetry}>
                        Retry
                    </Button>
                )
            }
        >
            {message}
        </Alert>
    )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
    return (
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 5 }, textAlign: 'center' }}>
            <Typography variant="h6" component="p" gutterBottom>
                {title}
            </Typography>
            {children}
        </Paper>
    )
}
