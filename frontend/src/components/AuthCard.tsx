import type { ReactNode } from 'react'
import { Box, Paper, Typography } from '@mui/material'

/** Centered card shared by the Login and Register pages. */
export function AuthCard({ title, children }: { title: string; children: ReactNode }) {
    return (
        <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', px: 2, py: 4 }}>
            <Paper sx={{ p: { xs: 3, sm: 4 }, width: '100%', maxWidth: 420 }}>
                <Typography variant="overline" color="primary" component="p">
                    Task Management
                </Typography>
                <Typography variant="h5" component="h1" sx={{ mb: 3 }}>
                    {title}
                </Typography>
                {children}
            </Paper>
        </Box>
    )
}
