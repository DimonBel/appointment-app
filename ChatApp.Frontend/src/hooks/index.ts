import { useSelector, useDispatch } from 'react-redux';
import { useEffect } from 'react';
import type { RootState, AppDispatch } from '../store/index';
import { login, logout, register, checkAuthStatus } from '../store/slices/authSlice';
import { fetchChats } from '../store/slices/chatsSlice';
import { fetchMessages } from '../store/slices/messagesSlice';

export const useAppDispatch = () => useDispatch<AppDispatch>();

export const useAppSelector = <T>(selector: (state: RootState) => T) => {
  return useSelector(selector);
};

export const useAuth = () => {
  const dispatch = useAppDispatch();
  const { user, token, isAuthenticated, isLoading } = useAppSelector(state => state.auth);

  const handleLogin = async (credentials: { email: string; password: string }) => {
    await dispatch(login(credentials)).unwrap();
  };

  const handleRegister = async (userData: { username: string; email: string; password: string }) => {
    await dispatch(register(userData)).unwrap();
  };

  const handleLogout = async () => {
    await dispatch(logout()).unwrap();
  };

  const handleCheckAuthStatus = async () => {
    await dispatch(checkAuthStatus()).unwrap();
  };

  return {
    user,
    token,
    isAuthenticated,
    isLoading,
    login: handleLogin,
    register: handleRegister,
    logout: handleLogout,
    checkAuthStatus: handleCheckAuthStatus,
  };
};

export const useChats = () => {
  const dispatch = useAppDispatch();
  const { chats, selectedChat, isLoading, searchQuery, filteredChats } = useAppSelector(state => state.chats);
  const { isAuthenticated } = useAppSelector(state => state.auth);

  useEffect(() => {
    // Only fetch chats if user is authenticated and chats haven't been loaded yet
    if (isAuthenticated && chats.length === 0) {
      dispatch(fetchChats());
    }
  }, [dispatch, isAuthenticated, chats.length]);

  const handleSelectChat = (chat: any) => {
    dispatch({ type: 'chats/setSelectedChat', payload: chat });
  };

  return {
    chats,
    selectedChat,
    filteredChats,
    isLoading,
    searchQuery,
    selectChat: handleSelectChat,
  };
};