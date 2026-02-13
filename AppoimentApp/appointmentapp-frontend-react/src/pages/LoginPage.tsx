import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Calendar, Mail, Lock, User, UserCog, Shield } from 'lucide-react';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<'patient' | 'doctor' | 'admin'>('patient');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    login(email, password, role);
    const destination = role === 'patient' ? '/patient' : role === 'doctor' ? '/doctor' : '/admin';
    navigate(destination);
  };

  const roleOptions = [
    { value: 'patient', label: 'Patient', icon: User, color: 'from-blue-500 to-blue-600', description: 'Book appointments' },
    { value: 'doctor', label: 'Doctor', icon: UserCog, color: 'from-green-500 to-green-600', description: 'Manage appointments' },
    { value: 'admin', label: 'Admin', icon: Shield, color: 'from-purple-500 to-purple-600', description: 'Manage slots' }
  ] as const;

  return (
    <div className="min-h-screen flex items-center justify-center bg-mesh-gradient px-4 relative overflow-hidden">
      {/* Animated Background Blobs */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-gradient-to-br from-indigo-400/30 to-purple-400/30 rounded-full blur-3xl animate-float"></div>
        <div className="absolute -bottom-40 -left-40 w-96 h-96 bg-gradient-to-br from-blue-400/30 to-cyan-400/30 rounded-full blur-3xl animate-float" style={{ animationDelay: '1s' }}></div>
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-gradient-to-br from-purple-400/20 to-pink-400/20 rounded-full blur-3xl animate-morph"></div>
      </div>
      
      <div className="max-w-md w-full space-y-8 relative z-10">
        {/* Header */}
        <div className="text-center animate-fade-in-down">
          <div className="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-gradient-to-br from-indigo-600 via-purple-600 to-pink-600 shadow-2xl shadow-indigo-500/50 mb-6 animate-float">
            <Calendar className="w-10 h-10 text-white" />
          </div>
          <h2 className="text-4xl font-bold bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 bg-clip-text text-transparent mb-2">
            Healthcare Appointment
          </h2>
          <p className="mt-3 text-base text-gray-600 font-medium">
            Sign in to manage your appointments
          </p>
        </div>

        {/* Login Form */}
        <div className="glass-card p-8 animate-fade-in-up shadow-2xl">
          <form className="space-y-6" onSubmit={handleSubmit}>
            {/* Role Selection */}
            <div>
              <div className="text-sm font-semibold text-gray-700 mb-4 uppercase tracking-wide">
                Select Your Role
              </div>
              <div className="grid grid-cols-3 gap-4">
                {roleOptions.map((option, index) => {
                  const Icon = option.icon;
                  return (
                    <button
                      key={option.value}
                      type="button"
                      onClick={() => setRole(option.value)}
                      className={`relative p-4 rounded-xl border-2 transition-all duration-300 group animate-fade-in-up ${
                        role === option.value
                          ? 'border-indigo-500 bg-gradient-to-br from-indigo-50 to-purple-50 shadow-lg scale-105'
                          : 'border-gray-200 hover:border-indigo-300 hover:shadow-md hover:scale-102'
                      }`}
                      style={{ animationDelay: `${index * 100}ms` }}
                    >
                      <div className={`inline-flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br ${option.color} mb-3 shadow-md group-hover:scale-110 transition-transform`}>
                        <Icon className="w-6 h-6 text-white" />
                      </div>
                      <p className="text-xs font-bold text-gray-900">{option.label}</p>
                      <p className="text-xs text-gray-500 mt-1">{option.description}</p>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Email Input */}
            <div className="animate-fade-in-up" style={{ animationDelay: '200ms' }}>
              <label htmlFor="email" className="block text-sm font-semibold text-gray-700 mb-2">
                Email Address
              </label>
              <div className="relative group">
                <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400 group-focus-within:text-indigo-500 transition-colors" />
                <input
                  id="email"
                  name="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="pl-11 w-full px-4 py-3 border-2 border-gray-200 rounded-xl text-sm focus:ring-4 focus:ring-indigo-100 focus:border-indigo-500 transition-all outline-none bg-white/50 backdrop-blur-sm"
                  placeholder="you@example.com"
                />
              </div>
            </div>

            {/* Password Input */}
            <div className="animate-fade-in-up" style={{ animationDelay: '300ms' }}>
              <label htmlFor="password" className="block text-sm font-semibold text-gray-700 mb-2">
                Password
              </label>
              <div className="relative group">
                <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400 group-focus-within:text-indigo-500 transition-colors" />
                <input
                  id="password"
                  name="password"
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="pl-11 w-full px-4 py-3 border-2 border-gray-200 rounded-xl text-sm focus:ring-4 focus:ring-indigo-100 focus:border-indigo-500 transition-all outline-none bg-white/50 backdrop-blur-sm"
                  placeholder="••••••••"
                />
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              className="w-full py-3.5 px-4 rounded-xl shadow-lg text-base font-semibold text-white bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 hover:from-indigo-700 hover:via-purple-700 hover:to-pink-700 focus:outline-none focus:ring-4 focus:ring-purple-200 transition-all duration-300 transform hover:scale-[1.02] hover:shadow-2xl hover:shadow-purple-500/50 animate-fade-in-up"
              style={{ animationDelay: '400ms' }}
            >
              Sign in as {roleOptions.find(r => r.value === role)?.label}
            </button>
          </form>

          {/* Demo Credentials */}
          <div className="mt-6 pt-6 border-t border-gray-200 animate-fade-in-up" style={{ animationDelay: '500ms' }}>
            <div className="flex items-center justify-center space-x-2">
              <div className="w-2 h-2 bg-gradient-to-r from-indigo-500 to-purple-500 rounded-full animate-pulse"></div>
              <p className="text-sm text-gray-600 font-medium">
                Demo Mode: Enter any email and password
              </p>
              <div className="w-2 h-2 bg-gradient-to-r from-purple-500 to-pink-500 rounded-full animate-pulse" style={{ animationDelay: '0.5s' }}></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
