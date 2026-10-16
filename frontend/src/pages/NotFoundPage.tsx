import { Link as RouterLink, useLocation } from 'react-router'
import { Button, Paper, Typography } from '@mui/material'

export default function NotFoundPage() {
    const { pathname } = useLocation()

    return (
        <Paper variant="outlined" sx={{ p: { xs: 3, sm: 5 }, textAlign: 'center' }}>
            <Typography variant="h2" component="p" color="text.secondary" sx={{ fontWeight: 300 }}>
                404
            </Typography>
            <Typography variant="h5" component="h1" gutterBottom>
                Page not found
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3, overflowWrap: 'anywhere' }}>
                There is no page at <code>{pathname}</code>.
            </Typography>
            <Button component={RouterLink} to="/tasks" variant="contained">
                Go to my tasks
            </Button>
        </Paper>
    )
}
