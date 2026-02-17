import React, { useState } from 'react'
import { useDispatch } from 'react-redux'
import { useNavigate, Link } from 'react-router-dom'
import { setError, setLoading } from '../../store/slices/authSlice'
import { authService } from '../../services/authService'
import { Input } from '../../components/ui/Input'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Calendar, UserCircle, Stethoscope } from 'lucide-react'

export const Register = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    role: 'User',
  })
  const [localError, setLocalError] = useState('')
  const [loading, setLocalLoading] = useState(false)
  const [avatarFile, setAvatarFile] = useState(null)
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')

    if (formData.password !== formData.confirmPassword) {
      setLocalError('Passwords do not match')
      return
    }

    if (formData.password.length < 6) {
      setLocalError('Password must be at least 6 characters')
      return
    }

    setLocalLoading(true)
    dispatch(setLoading(true))

    try {
      const response = await authService.register(
        formData.email,
        formData.password,
        formData.firstName,
        formData.lastName,
        formData.role,
        avatarFile
      )

      navigate('/verify-email', {
        state: {
          email: formData.email,
          message: response?.message || 'Registration successful. Please confirm your email before signing in.',
        },
      })
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Registration failed. Please try again.'
      setLocalError(errorMessage)
      dispatch(setError(errorMessage))
    } finally {
      setLocalLoading(false)
      dispatch(setLoading(false))
    }
  }

  return (
    <div className="min-h-screen bg-background-app flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <div className="flex flex-col items-center mb-6">
          <div className="w-16 h-16 rounded-2xl flex items-center justify-center bg-primary-accent mb-4">
            <Calendar size={32} className="text-white" />
          </div>
          <h1 className="text-2xl font-bold text-text-primary">Healthcare Hub</h1>
          <p className="text-text-secondary mt-2">Create your account</p>
        </div>

        {localError && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-sm">
            {localError}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Role Selection */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-text-primary mb-3">
              I am registering as
            </label>
            <div className="grid grid-cols-2 gap-4">
              <button
                type="button"
                onClick={() => setFormData({ ...formData, role: 'User' })}
                className={`p-4 border-2 rounded-lg transition-all ${
                  formData.role === 'User'
                    ? 'border-primary-accent bg-primary-accent/10'
                    : 'border-gray-300 hover:border-gray-400'
                }`}
              >
                <UserCircle
                  size={32}
                  className={`mx-auto mb-2 ${
                    formData.role === 'User' ? 'text-primary-accent' : 'text-gray-400'
                  }`}
                />
                <p className="font-medium text-text-primary">Client</p>
                <p className="text-xs text-text-secondary mt-1">
                  Book appointments
                </p>
              </button>
              
              <button
                type="button"
                onClick={() => setFormData({ ...formData, role: 'Professional' })}
                className={`p-4 border-2 rounded-lg transition-all ${
                  formData.role === 'Professional'
                    ? 'border-primary-accent bg-primary-accent/10'
                    : 'border-gray-300 hover:border-gray-400'
                }`}
              >
                <Stethoscope
                  size={32}
                  className={`mx-auto mb-2 ${
                    formData.role === 'Professional' ? 'text-primary-accent' : 'text-gray-400'
                  }`}
                />
                <p className="font-medium text-text-primary">Doctor</p>
                <p className="text-xs text-text-secondary mt-1">
                  Provide services
                </p>
              </button>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Input
              label="First Name"
              name="firstName"
              value={formData.firstName}
              onChange={handleChange}
              placeholder="John"
              required
            />

            <Input
              label="Last Name"
              name="lastName"
              value={formData.lastName}
              onChange={handleChange}
              placeholder="Doe"
              required
            />
          </div>

          <Input
            label="Email"
            type="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            placeholder="john.doe@example.com"
            required
          />

          <div className="mb-4">
            <label className="block text-sm font-medium text-text-primary mb-2">Avatar (optional)</label>
            <input
              type="file"
              accept="image/png,image/jpeg,image/webp,image/gif"
              onChange={(e) => setAvatarFile(e.target.files?.[0] || null)}
              className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent bg-white"
            />
            <p className="text-xs text-text-secondary mt-1">Max size: 5MB</p>
          </div>

          <Input
            label="Password"
            type="password"
            name="password"
            value={formData.password}
            onChange={handleChange}
            placeholder="At least 6 characters"
            required
          />

          <Input
            label="Confirm Password"
            type="password"
            name="confirmPassword"
            value={formData.confirmPassword}
            onChange={handleChange}
            placeholder="Repeat your password"
            required
          />

          <Button
            type="submit"
            variant="primary"
            className="w-full"
            disabled={loading}
          >
            {loading ? 'Creating account...' : 'Sign Up'}
          </Button>
        </form>

        <div className="mt-6 text-center">
          <p className="text-sm text-text-secondary">
            Already have an account?{' '}
            <Link to="/login" className="text-primary-accent font-medium hover:underline">
              Sign in
            </Link>
          </p>
        </div>
      </Card>
    </div>
  )
}
