# Quick Start Guide

Get started with the Appointment App UI in 5 minutes!

## 📦 Installation

```bash
cd appointment-app
npm install
npm run dev
```

Visit `http://localhost:5173` to see your app!

## 🎯 What You Get

A complete appointment management UI with:

- ✅ **4 Pages**: Bookings, Doctor List, Profile, Settings
- ✅ **10+ Reusable Components**: Button, Card, Avatar, BookingCard, etc.
- ✅ **Responsive Design**: Works on desktop and mobile
- ✅ **Clean Code**: Well-organized and maintainable
- ✅ **Design System**: Consistent colors, typography, and spacing

## 🚀 Fast Track to Customization

### 1. Change the Theme Colors

Edit `src/utils/theme.js`:

```javascript
export const COLORS = {
  primary: {
    dark: '#1E2A38',      // Change this
    light: '#2C3E50',
    accent: '#4DA3FF'
  }
}
```

### 2. Add a New Page

```bash
# Create a new page
touch src/pages/NewPage.jsx
```

```jsx
// src/pages/NewPage.jsx
import { MainContent, Section, SectionHeader } from '../components/layout/MainContent'

export const NewPage = () => {
  return (
    <MainContent>
      <SectionHeader title="New Page" />
      <Section>
        <p>Your content here</p>
      </Section>
    </MainContent>
  )
}
```

```jsx
// Add route to src/App.jsx
import { NewPage } from './pages/NewPage'

<Route path="/new-page" element={<AppLayout><NewPage /></AppLayout>} />
```

### 3. Create a New Component

```bash
# Create a component
touch src/components/ui/NewComponent.jsx
```

```jsx
// src/components/ui/NewComponent.jsx
import React from 'react'

export const NewComponent = ({ title, children }) => {
  return (
    <div className="bg-white rounded-2xl shadow-sm p-6">
      <h3 className="text-[16px] font-semibold text-gray-900 mb-4">
        {title}
      </h3>
      {children}
    </div>
  )
}
```

```jsx
// Use it in your page
import { NewComponent } from '../components/ui/NewComponent'

<NewComponent title="My Component">
  <p>Your content</p>
</NewComponent>
```

### 4. Add Icons

```jsx
import { Icon } from '../components/ui/Icon'

// Available icons: calendar, bell, user, settings, etc.
<Icon name="calendar" size={20} />
```

## 📁 Project Structure

```
appointment-app/
├── src/
│   ├── components/
│   │   ├── ui/          # Reusable UI components
│   │   ├── layout/      # Layout components
│   │   └── booking/     # Booking-specific components
│   ├── pages/          # Page components
│   ├── utils/          # Utilities and config
│   ├── App.jsx         # Main app with routing
│   └── main.jsx        # Entry point
├── tailwind.config.js  # Tailwind config
└── README.md           # Full documentation
```

## 🎨 Design Tokens

All design system values are in `src/utils/theme.js`:

```javascript
export const COLORS = {
  primary: { dark: '#1E2A38', light: '#2C3E50', accent: '#4DA3FF' },
  background: { app: '#F2F2F2', content: '#FFFFFF' },
  text: { primary: '#1E1E1E', secondary: '#6B7280' }
}

export const SPACING = {
  small: '8px',
  medium: '16px',
  large: '24px'
}

export const TYPOGRAPHY = {
  pageTitle: '20px / SemiBold',
  body: '14px / Regular'
}
```

Change these values once and apply them everywhere!

## 🔌 Connect to Backend

See `BACKEND_INTEGRATION.md` for complete API integration guide.

Quick example:

```javascript
import { bookingsApi } from './utils/api'

// Fetch bookings
const appointments = await bookingsApi.getAll()

// Cancel booking
await bookingsApi.cancel(appointmentId)
```

## 🧪 Testing

```bash
# Run development server
npm run dev

# Create production build
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint
```

## 📚 Documentation

- **README.md** - Complete project documentation
- **COMPONENTS.md** - Detailed component reference
- **BACKEND_INTEGRATION.md** - API integration guide

## 🤝 Common Use Cases

### Show Only Upcoming Appointments

```jsx
const upcomingAppointments = appointments.filter(
  appointment => appointment.status === 'upcoming'
)
```

### Add Search to Doctor List

```jsx
const [searchQuery, setSearchQuery] = useState('')

const filteredDoctors = doctors.filter(
  doctor => doctor.name.toLowerCase().includes(searchQuery.toLowerCase())
)
```

### Create a Loading Skeleton

```jsx
{loading ? (
  <div className="space-y-4">
    {[1, 2, 3].map(i => <BookingCardSkeleton key={i} />)}
  </div>
) : (
  <BookingList appointments={appointments} loading={loading} />
)}
```

### Custom Tab Styles

```jsx
<BookingTabs 
  activeTab={activeTab}
  onTabChange={setActiveTab}
  className="justify-start" // Add Tailwind classes
/>
```

## 🎯 Next Steps

1. **Customize the theme** - Change colors and fonts
2. **Add your pages** - Create new routes and components
3. **Connect to API** - Integrate with your backend
4. **Add features** - Implement booking, search, filters, etc.
5. **Deploy** - Build for production with `npm run build`

## 🆘 Need Help?

- Check the full documentation in `README.md`
- See component examples in `COMPONENTS.md`
- Review backend integration guide in `BACKEND_INTEGRATION.md`

---

**Happy coding! 🚀**

Built with React + Vite + Tailwind CSS
