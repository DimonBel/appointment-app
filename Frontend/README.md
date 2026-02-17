# Healthcare Hub - Unified Frontend

A modern, unified frontend application built with React that integrates both **Appointment Management** and **Chat** functionality into a single, cohesive user experience.

## 🎯 Overview

This application combines two microservices into one seamless frontend:

- **Appointment Service**: Book, manage, and track medical appointments
- **Chat Service**: Real-time messaging with healthcare professionals

## 🛠 Tech Stack

- **React 19.2** - UI Framework
- **Redux Toolkit** - State Management
- **Redux Persist** - Persistent Storage
- **React Router DOM** - Navigation
- **Tailwind CSS** - Styling
- **Vite** - Build Tool
- **Axios** - HTTP Client
- **SignalR** - Real-time Communication
- **Lucide React** - Icons

## 📁 Project Structure

```
Frontend/
├── public/              # Static assets
├── src/
│   ├── components/      # Reusable components
│   │   ├── layout/      # Layout components (Header, Sidebar, MainContent)
│   │   └── ui/          # UI components (Button, Card, Input, etc.)
│   ├── pages/           # Page components
│   │   ├── auth/        # Authentication pages (Login, Register)
│   │   ├── appointments/# Appointment pages (Bookings, DoctorList)
│   │   └── chat/        # Chat page
│   ├── services/        # API services
│   │   ├── authService.js
│   │   ├── appointmentService.js
│   │   ├── chatService.js
│   │   └── signalRService.js
│   ├── store/           # Redux store
│   │   ├── slices/      # Redux slices
│   │   └── index.js     # Store configuration
│   ├── App.jsx          # Main app component
│   ├── main.jsx         # Entry point
│   └── index.css        # Global styles
├── .env                 # Environment variables
├── package.json
├── vite.config.js
└── tailwind.config.js
```

