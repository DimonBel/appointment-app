import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { User, AuthState } from '../../types/index';
import { authService } from '../../services/authService';

const initialState: AuthState = {
  user: null,
  token: null, // With cookie-based auth, we don't rely on JWT tokens
  isAuthenticated: false,
  isLoading: false,
};

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: { email: string; password: string }, { rejectWithValue }) => {
    try {
      const response = await authService.login(credentials);
      // With cookie-based auth, we don't need to store a JWT token
      // The authentication cookie will be handled automatically by the browser
      return response;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const register = createAsyncThunk(
  'auth/register',
  async (userData: { username: string; email: string; password: string }, { rejectWithValue }) => {
    try {
      const response = await authService.register(userData);
      // With cookie-based auth, we don't need to store a JWT token
      // The authentication cookie will be handled automatically by the browser
      return response;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const logout = createAsyncThunk('auth/logout', async () => {
  localStorage.removeItem('token');
  return true;
});

export const checkAuthStatus = createAsyncThunk(
  'auth/checkAuthStatus',
  async (_, { dispatch }) => {
    try {
      // Try to get the current user to verify authentication status
      const user = await authService.getCurrentUser();
      if (user) {
        return { user, isAuthenticated: true };
      } else {
        return { user: null, isAuthenticated: false };
      }
    } catch (error) {
      // If getting current user fails, assume user is not authenticated
      return { user: null, isAuthenticated: false };
    }
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setUser: (state, action: PayloadAction<User>) => {
      state.user = action.payload;
      state.isAuthenticated = true;
    },
    clearAuth: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem('token');
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        // With cookie-based auth, token might be null
        state.token = action.payload.token || localStorage.getItem('token') || null;
        state.isAuthenticated = true;
      })
      .addCase(login.rejected, (state) => {
        state.isLoading = false;
      })
      .addCase(register.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(register.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        // With cookie-based auth, token might be null
        state.token = action.payload.token || localStorage.getItem('token') || null;
        state.isAuthenticated = true;
      })
      .addCase(register.rejected, (state) => {
        state.isLoading = false;
      })
      .addCase(logout.fulfilled, (state) => {
        state.user = null;
        state.token = null;
        state.isAuthenticated = false;
      })
      .addCase(checkAuthStatus.fulfilled, (state, action) => {
        state.user = action.payload.user;
        state.isAuthenticated = action.payload.isAuthenticated;
        state.isLoading = false;
      });
  },
});

export const { setUser, clearAuth } = authSlice.actions;
export default authSlice.reducer;