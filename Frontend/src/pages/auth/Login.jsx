import React, { useState } from 'react'
import { useDispatch } from 'react-redux'
import { useNavigate, Link, useLocation } from 'react-router-dom'
import { setCredentials, setError, setLoading } from '../../store/slices/authSlice'
import { authService } from '../../services/authService'
import { Input } from '../../components/ui/Input'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Calendar } from 'lucide-react'

export const Login = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [localError, setLocalError] = useState('')
  const [loading, setLocalLoading] = useState(false)
  const [infoMessage, setInfoMessage] = useState('')
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const location = useLocation()

  React.useEffect(() => {
    if (location.state?.message) {
      setInfoMessage(location.state.message)
    }
  }, [location.state])

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')
    setLocalLoading(true)
    dispatch(setLoading(true))

    try {
      const response = await authService.login(email, password)
      
      dispatch(setCredentials({
        user: response.user,
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
      }))
      
      navigate('/')
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Login failed. Please check your credentials.'
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
          <p className="text-text-secondary mt-2">Sign in to your account</p>
        </div>

        {localError && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-sm">
            {localError}
          </div>
        )}

        {infoMessage && (
          <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg text-blue-700 text-sm">
            {infoMessage}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Enter your email"
            required
          />

          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Enter your password"
            required
          />

          <Button
            type="submit"
            variant="primary"
            className="w-full"
            disabled={loading}
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </Button>
        </form>

        <div className="mt-6 text-center">
          <p className="text-sm text-text-secondary">
            Don't have an account?{' '}
            <Link to="/register" className="text-primary-accent font-medium hover:underline">
              Sign up
            </Link>
          </p>
        </div>
      </Card>
    </div>
  )
}
