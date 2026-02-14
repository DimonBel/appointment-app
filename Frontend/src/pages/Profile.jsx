import React, { useState } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Avatar } from '../components/ui/Avatar'
import { updateUser } from '../store/slices/authSlice'
import { User, Mail, Phone, MapPin, Calendar } from 'lucide-react'

export const Profile = () => {
  const user = useSelector((state) => state.auth.user)
  const dispatch = useDispatch()
  
  const [isEditing, setIsEditing] = useState(false)
  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phone: user?.phone || '',
    address: user?.address || '',
  })

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    })
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    dispatch(updateUser(formData))
    setIsEditing(false)
  }

  const handleCancel = () => {
    setFormData({
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      email: user?.email || '',
      phone: user?.phone || '',
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
          !isEditing && (
            <Button onClick={() => setIsEditing(true)} variant="primary">
              Edit Profile
            </Button>
          )
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Profile Card */}
        <Card>
          <CardContent className="text-center py-8">
            <Avatar 
              src={user?.avatarUrl}
              alt={`${user?.firstName} ${user?.lastName}`}
              size={120}
              className="mx-auto mb-4"
            />
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
                  name="phone"
                  value={formData.phone}
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
                  <Button type="submit" variant="primary">
                    Save Changes
                  </Button>
                  <Button type="button" variant="outline" onClick={handleCancel}>
                    Cancel
                  </Button>
                </div>
              </form>
            ) : (
              <div className="space-y-4">
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
                  <p className="mt-1 text-text-primary">{user?.phone || 'Not provided'}</p>
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
