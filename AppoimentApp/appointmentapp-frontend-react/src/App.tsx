import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { ToastProvider } from './components/shared/ToastContext';
import LoginPage from './pages/LoginPage';
import PatientDashboard from './pages/PatientDashboard';
import DoctorDashboard from './pages/DoctorDashboard';
import AdminDashboard from './pages/AdminDashboard';
import AppointmentBookingLayout from './pages/AppointmentBookingLayout';
import './App.css';
import './index.css';

// Protected Route Component
const ProtectedRoute = ({ children, allowedRoles }: { children: React.ReactNode; allowedRoles: string[] }) => {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  if (user && !allowedRoles.includes(user.role)) {
    // Redirect to their appropriate dashboard
    const redirectPath = user.role === 'patient' ? '/patient' : user.role === 'doctor' ? '/doctor' : '/admin';
    return <Navigate to={redirectPath} replace />;
  }

  return <>{children}</>;
};

function AppRoutes() {
  const { isAuthenticated, user } = useAuth();

  return (
    <Routes>
      {/* Public Routes */}
      <Route 
        path="/" 
        element={
          isAuthenticated ? (
            <Navigate to={user?.role === 'patient' ? '/patient' : user?.role === 'doctor' ? '/doctor' : '/admin'} replace />
          ) : (
            <LoginPage />
          )
        } 
      />

      {/* Demo Layout Route */}
      <Route path="/layout-demo" element={<AppointmentBookingLayout />} />

      {/* Protected Routes */}
      <Route
        path="/patient"
        element={
          <ProtectedRoute allowedRoles={['patient']}>
            <PatientDashboard />
          </ProtectedRoute>
        }
      />

      <Route
        path="/doctor"
        element={
          <ProtectedRoute allowedRoles={['doctor']}>
            <DoctorDashboard />
          </ProtectedRoute>
        }
      />

      <Route
        path="/admin"
        element={
          <ProtectedRoute allowedRoles={['admin']}>
            <AdminDashboard />
          </ProtectedRoute>
        }
      />

      {/* Catch all - redirect to home */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function App() {
  return (
    <Router>
      <AuthProvider>
        <ToastProvider>
          <AppRoutes />
        </ToastProvider>
      </AuthProvider>
    </Router>
  );
}

export default App;
