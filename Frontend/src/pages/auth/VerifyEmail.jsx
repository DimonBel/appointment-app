import React, { useEffect, useMemo, useState } from 'react'
import { useDispatch } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Calendar, CheckCircle2, AlertCircle, Mail } from 'lucide-react'
import { authService } from '../../services/authService'
import { setCredentials } from '../../store/slices/authSlice'

export const VerifyEmail = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const location = useLocation()

  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [status, setStatus] = useState(userId && token ? 'verifying' : 'pending')
  const [message, setMessage] = useState(
    location.state?.message || 'Please check your email and click the confirmation link.'
  )

  const email = location.state?.email || ''

  useEffect(() => {
    const verify = async () => {
      if (!userId || !token) return

      try {
        const response = await authService.confirmEmail(userId, token)
        const authPayload = response?.response
        if (authPayload?.accessToken && authPayload?.refreshToken && authPayload?.user) {
          dispatch(setCredentials({
            user: authPayload.user,
            accessToken: authPayload.accessToken,
            refreshToken: authPayload.refreshToken,
          }))

          setMessage(response?.message || 'Email confirmed successfully. Redirecting...')
          setStatus('success')
          setTimeout(() => navigate('/'), 700)
          return
        }

        setMessage(response?.message || 'Email confirmed successfully. You can now sign in.')
        setStatus('success')
      } catch (error) {
        const errorMessage = error.response?.data?.message || 'Email confirmation failed. Please request a new confirmation email.'
        setMessage(errorMessage)
        setStatus('error')
      }
    }

    verify()
  }, [userId, token, dispatch, navigate])

  const statusIcon = useMemo(() => {
    if (status === 'success') return <CheckCircle2 size={18} className="text-green-600" />
    if (status === 'error') return <AlertCircle size={18} className="text-red-600" />
    return <Mail size={18} className="text-blue-600" />
  }, [status])

  return (
    <div className="min-h-screen bg-background-app flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <div className="flex flex-col items-center mb-6">
          <div className="w-16 h-16 rounded-2xl flex items-center justify-center bg-primary-accent mb-4">
            <Calendar size={32} className="text-white" />
          </div>
          <h1 className="text-2xl font-bold text-text-primary">Healthcare Hub</h1>
          <p className="text-text-secondary mt-2">Verify your email</p>
        </div>

        <div className={`mb-4 p-3 rounded-lg border text-sm flex items-start gap-2 ${
          status === 'success'
            ? 'bg-green-50 border-green-200 text-green-700'
            : status === 'error'
              ? 'bg-red-50 border-red-200 text-red-700'
              : 'bg-blue-50 border-blue-200 text-blue-700'
        }`}>
          {statusIcon}
          <div>
            <p>{status === 'verifying' ? 'Verifying your email…' : message}</p>
            {status === 'pending' && email && (
              <p className="mt-1">A confirmation email was sent to <strong>{email}</strong>.</p>
            )}
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Link to="/login">
            <Button type="button" variant="primary" className="w-full">
              Go to Sign In
            </Button>
          </Link>

          <p className="text-xs text-text-secondary text-center mt-2">
            Didn&apos;t receive email? Check spam folder and try registering again.
          </p>
        </div>
      </Card>
    </div>
  )
}
