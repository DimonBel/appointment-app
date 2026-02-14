import React from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Bookings } from './pages/Bookings'
import { DoctorList } from './pages/DoctorList'
import { Profile } from './pages/Profile'
import { Settings } from './pages/Settings'
import Test from './pages/Test'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/test" element={<Test />} />
        <Route path="/" element={<Bookings />} />
        <Route path="/doctors" element={<DoctorList />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/settings" element={<Settings />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
