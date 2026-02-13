import { createContext, useContext, useState, type ReactNode } from 'react';

interface User {
  id: string;
  name: string;
  role: 'patient' | 'doctor' | 'admin';
  email?: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, _password: string, role: 'patient' | 'doctor' | 'admin') => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(() => {
    const savedUser = localStorage.getItem('user');
    return savedUser ? JSON.parse(savedUser) : null;
  });

  const login = (email: string, _password: string, role: 'patient' | 'doctor' | 'admin') => {
    // This is a mock login - in production, you'd call an authentication API
    const mockUser: User = {
      id: crypto.randomUUID(),
      name: email.split('@')[0],
      role,
      email
    };
    setUser(mockUser);
    localStorage.setItem('user', JSON.stringify(mockUser));
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('user');
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
