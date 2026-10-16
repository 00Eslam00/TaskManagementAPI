import { Container, Paper, Typography } from '@mui/material'
import { config } from '../config'

// Temporary page now
export default function SetupPage() {
  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Typography variant="h5" component="h1" gutterBottom>
          Task Management
        </Typography>
        <Typography color="text.secondary">
          Frontend is running. API address: <code>{config.apiUrl}</code>
        </Typography>
      </Paper>
    </Container>
  )
}
