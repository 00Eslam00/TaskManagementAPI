import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
    Alert,
    Box,
    Button,
    Chip,
    IconButton,
    Paper,
    Skeleton,
    Snackbar,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Tooltip,
    Typography,
    useMediaQuery,
    useTheme,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import PersonAddIcon from '@mui/icons-material/PersonAdd'
import { queryKeys } from '../api/queryKeys'
import type { UserResponse } from '../api/types'
import { deleteUser, getUsers } from '../api/users'
import { useAuth } from '../auth/useAuth'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { CreateUserDialog } from '../components/CreateUserDialog'
import { EmptyState, ErrorState } from '../components/PageStates'
import { toApiError } from '../lib/errors'
import { formatDateTime } from '../lib/format'
import AccessDeniedPage from './AccessDeniedPage'

interface Notice {
    severity: 'success' | 'error'
    message: string
}

export default function AdminUsersPage() {
    const { user: currentUser } = useAuth()
    const queryClient = useQueryClient()
    const theme = useTheme()
    const isPhone = useMediaQuery(theme.breakpoints.down('sm'))

    const [createOpen, setCreateOpen] = useState(false)
    // Kept after the dialog closes so its text does not go blank during the close animation.
    const [userToDelete, setUserToDelete] = useState<UserResponse | null>(null)
    const [confirmOpen, setConfirmOpen] = useState(false)
    const [notice, setNotice] = useState<Notice | null>(null)

    const usersQuery = useQuery({ queryKey: queryKeys.users, queryFn: getUsers })

    const removeFromList = (id: string) =>
        queryClient.setQueryData<UserResponse[]>(queryKeys.users, (users) => users?.filter((u) => u.id !== id))

    const deleteMutation = useMutation({
        mutationFn: (user: UserResponse) => deleteUser(user.id),
        onSuccess: (_, user) => {
            removeFromList(user.id)
            setNotice({ severity: 'success', message: `${user.name} was deleted.` })
        },
        onError: (error, user) => {
            const apiError = toApiError(error)
            // Already deleted (for example in another tab): the list was out of date, so fix it.
            if (apiError.status === 404) {
                removeFromList(user.id)
                setNotice({ severity: 'error', message: `${user.name} had already been deleted.` })
            } else {
                setNotice({ severity: 'error', message: apiError.message })
            }
        },
        onSettled: () => setConfirmOpen(false),
    })

    // The route guard already hides this page from normal users; this covers the API saying no anyway.
    if (usersQuery.isError && toApiError(usersQuery.error).status === 403) return <AccessDeniedPage />

    const isSelf = (user: UserResponse) => user.id === currentUser?.id

    const askToDelete = (user: UserResponse) => {
        setUserToDelete(user)
        setConfirmOpen(true)
    }

    return (
        <Stack spacing={3}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                <Typography variant="h4" component="h1" sx={{ fontSize: { xs: '1.75rem', sm: '2.125rem' } }}>
                    Users
                </Typography>
                <Button variant="contained" startIcon={<PersonAddIcon />} onClick={() => setCreateOpen(true)}>
                    Add user
                </Button>
            </Box>

            {usersQuery.isPending ? (
                <Stack spacing={1} aria-label="Loading users">
                    {[0, 1, 2].map((i) => (
                        <Skeleton key={i} variant="rounded" height={56} />
                    ))}
                </Stack>
            ) : usersQuery.isError ? (
                <ErrorState
                    message={`Could not load users. ${toApiError(usersQuery.error).message}`}
                    onRetry={() => void usersQuery.refetch()}
                />
            ) : usersQuery.data.length === 0 ? (
                <EmptyState title="No users yet" />
            ) : isPhone ? (
                // Cards on phones, where a five-column table would not fit.
                <Stack component="ul" spacing={1.5} sx={{ listStyle: 'none', p: 0, m: 0 }}>
                    {usersQuery.data.map((user) => (
                        <Paper key={user.id} component="li" variant="outlined" sx={{ p: 2 }}>
                            <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
                                <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                                    <Typography sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>{user.name}</Typography>
                                    <Typography variant="body2" color="text.secondary" sx={{ overflowWrap: 'anywhere' }}>
                                        {user.email}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Created {formatDateTime(user.createdAt)}
                                    </Typography>
                                    <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                                        <RoleChip user={user} />
                                        {isSelf(user) && <Chip size="small" variant="outlined" label="You" />}
                                    </Stack>
                                </Box>
                                {!isSelf(user) && <DeleteButton user={user} onClick={askToDelete} />}
                            </Stack>
                        </Paper>
                    ))}
                </Stack>
            ) : (
                <TableContainer component={Paper} variant="outlined">
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>Name</TableCell>
                                <TableCell>Email</TableCell>
                                <TableCell>Role</TableCell>
                                <TableCell>Created</TableCell>
                                <TableCell align="right">
                                    <Box component="span" sx={visuallyHidden}>
                                        Actions
                                    </Box>
                                </TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {usersQuery.data.map((user) => (
                                <TableRow key={user.id} hover>
                                    <TableCell sx={{ fontWeight: 500 }}>
                                        {user.name}
                                        {isSelf(user) && <Chip size="small" variant="outlined" label="You" sx={{ ml: 1 }} />}
                                    </TableCell>
                                    <TableCell sx={{ overflowWrap: 'anywhere' }}>{user.email}</TableCell>
                                    <TableCell>
                                        <RoleChip user={user} />
                                    </TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(user.createdAt)}</TableCell>
                                    <TableCell align="right">
                                        {/* The API refuses to delete your own account, so the button is not offered. */}
                                        {!isSelf(user) && <DeleteButton user={user} onClick={askToDelete} />}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            )}

            <CreateUserDialog
                open={createOpen}
                onClose={() => setCreateOpen(false)}
                onCreated={(user) => setNotice({ severity: 'success', message: `${user.name} was added.` })}
            />

            <ConfirmDialog
                open={confirmOpen}
                title="Delete user?"
                confirmLabel="Delete"
                confirmColor="error"
                pending={deleteMutation.isPending}
                onConfirm={() => userToDelete && deleteMutation.mutate(userToDelete)}
                onCancel={() => setConfirmOpen(false)}
            >
                <strong>{userToDelete?.name}</strong> ({userToDelete?.email}) will no longer be able to log in, and any
                session they have open ends immediately.
            </ConfirmDialog>

            <Snackbar
                open={notice !== null}
                autoHideDuration={5000}
                onClose={() => setNotice(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                {/* Snackbar needs a single element child; the notice is kept until the close animation ends. */}
                <Alert severity={notice?.severity ?? 'success'} variant="filled" onClose={() => setNotice(null)}>
                    {notice?.message}
                </Alert>
            </Snackbar>
        </Stack>
    )
}

function RoleChip({ user }: { user: UserResponse }) {
    return <Chip size="small" label={user.role} color={user.role === 'Admin' ? 'warning' : 'default'} />
}

function DeleteButton({ user, onClick }: { user: UserResponse; onClick: (user: UserResponse) => void }) {
    return (
        <Tooltip title="Delete user">
            <IconButton aria-label={`Delete ${user.name}`} color="error" onClick={() => onClick(user)}>
                <DeleteOutlineIcon />
            </IconButton>
        </Tooltip>
    )
}

// Read by screen readers but not shown, so the empty header cell still has a name.
const visuallyHidden = {
    position: 'absolute',
    width: 1,
    height: 1,
    overflow: 'hidden',
    clip: 'rect(0 0 0 0)',
    whiteSpace: 'nowrap',
} as const
