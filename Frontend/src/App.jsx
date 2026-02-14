import React, { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useSelector } from 'react-redux'
import { Header } from './components/layout/Header'
import { Sidebar } from './components/layout/Sidebar'
import { MainContent } from './components/layout/MainContent'
import { Login } from './pages/auth/Login'
import { Register } from './pages/auth/Register'
import { Bookings } from './pages/appointments/Bookings'
import { DoctorList } from './pages/appointments/DoctorList'
import { Chat } from './pages/chat/Chat'
import { Profile } from './pages/Profile'
import { Settings } from './pages/Settings'
import './App.css'

function App() {
  const [activeItem, setActiveItem] = useState('bookings')
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)

  const handleNavigate = (itemId) => {
    setActiveItem(itemId)
  }

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen)
  }

  // If not authenticated, show login/register pages
  if (!isAuthenticated) {
    return (
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    )
  }

  // Authenticated routes
  return (
    <BrowserRouter>
      <div className="App">
        <div className="app-container">
          {sidebarOpen && (
            <Sidebar 
              activeItem={activeItem} 
              onNavigate={handleNavigate}
            />
          )}
          <div className="main-content">
            <Header onMenuClick={toggleSidebar} />
            <MainContent>
              <Routes>
                <Route path="/" element={<Bookings />} />
                <Route path="/doctors" element={<DoctorList />} />
                <Route path="/chat" element={<Chat />} />
                <Route path="/profile" element={<Profile />} />
                <Route path="/settings" element={<Settings />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </MainContent>
          </div>
        </div>
      </div>
    </BrowserRouter>
  )
}

export default App
