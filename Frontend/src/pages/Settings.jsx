import React, { useState } from 'react'
import { useDispatch } from 'react-redux'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { setTheme } from '../store/slices/uiSlice'
import { Bell, Lock, Shield, Moon, Sun, Globe } from 'lucide-react'

export const Settings = () => {
  const dispatch = useDispatch()
  const [notifications, setNotifications] = useState({
    email: true,
    push: true,
    sms: false,
  })

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
            <Button variant="outline" className="w-full justify-start">
              <Lock size={18} className="mr-2" />
              Change Password
            </Button>
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
