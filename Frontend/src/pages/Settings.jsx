import React, { useState } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { setTheme } from '../store/slices/uiSlice'
import { Bell, Lock, Shield, Moon, Sun, Globe, Check, AlertCircle } from 'lucide-react'
import axios from 'axios'

export const Settings = () => {
  const dispatch = useDispatch()
  const token = useSelector((state) => state.auth.token)
  const [notifications, setNotifications] = useState({
    email: true,
    push: true,
    sms: false,
  })
  const [showPasswordForm, setShowPasswordForm] = useState(false)
  const [passwordData, setPasswordData] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [passwordMessage, setPasswordMessage] = useState(null)
  const [passwordLoading, setPasswordLoading] = useState(false)

  const handleThemeChange = (theme) => {
    dispatch(setTheme(theme))
  }

  const handleNotificationChange = (type) => {
    setNotifications({
      ...notifications,
      [type]: !notifications[type],
    })
  }

  return (
    <MainContent>
      <SectionHeader 
        title="Settings"
        subtitle="Manage your account preferences"
      />

      <div className="space-y-6">
        {/* Notifications */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Bell size={20} className="text-primary-accent" />
              <CardTitle>Notifications</CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-text-primary">Email Notifications</p>
                <p className="text-sm text-text-secondary">Receive email about your appointments</p>
              </div>
              <button
                onClick={() => handleNotificationChange('email')}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  notifications.email ? 'bg-primary-accent' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    notifications.email ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-text-primary">Push Notifications</p>
                <p className="text-sm text-text-secondary">Receive push notifications on your device</p>
              </div>
              <button
                onClick={() => handleNotificationChange('push')}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  notifications.push ? 'bg-primary-accent' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    notifications.push ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-text-primary">SMS Notifications</p>
                <p className="text-sm text-text-secondary">Receive text messages about appointments</p>
              </div>
              <button
                onClick={() => handleNotificationChange('sms')}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  notifications.sms ? 'bg-primary-accent' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    notifications.sms ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>
          </CardContent>
        </Card>

        {/* Security */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Shield size={20} className="text-primary-accent" />
              <CardTitle>Security</CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {!showPasswordForm ? (
              <Button variant="outline" className="w-full justify-start" onClick={() => setShowPasswordForm(true)}>
                <Lock size={18} className="mr-2" />
                Change Password
              </Button>
            ) : (
              <div className="space-y-3">
                <Input
                  type="password"
                  placeholder="Current Password"
                  value={passwordData.currentPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
                />
                <Input
                  type="password"
                  placeholder="New Password (min 6 characters)"
                  value={passwordData.newPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                />
                <Input
                  type="password"
                  placeholder="Confirm New Password"
                  value={passwordData.confirmPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                />
                {passwordMessage && (
                  <div className={`flex items-center gap-2 text-sm ${passwordMessage.type === 'success' ? 'text-green-600' : 'text-red-600'}`}>
                    {passwordMessage.type === 'success' ? <Check size={14} /> : <AlertCircle size={14} />}
                    {passwordMessage.text}
                  </div>
                )}
                <div className="flex gap-2">
                  <Button
                    disabled={passwordLoading}
                    onClick={async () => {
                      if (passwordData.newPassword !== passwordData.confirmPassword) {
                        setPasswordMessage({ type: 'error', text: 'Passwords do not match' })
                        return
                      }
                      if (passwordData.newPassword.length < 6) {
                        setPasswordMessage({ type: 'error', text: 'Password must be at least 6 characters' })
                        return
                      }
                      setPasswordLoading(true)
                      setPasswordMessage(null)
                      try {
                        await axios.post('/api/auth/change-password', {
                          currentPassword: passwordData.currentPassword,
                          newPassword: passwordData.newPassword,
                        }, { headers: { Authorization: `Bearer ${token}` } })
                        setPasswordMessage({ type: 'success', text: 'Password changed successfully' })
                        setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' })
                        setTimeout(() => setShowPasswordForm(false), 2000)
                      } catch (err) {
                        setPasswordMessage({ type: 'error', text: err.response?.data?.message || 'Failed to change password' })
                      } finally {
                        setPasswordLoading(false)
                      }
                    }}
                  >
                    {passwordLoading ? 'Saving...' : 'Save Password'}
                  </Button>
                  <Button variant="outline" onClick={() => { setShowPasswordForm(false); setPasswordMessage(null) }}>
                    Cancel
                  </Button>
                </div>
              </div>
            )}
            <Button variant="outline" className="w-full justify-start">
              <Shield size={18} className="mr-2" />
              Two-Factor Authentication
            </Button>
          </CardContent>
        </Card>

        {/* Appearance */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Sun size={20} className="text-primary-accent" />
              <CardTitle>Appearance</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <p className="text-sm text-text-secondary mb-4">Choose your preferred theme</p>
              <div className="grid grid-cols-3 gap-4">
                <button
                  onClick={() => handleThemeChange('light')}
                  className="p-4 border-2 border-gray-200 rounded-lg hover:border-primary-accent transition-colors"
                >
                  <Sun size={24} className="mx-auto mb-2" />
                  <p className="text-sm font-medium">Light</p>
                </button>
                <button
                  onClick={() => handleThemeChange('dark')}
                  className="p-4 border-2 border-gray-200 rounded-lg hover:border-primary-accent transition-colors"
                >
                  <Moon size={24} className="mx-auto mb-2" />
                  <p className="text-sm font-medium">Dark</p>
                </button>
                <button
                  onClick={() => handleThemeChange('system')}
                  className="p-4 border-2 border-gray-200 rounded-lg hover:border-primary-accent transition-colors"
                >
                  <Globe size={24} className="mx-auto mb-2" />
                  <p className="text-sm font-medium">System</p>
                </button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </MainContent>
  )
}
