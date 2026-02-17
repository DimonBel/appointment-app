import React, { useState } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Avatar } from '../components/ui/Avatar'
import { updateUser } from '../store/slices/authSlice'
import { userService } from '../services/userService'
import { User, Mail, Phone, MapPin, Calendar, Camera } from 'lucide-react'

export const Profile = () => {
  const user = useSelector((state) => state.auth.user)
  const token = useSelector((state) => state.auth.token)
  const dispatch = useDispatch()
  const navigate = useNavigate()
  
  const [isEditing, setIsEditing] = useState(false)
  const [formData, setFormData] = useState({
    userName: user?.userName || '',
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phoneNumber: user?.phoneNumber || '',
    address: user?.address || '',
  })

  const [isSaving, setIsSaving] = useState(false)
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false)

  const handleAvatarChange = async (e) => {
    const file = e.target.files?.[0]
    if (!file || !user?.id || !token) return

    try {
      setIsUploadingAvatar(true)
      const result = await userService.uploadAvatar(user.id, file, token)
      const nextAvatarUrl = result?.avatarUrl
      if (nextAvatarUrl) {
        dispatch(updateUser({ avatarUrl: nextAvatarUrl }))
        window.dispatchEvent(new CustomEvent('profile-updated'))
      }
    } finally {
      setIsUploadingAvatar(false)
      e.target.value = ''
    }
  }

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!user?.id || !token) return

    try {
      setIsSaving(true)

      const userToUpdate = {
        ...user,
        userName: formData.userName,
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phoneNumber: formData.phoneNumber,
      }

      const savedUser = await userService.updateUser(userToUpdate, token)

      dispatch(
        updateUser({
          ...savedUser,
          address: formData.address,
        })
      )

      window.dispatchEvent(new CustomEvent('profile-updated'))
      setIsEditing(false)
    } finally {
      setIsSaving(false)
    }
  }

  const handleCancel = () => {
    setFormData({
      userName: user?.userName || '',
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      email: user?.email || '',
      phoneNumber: user?.phoneNumber || '',
      address: user?.address || '',
    })
    setIsEditing(false)
  }

  return (
    <MainContent>
      <SectionHeader 
        title="Profile"
        subtitle="Manage your personal information"
        action={
          <div className="flex gap-2">
            <Button onClick={() => navigate('/')} variant="outline">
              Home
            </Button>
            {!isEditing && (
              <Button onClick={() => setIsEditing(true)} variant="primary">
                Edit Profile
              </Button>
            )}
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Profile Card */}
        <Card>
          <CardContent className="text-center py-8">
            <div className="relative w-30 h-30 mx-auto mb-4">
              <Avatar 
                src={user?.avatarUrl}
                alt={`${user?.firstName} ${user?.lastName}`}
                size={120}
                className="mx-auto"
              />
              <label className="absolute bottom-1 right-1 w-9 h-9 rounded-full bg-primary-accent text-white flex items-center justify-center cursor-pointer hover:opacity-90 transition-opacity border-2 border-white">
                <Camera size={16} />
                <input
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/gif"
                  className="hidden"
                  onChange={handleAvatarChange}
                  disabled={isUploadingAvatar}
                />
              </label>
            </div>
            {isUploadingAvatar && (
              <p className="text-xs text-text-secondary -mt-2 mb-2">Uploading avatar...</p>
            )}
            <h2 className="text-xl font-semibold text-text-primary">
              {user?.firstName} {user?.lastName}
            </h2>
            <p className="text-text-secondary mt-1">{user?.email}</p>
            
            <div className="mt-6 pt-6 border-t border-gray-100">
              <div className="text-sm text-text-secondary space-y-2">
                <div className="flex items-center justify-center gap-2">
                  <Calendar size={16} />
                  <span>Member since {new Date().getFullYear()}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Information Card */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Personal Information</CardTitle>
          </CardHeader>
          <CardContent>
            {isEditing ? (
              <form onSubmit={handleSubmit} className="space-y-4">
                <Input
                  label="Nickname"
                  name="userName"
                  value={formData.userName}
                  onChange={handleChange}
                  icon={<User size={18} />}
                />

                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="First Name"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                    icon={<User size={18} />}
                  />
                  <Input
                    label="Last Name"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
                    icon={<User size={18} />}
                  />
                </div>

                <Input
                  label="Email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  icon={<Mail size={18} />}
                />

                <Input
                  label="Phone"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleChange}
                  icon={<Phone size={18} />}
                />

                <Input
                  label="Address"
                  name="address"
                  value={formData.address}
                  onChange={handleChange}
                  icon={<MapPin size={18} />}
                />

                <div className="flex gap-2 pt-4">
                  <Button type="submit" variant="primary" disabled={isSaving}>
                    {isSaving ? 'Saving...' : 'Save Changes'}
                  </Button>
                  <Button type="button" variant="outline" onClick={handleCancel}>
                    Cancel
                  </Button>
                </div>
              </form>
            ) : (
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-text-secondary">Nickname</label>
                  <p className="mt-1 text-text-primary">{user?.userName || 'Not provided'}</p>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-text-secondary">First Name</label>
                    <p className="mt-1 text-text-primary">{user?.firstName}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-text-secondary">Last Name</label>
                    <p className="mt-1 text-text-primary">{user?.lastName}</p>
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium text-text-secondary">Email</label>
                  <p className="mt-1 text-text-primary">{user?.email}</p>
                </div>

                <div>
                  <label className="text-sm font-medium text-text-secondary">Phone</label>
                  <p className="mt-1 text-text-primary">{user?.phoneNumber || 'Not provided'}</p>
                </div>

                <div>
                  <label className="text-sm font-medium text-text-secondary">Address</label>
                  <p className="mt-1 text-text-primary">{user?.address || 'Not provided'}</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </MainContent>
  )
}
