import { Link as RouterLink } from 'react-router'
import { Button, Paper, Typography } from '@mui/material'
import LockIcon from '@mui/icons-material/Lock'

/** Shown when a logged-in user opens a page their role does not allow (the API's 403). */
export default function AccessDeniedPage() {
    return (
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 5 }, textAlign: 'center' }}>
            <LockIcon color="error" sx={{ fontSize: 48, mb: 1 }} />
            <Typography variant="h5" component="h1" gutterBottom>
                Access denied
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3 }}>
                This page is only available to administrators.
            </Typography>
            <Button component={RouterLink} to="/tasks" variant="contained">
                Go to my tasks
            </Button>
        </Paper>
    )
}
