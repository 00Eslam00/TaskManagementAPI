import { Link as RouterLink, Outlet, useLocation } from 'react-router'
import { AppBar, Box, Button, Chip, Container, IconButton, Tab, Tabs, Toolbar, Tooltip, Typography } from '@mui/material'
import LogoutIcon from '@mui/icons-material/Logout'
import { useAuth } from '../auth/useAuth'

export function AppLayout() {
    const { user, logout } = useAuth()
    const { pathname } = useLocation()
    const isAdmin = user?.role === 'Admin'

    return (
        <Box sx={{ minHeight: '100vh' }}>
            <AppBar position="sticky" elevation={0}>
                <Toolbar sx={{ gap: 1 }}>
                    <Typography
                        variant="h6"
                        component={RouterLink}
                        to="/tasks"
                        noWrap
                        sx={{ color: 'inherit', textDecoration: 'none', flexGrow: 1, fontSize: { xs: '1rem', sm: '1.25rem' } }}
                    >
                        Task Management
                    </Typography>

                    {user && (
                        <>
                            <Typography variant="body2" noWrap sx={{ maxWidth: { xs: 100, sm: 240 } }} title={user.email}>
                                {user.name}
                            </Typography>
                            <Chip
                                label={user.role}
                                size="small"
                                color={user.role === 'Admin' ? 'warning' : 'default'}
                                sx={user.role === 'Admin' ? undefined : { bgcolor: 'grey.100' }}
                            />
                        </>
                    )}

                    {/* Text button on wider screens, icon only on phones. */}
                    <Button
                        color="inherit"
                        startIcon={<LogoutIcon />}
                        onClick={() => void logout()}
                        sx={{ display: { xs: 'none', sm: 'inline-flex' } }}
                    >
                        Log out
                    </Button>
                    <Tooltip title="Log out">
                        <IconButton
                            color="inherit"
                            aria-label="Log out"
                            onClick={() => void logout()}
                            sx={{ display: { xs: 'inline-flex', sm: 'none' } }}
                        >
                            <LogoutIcon />
                        </IconButton>
                    </Tooltip>
                </Toolbar>

                {/* Only admins have more than one area, so normal users never see the Users item. */}
                {isAdmin && (
                    <Tabs
                        component="nav"
                        aria-label="Main navigation"
                        value={pathname.startsWith('/admin') ? '/admin/users' : '/tasks'}
                        textColor="inherit"
                        indicatorColor="secondary"
                        sx={{ px: { xs: 0, sm: 2 }, minHeight: 40, '& .MuiTab-root': { minHeight: 40 } }}
                    >
                        <Tab label="My tasks" value="/tasks" component={RouterLink} to="/tasks" />
                        <Tab label="Users" value="/admin/users" component={RouterLink} to="/admin/users" />
                    </Tabs>
                )}
            </AppBar>

            <Container component="main" maxWidth="md" sx={{ py: { xs: 2, sm: 4 } }}>
                <Outlet />
            </Container>
        </Box>
    )
}
