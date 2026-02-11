# ChatApp Frontend - Modern Real-time Chat Application

A modern, feature-rich chat web application built with React 18, TypeScript, and Tailwind CSS, integrated with ASP.NET backend.

## 🚀 Features

### Core Functionality
- **Real-time Messaging** - Instant message delivery with SignalR
- **User Authentication** - Secure login/registration with JWT
- **Modern UI/UX** - Clean, Telegram-inspired interface
- **Dark/Light Mode** - Seamless theme switching
- **Responsive Design** - Works perfectly on desktop and mobile

### UI Components
- **Chat List** - Searchable conversation list with online status
- **Message Bubbles** - Sent/delivered/read status indicators
- **Typing Indicators** - Real-time typing notifications
- **User Profiles** - Avatar display and online status
- **Smooth Animations** - Micro-interactions and transitions

### Technical Features
- **TypeScript** - Full type safety
- **Redux Toolkit** - Efficient state management
- **SignalR** - Real-time WebSocket communication
- **Axios** - HTTP client with interceptors
- **shadcn/ui** - Modern component library
- **Tailwind CSS** - Utility-first styling

## 🛠️ Tech Stack

- **Frontend**: React 18, TypeScript, Vite
- **State Management**: Redux Toolkit
- **UI Framework**: Tailwind CSS + shadcn/ui
- **Real-time**: SignalR
- **HTTP Client**: Axios
- **Icons**: Lucide React
- **Backend Integration**: ASP.NET Core Web API

## 📁 Project Structure

```
ChatApp.Frontend/
├── src/
│   ├── components/
│   │   ├── ui/              # Reusable UI components
│   │   ├── auth/            # Authentication components
│   │   └── chat/            # Chat-specific components
│   ├── pages/               # Page components
│   ├── store/
│   │   └── slices/          # Redux state slices
│   ├── services/            # API and WebSocket services
│   ├── hooks/               # Custom React hooks
│   ├── utils/               # Utility functions
│   └── types/               # TypeScript type definitions
├── public/
└── package.json
```

## 🚀 Getting Started

### Prerequisites
- Node.js 18+ 
- ASP.NET Backend running on https://localhost:7001

### Installation

1. **Clone and install dependencies:**
   ```bash
   cd ChatApp.Frontend
   npm install
   ```

2. **Configure environment:**
   ```bash
   cp .env.example .env
   # Update VITE_API_URL if your backend runs on different port
   ```

3. **Start development server:**
   ```bash
   npm run dev
   ```

4. **Build for production:**
   ```bash
   npm run build
   npm run preview
   ```

### Backend Integration

The frontend is configured to work with the existing ASP.NET backend:

- **API Base URL**: `https://localhost:7001` (configurable via `.env`)
- **SignalR Hub**: `/chathub`
- **Authentication**: Cookie-based with JWT fallback
- **CORS**: Enabled for frontend development

### API Endpoints Used

#### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration  
- `POST /api/auth/logout` - User logout
- `GET /api/auth/current` - Get current user

#### Chat
- `GET /api/chat/users` - Get all users
- `GET /api/chat/messages/{userId}` - Get messages with user
- `POST /api/chat/messages` - Send message
- `GET /api/chat/messages/recent` - Get recent messages

#### SignalR Events
- `ReceiveMessage` - New message received
- `MessageSent` - Message sent confirmation
- `UserTyping` - User typing notification
- `UserOnline/UserOffline` - User status changes

## 🎨 Design System

### Color Scheme
- **Primary**: Blue slate palette
- **Secondary**: Muted grays
- **Accent**: Subtle highlights
- **Dark Mode**: Complete dark theme support

### Component Library
- **Buttons**: Multiple variants (default, destructive, outline, ghost)
- **Inputs**: Form controls with validation
- **Avatars**: User profile images with fallbacks
- **Cards**: Content containers with shadows
- **Scroll Areas**: Custom scrollbars

### Animations
- **Fade In**: Smooth content appearance
- **Slide Up**: Modal and overlay animations
- **Hover States**: Interactive feedback
- **Typing Indicator**: Pulsing dots animation

## 🔧 Development

### Available Scripts
- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint
- `npm run type-check` - TypeScript type checking

### Code Quality
- **ESLint** - Code linting and formatting
- **TypeScript** - Static type checking
- **Prettier** - Code formatting
- **Husky** - Git hooks for pre-commit checks

## 📱 Responsive Design

- **Desktop First** - Optimized for desktop experience
- **Mobile Friendly** - Responsive layout for mobile devices
- **Tablet Support** - Adaptive layouts for tablets
- **Touch Interactions** - Mobile-optimized controls

## 🔒 Security Features

- **Authentication** - Secure user sessions
- **API Interceptors** - Automatic token handling
- **CORS Configuration** - Cross-origin security
- **Input Validation** - Client-side validation
- **XSS Prevention** - Safe HTML rendering

## 🚀 Performance Optimizations

- **Code Splitting** - Lazy loaded components
- **Image Optimization** - Efficient image handling
- **Bundle Optimization** - Minimal production bundle
- **Memoization** - React performance patterns
- **Virtual Scrolling** - Efficient large lists

## 🐛 Troubleshooting

### Common Issues

1. **CORS Errors**: Ensure backend CORS allows frontend origin
2. **SignalR Connection**: Check backend is running and accessible
3. **Authentication Issues**: Verify cookie/JWT configuration
4. **Build Errors**: Run `npm install` and check Node.js version

### Development Tips

- Use browser dev tools for debugging SignalR connections
- Check Network tab for API request/response details
- Monitor Redux state with Redux DevTools extension
- Test responsive design with browser device emulation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Built with ❤️ using modern web technologies**