## 🚀 Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn
- Running backend services:
  - Identity Service (http://localhost:5005)
  - Appointment Service (http://localhost:5001)
  - Chat Service (http://localhost:5002)

### Installation

1. **Navigate to the Frontend directory:**

   ```bash
   cd Frontend
   ```

2. **Install dependencies:**

   ```bash
   npm install
   ```

3. **Configure environment variables:**

   Edit `.env` file if needed (default values are already set):

   ```env
   VITE_IDENTITY_API_URL=http://localhost:5005/api
   VITE_APPOINTMENT_API_URL=http://localhost:5001/api
   VITE_CHAT_API_URL=http://localhost:5002/api
   VITE_CHAT_HUB_URL=http://localhost:5002/chathub
   VITE_ORDER_HUB_URL=http://localhost:5001/orderhub
   ```

4. **Start the development server:**

   ```bash
   npm run dev
   ```

5. **Open your browser:**
   Navigate to `http://localhost:3000`

## 📱 Features

### Authentication

- User registration with email and password
- Secure login with JWT tokens
- Token refresh mechanism
- Persistent authentication state

### Appointments

- **My Bookings**: View all appointments (upcoming, completed, cancelled)
- **Find Doctors**: Search and filter healthcare professionals
- **Book Appointments**: Schedule appointments with doctors
- **Manage Appointments**: Reschedule or cancel appointments
- **Real-time Updates**: Receive appointment notifications via SignalR

### Chat

- **Real-time Messaging**: Instant messaging with healthcare professionals
- **Chat History**: View past conversations
- **Online Status**: See who's online
- **Message Notifications**: Get notified of new messages
- **SignalR Integration**: WebSocket-based real-time communication

### Profile & Settings

- **Profile Management**: Update personal information
- **Settings**: Configure notifications and preferences
- **Theme Support**: Light/Dark/System themes

## 🎨 Design System

The application uses a consistent design system based on the original AppointmentApp mockup:

### Colors

- **Primary Dark**: `#1E2A38`
- **Primary Light**: `#2C3E50`
- **Primary Accent**: `#4DA3FF`
- **Background**: `#F2F2F2`
- **Text Primary**: `#1E1E1E`
- **Text Secondary**: `#6B7280`

### Components

All components follow a consistent design pattern with:

- Rounded corners (1rem - 1.5rem)
- Smooth transitions
- Hover effects
- Responsive design

## 🔄 State Management

### Redux Slices

1. **authSlice**: User authentication and tokens
2. **chatsSlice**: Chat list and selected chat
3. **messagesSlice**: Messages by chat ID
4. **appointmentsSlice**: Appointment data
5. **uiSlice**: UI state (theme, sidebar, notifications)

### Persistence

User authentication state is persisted using `redux-persist` with localStorage.

## 🌐 API Integration

### Services

- **authService**: Handles authentication (login, register, refresh token)
- **appointmentService**: Manages appointments and doctors
- **chatService**: Handles chat operations
- **signalRService**: Real-time communication for both chat and appointments

### API Proxy

Vite dev server is configured to proxy API requests:

- `/api/auth` → Identity Service (5005)
- `/api/appointment` → Appointment Service (5001)
- `/api/chat` → Chat Service (5002)
- `/chathub` → Chat SignalR Hub
- `/orderhub` → Order SignalR Hub

## 🧪 Development

### Available Scripts

```bash
npm run dev      # Start development server
npm run build    # Build for production
npm run preview  # Preview production build
npm run lint     # Run ESLint
```

### Code Style

- ESLint configuration included
- Follows React best practices
- Uses functional components and hooks

## 📦 Building for Production

```bash
npm run build
```

The build output will be in the `dist/` directory.

## 🔐 Environment Variables

All environment variables use the `VITE_` prefix:

| Variable                   | Description                      | Default                          |
| -------------------------- | -------------------------------- | -------------------------------- |
| `VITE_IDENTITY_API_URL`    | Identity service API endpoint    | `http://localhost:5005/api`      |
| `VITE_APPOINTMENT_API_URL` | Appointment service API endpoint | `http://localhost:5001/api`      |
| `VITE_CHAT_API_URL`        | Chat service API endpoint        | `http://localhost:5002/api`      |
| `VITE_CHAT_HUB_URL`        | Chat SignalR hub URL             | `http://localhost:5002/chathub`  |
| `VITE_ORDER_HUB_URL`       | Order SignalR hub URL            | `http://localhost:5001/orderhub` |

## 🎯 Key Features

### Navigation

- Integrated sidebar with 5 main sections:
  - My Bookings
  - Find Doctors
  - Messages (Chat)
  - Profile
  - Settings

### Responsive Design

- Mobile-first approach
- Responsive grid layouts
- Adaptive navigation

### Real-time Features

- Live chat updates
- Appointment notifications
- Online status indicators

## 🤝 Integration with Backend Services

This frontend connects to three backend services:

1. **Identity Service** (Port 5005)
   - User authentication
   - JWT token management
   - User profile data

2. **Appointment Service** (Port 5001)
   - Appointment CRUD operations
   - Professional listings
   - Real-time order updates via SignalR

3. **Chat Service** (Port 5002)
   - Chat conversations
   - Real-time messaging via SignalR
   - Message history

## 📝 Notes

- Ensure all backend services are running before starting the frontend
- JWT tokens are stored in Redux and persisted to localStorage
- SignalR connections are established automatically upon authentication
- The application checks authentication status on mount and redirects to login if needed

## 🎉 What's Different from Original Frontends?

### Merged Features

- ✅ Combined appointment and chat functionality in one app
- ✅ Single authentication flow for both services
- ✅ Unified navigation and layout
- ✅ Consistent design system
- ✅ Shared state management
- ✅ Single build and deployment

### Improvements

- ✅ Better code organization
- ✅ Centralized API services
- ✅ Redux for predictable state management
- ✅ Persistent authentication
- ✅ Modern React patterns (hooks, functional components)
- ✅ Responsive design improvements

## 🐛 Troubleshooting

### Common Issues

1. **Cannot connect to backend**
   - Verify all backend services are running
   - Check `.env` file for correct URLs
   - Ensure CORS is configured on backend

2. **SignalR connection fails**
   - Check SignalR hub URLs
   - Verify authentication token is valid
   - Check browser console for errors

3. **Build fails**
   - Run `npm install` to ensure dependencies are up to date
   - Clear `node_modules` and reinstall if needed

## 📄 License

This project is part of the Healthcare Hub microservices architecture.

---

**Made with ❤️ using React, Redux, and Tailwind CSS**
