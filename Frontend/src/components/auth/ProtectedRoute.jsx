import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useSelector } from 'react-redux'

/**
 * ProtectedRoute component - Only accessible when user is authenticated
 * Redirects to login if user is not authenticated
 */
export const ProtectedRoute = ({ children }) => {
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return children || <Outlet />
}

/**
 * PublicRoute component - Only accessible when user is NOT authenticated
 * Redirects to login if user is already authenticated (to let them navigate from there)
 */
export const PublicRoute = ({ children }) => {
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)

  if (isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return children || <Outlet />
}