import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  appointments: [],
  selectedAppointment: null,
  isLoading: false,
  error: null,
}

const appointmentsSlice = createSlice({
  name: 'appointments',
  initialState,
  reducers: {
    setAppointments: (state, action) => {
      state.appointments = action.payload
    },
    addAppointment: (state, action) => {
      state.appointments.unshift(action.payload)
    },
    updateAppointment: (state, action) => {
      const index = state.appointments.findIndex(apt => apt.id === action.payload.id)
      if (index !== -1) {
        state.appointments[index] = { ...state.appointments[index], ...action.payload }
      }
    },
    deleteAppointment: (state, action) => {
      state.appointments = state.appointments.filter(apt => apt.id !== action.payload)
    },
    selectAppointment: (state, action) => {
      state.selectedAppointment = action.payload
    },
    setLoading: (state, action) => {
      state.isLoading = action.payload
    },
    setError: (state, action) => {
      state.error = action.payload
    },
  },
})

export const {
  setAppointments,
  addAppointment,
  updateAppointment,
  deleteAppointment,
  selectAppointment,
  setLoading,
  setError,
} = appointmentsSlice.actions

export default appointmentsSlice.reducer
