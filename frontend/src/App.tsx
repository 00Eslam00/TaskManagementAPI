import { Navigate, Route, Routes } from 'react-router'
import { AdminRoute } from './auth/AdminRoute'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { PublicOnlyRoute } from './auth/PublicOnlyRoute'
import { AppLayout } from './components/AppLayout'
import AdminUsersPage from './pages/AdminUsersPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import TaskDetailsPage from './pages/TaskDetailsPage'
import TasksPage from './pages/TasksPage'

export default function App() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/tasks" replace />} />
          <Route path="/tasks" element={<TasksPage />} />
          <Route path="/tasks/:id" element={<TaskDetailsPage />} />

          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<Navigate to="/admin/users" replace />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
          </Route>
        </Route>
      </Route>

      {/* Temporary until the "Page not found" page is added in Phase 4. */}
      <Route path="*" element={<Navigate to="/tasks" replace />} />
    </Routes>
  )
}
