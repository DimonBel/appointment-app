import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  friends: [],
  pendingRequests: [],
  sentRequests: [],
  friendIds: [],
  isLoading: false,
  error: null,
}

const friendsSlice = createSlice({
  name: 'friends',
  initialState,
  reducers: {
    setFriends: (state, action) => {
      state.friends = action.payload
      state.isLoading = false
    },
    setPendingRequests: (state, action) => {
      state.pendingRequests = action.payload
      state.isLoading = false
    },
    setSentRequests: (state, action) => {
      state.sentRequests = action.payload
    },
    setFriendIds: (state, action) => {
      state.friendIds = action.payload
    },
    addFriendId: (state, action) => {
      if (!state.friendIds.includes(action.payload)) {
        state.friendIds.push(action.payload)
      }
    },
    removeFriendId: (state, action) => {
      state.friendIds = state.friendIds.filter(id => id !== action.payload)
    },
    removePendingRequest: (state, action) => {
      state.pendingRequests = state.pendingRequests.filter(r => r.id !== action.payload)
    },
    removeSentRequest: (state, action) => {
      state.sentRequests = state.sentRequests.filter(r => r.id !== action.payload)
    },
    setLoading: (state, action) => {
      state.isLoading = action.payload
    },
    setError: (state, action) => {
      state.error = action.payload
      state.isLoading = false
    },
  },
})

export const {
  setFriends,
  setPendingRequests,
  setSentRequests,
  setFriendIds,
  addFriendId,
  removeFriendId,
  removePendingRequest,
  removeSentRequest,
  setLoading,
  setError,
} = friendsSlice.actions

export default friendsSlice.reducer
