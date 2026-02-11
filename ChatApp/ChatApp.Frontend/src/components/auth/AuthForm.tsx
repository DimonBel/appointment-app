import React, { useState, useEffect } from 'react';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { Card } from '../ui/Card';
import { Eye, EyeOff, Mail, Lock, User } from 'lucide-react';

interface AuthFormProps {
  isLogin: boolean;
  onSubmit: (data: { email: string; password: string; username?: string }) => Promise<void>;
  isLoading: boolean;
  onToggleMode: () => void;
}

export const AuthForm: React.FC<AuthFormProps> = ({
  isLogin,
  onSubmit,
  isLoading,
  onToggleMode,
}) => {
  const [isLoginMode, setIsLoginMode] = useState(isLogin);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    username: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setIsLoginMode(isLogin);
  }, [isLogin]);

  const handleToggle = () => {
    setIsLoginMode(!isLoginMode);
    onToggleMode();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!isLoginMode && formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    try {
      if (isLoginMode) {
        await onSubmit({
          email: formData.email,
          password: formData.password,
        });
      } else {
        await onSubmit({
          email: formData.email,
          password: formData.password,
          username: formData.username,
        });
      }
    } catch (err: any) {
      // Handle error message properly
      const errorMessage = err?.response?.data?.message || err?.message || 'Authentication failed. Please try again.';
      setError(errorMessage);
    }
  };



  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md p-8 space-y-6 border shadow-lg">
        <div className="text-center space-y-2">
          <div className="w-12 h-12 bg-primary rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-6 h-6 text-primary-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
          </div>
          <h1 className="text-2xl font-bold">
            {isLoginMode ? 'Welcome back' : 'Create account'}
          </h1>
          <p className="text-muted-foreground">
            {isLoginMode 
              ? 'Sign in to your account to continue' 
              : 'Sign up to start chatting with friends'
            }
          </p>
        </div>

        {error && (
          <div className="bg-destructive/10 border border-destructive/20 text-destructive px-4 py-3 rounded-md text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {!isLoginMode && (
            <div className="relative">
              <User className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                type="text"
                name="username"
                placeholder="Username"
                value={formData.username}
                onChange={handleChange}
                required
                className="pl-10"
              />
            </div>
          )}
          {!isLoginMode && (
            <div className="relative">
              <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                type={showPassword ? 'text' : 'password'}
                name="confirmPassword"
                placeholder="Confirm Password"
                value={formData.confirmPassword}
                onChange={handleChange}
                required
                className="pl-10 pr-10"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          )}

          <div className="relative">
            <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              type="email"
              name="email"
              placeholder="Email address"
              value={formData.email}
              onChange={handleChange}
              required
              className="pl-10"
            />
          </div>

          <div className="relative">
            <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              type={showPassword ? 'text' : 'password'}
              name="password"
              placeholder="Password"
              value={formData.password}
              onChange={handleChange}
              required
              className="pl-10 pr-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>

          <Button
            type="submit"
            className="w-full"
            disabled={isLoading}
          >
            {isLoading ? 'Please wait...' : isLoginMode ? 'Sign In' : 'Sign Up'}
          </Button>
        </form>

        <div className="text-center">
          <button
            type="button"
            onClick={handleToggle}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            {isLoginMode 
              ? "Don't have an account? Sign up" 
              : 'Already have an account? Sign in'
            }
          </button>
        </div>
      </Card>
    </div>
  );
};