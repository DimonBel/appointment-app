import { apiClient } from './apiClient';
import type { User, RegisterDto, LoginDto } from '../types/index';

export const authService = {
  login: async (credentials: { email: string; password: string; rememberMe?: boolean }) => {
    const loginDto: LoginDto = {
      email: credentials.email,
      password: credentials.password,
      rememberMe: credentials.rememberMe || false
    };

    const response = await apiClient.post<{ message: string; user: User }>('/api/auth/login', loginDto);

    // Since backend uses cookie-based auth, we don't need to store a JWT token
    // The authentication cookie will be handled automatically by the browser
    return {
      user: response.user,
      token: null // No JWT token needed with cookie auth
    };
  },

  register: async (userData: { username: string; email: string; password: string; confirmPassword?: string }) => {
    const registerDto: RegisterDto = {
      email: userData.email,
      password: userData.password,
      confirmPassword: userData.confirmPassword || userData.password,
      userName: userData.username
    };

    const response = await apiClient.post<{ message: string; user: User }>('/api/auth/register', registerDto);

    // Since backend uses cookie-based auth, we don't need to store a JWT token
    // The authentication cookie will be handled automatically by the browser
    return {
      user: response.user,
      token: null // No JWT token needed with cookie auth
    };
  },

  logout: async () => {
    try {
      await apiClient.post('/api/auth/logout');
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      // Clear any stored token if present
      localStorage.removeItem('token');
    }
    return true;
  },

  getCurrentUser: async (): Promise<User | null> => {
    try {
      const user = await apiClient.get<User>('/api/auth/current');
      return user;
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
  },
};