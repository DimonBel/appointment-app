import React, { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Outlet, Navigate } from 'react-router-dom'
import { useSelector } from 'react-redux'
import { Header } from './components/layout/Header'
import { MainContent } from './components/layout/MainContent'
import { Login } from './pages/auth/Login'
import { Register } from './pages/auth/Register'
import { VerifyEmail } from './pages/auth/VerifyEmail'
import { Landing } from './pages/Landing'
import { Bookings } from './pages/appointments/Bookings'
import { DoctorList } from './pages/appointments/DoctorList'
import { Chat } from './pages/chat/Chat'
import { Profile } from './pages/Profile'
import { Settings } from './pages/Settings'
import { DoctorProfile } from './pages/DoctorProfile'
import { DoctorPanel } from './pages/doctor/DoctorPanel'
import { ClientDetail } from './pages/doctor/ClientDetail'
import { ClientPanel } from './pages/client/ClientPanel'
import { Notifications } from './pages/notifications/Notifications'
import { AdminPanel } from './pages/admin/AdminPanel'
import { ManagementPanel } from './pages/management/ManagementPanel'
import { DocumentPreview } from './pages/management/DocumentPreview'
import { AIAssistant } from './pages/AIAssistant'
import { useNotificationHub } from './hooks/useNotificationHub'
import './App.css'

function App() {
  const [activeItem, setActiveItem] = useState('bookings')
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)

  // Connect to NotificationHub for real-time notifications
  // The hook handles authentication checks internally
  useNotificationHub()

  const handleNavigate = (itemId) => {
    setActiveItem(itemId)
  }

  const ProtectedLayout = () => (
    <div className="App">
      <Header 
        activeItem={activeItem}
        onNavigate={handleNavigate}
      />
      <MainContent>
        <Outlet />
      </MainContent>
    </div>
  )

  return (
    <BrowserRouter>
      <Routes>
        {/* Public routes - always accessible */}
        <Route path="/" element={<Landing />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/verify-email" element={<VerifyEmail />} />

        {/* Protected routes - require authentication */}
        <Route element={<ProtectedLayout />}>
          <Route path="/bookings" element={<Bookings />} />
          <Route path="/doctors" element={<DoctorList />} />
          <Route path="/chat" element={<Chat />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/doctor-profile" element={<DoctorProfile />} />
          <Route path="/doctor-panel" element={<DoctorPanel />} />
          <Route path="/doctor-panel/client/:clientId" element={<ClientDetail />} />
          <Route path="/client-panel" element={<ClientPanel />} />
          <Route path="/management" element={<ManagementPanel />} />
          <Route path="/document-preview" element={<DocumentPreview />} />
          <Route path="/notifications" element={<Notifications />} />
          <Route path="/ai-assistant" element={<AIAssistant />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="/admin" element={<AdminPanel />} />
        </Route>

        {/* Redirect unknown routes */}
        <Route path="*" element={<Navigate to={isAuthenticated ? "/bookings" : "/"} replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
