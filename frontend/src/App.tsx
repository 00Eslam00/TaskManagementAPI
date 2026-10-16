import { Route, Routes } from 'react-router'
import SetupPage from './pages/SetupPage'


export default function App() {
  return (
    <Routes>
      <Route path="*" element={<SetupPage />} />
    </Routes>
  )
}
