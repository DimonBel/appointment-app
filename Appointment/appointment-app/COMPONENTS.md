# UI Components Documentation

Complete reference for all UI components available in the Appointment App.

## Table of Contents

- [Base Components](#base-components)
  - [Avatar](#avatar)
  - [Button](#button)
  - [Card](#card)
  - [Icon](#icon)
- [Layout Components](#layout-components)
  - [Header](#header)
  - [Sidebar](#sidebar)
  - [MainContent](#maincontent)
- [Booking Components](#booking-components)
  - [BookingCard](#bookingcard)
  - [BookingTabs](#bookingtabs)
  - [BookingList](#bookinglist)
- [Profile Components](#profile-components)
  - [ProfileNav](#profilenav)

---

## Base Components

### Avatar

Display avatars with size and group support.

**Props:**
- `src` (string, optional) - Image URL for avatar
- `alt` (string, optional) - Alt text for accessibility
- `size` (number, optional) - Avatar size in pixels (default: 56)
- `className` (string, optional) - Additional CSS classes

**Variations:**
```jsx
<Avatar src="user.jpg" alt="John Doe" size={56} />

// Large avatar
<Avatar src="user.jpg" alt="John Doe" size={80} />

// Circular variant
<div className="rounded-full">
  <Avatar src="user.jpg" alt="John Doe" size={40} />
</div>
```

**Usage with Group:**
```jsx
import { AvatarGroup } from './components/ui/Avatar'

<AvatarGroup
  avatars={[
    { src: 'avatar1.jpg', alt: 'User 1' },
    { src: 'avatar2.jpg', alt: 'User 2' },
    { src: 'avatar3.jpg', alt: 'User 3' },
  ]}
  size={40}
  max={3}
/>
```

**Related Props:**
- `bgColor` (string, optional) - Background color (fallback when no image)
- `showInitials` (boolean, optional) - Show initials instead of image (default: false)

---

### Button

Reusable button component with multiple variants and sizes.

**Props:**
- `children` (ReactNode, required) - Button text or content
- `variant` (string, optional) - Button style variant (default: 'primary')
  - `'primary'` - Primary button (dark background)
  - `'secondary'` - Secondary button (light background)
  - `'ghost'` - Ghost button (transparent, no background)
- `size` (string, optional) - Button size (default: 'medium')
  - `'small'` - 32px height
  - `'medium'` - 40px height
  - `'large'` - 48px height
- `className` (string, optional) - Additional CSS classes
- `onClick` (function, optional) - Click handler
- `type` (string, optional) - HTML button type (default: 'button')
  - `'button'`, `'submit'`, `'reset'`
- `disabled` (boolean, optional) - Disabled state (default: false)
- `icon` (string, optional) - Icon name from Lucide React
- `loading` (boolean, optional) - Loading state (default: false)

**Variants:**
```jsx
<Button variant="primary">Primary</Button>
<Button variant="secondary">Secondary</Button>
<Button variant="ghost">Ghost</Button>
```

**Sizes:**
```jsx
<Button size="small">Small</Button>
<Button size="medium">Medium</Button>
<Button size="large">Large</Button>
```

**With Icons:**
```jsx
<Button icon="plus">Add Item</Button>
<Button variant="secondary" icon="edit">Edit</Button>
```

**Loading State:**
```jsx
<Button loading onClick={handleClick}>Save</Button>
```

**Button Group:**
```jsx
import { ButtonGroup } from './components/ui/Button'

<ButtonGroup>
  <Button variant="primary">Save</Button>
  <Button variant="secondary">Cancel</Button>
</ButtonGroup>
```

---

### Card

Container component for content with elevation and sub-components.

**Props:**
- `children` (ReactNode, optional) - Card content
- `className` (string, optional) - Additional CSS classes
- `elevation` (string, optional) - Shadow elevation (default: 'medium')
  - `'none'` - No shadow
  - `'light'` - Light shadow
  - `'medium'` - Medium shadow (default)
  - `'hover'` - Hover effect

**Sub-components:**

#### CardHeader
```jsx
import { CardHeader } from './components/ui/Card'

<CardHeader className="p-4">
  <CardTitle>Title</CardTitle>
  <CardSubtitle>Subtitle</CardSubtitle>
</CardHeader>
```

**CardHeader Props:**
- `children` (ReactNode, optional) - Header content
- `className` (string, optional) - Additional CSS classes

#### CardBody
```jsx
import { CardBody } from './components/ui/Card'

<CardBody>
  <p>Content goes here</p>
</CardBody>
```

**CardBody Props:**
- `children` (ReactNode, optional) - Body content
- `className` (string, optional) - Additional CSS classes

#### CardFooter
```jsx
import { CardFooter } from './components/ui/Card'

<CardFooter>
  <Button>Action</Button>
</CardFooter>
```

**CardFooter Props:**
- `children` (ReactNode, optional) - Footer content
- `className` (string, optional) - Additional CSS classes

#### CardTitle
```jsx
import { CardTitle } from './components/ui/Card'

<CardTitle>Title</CardTitle>
```

**CardTitle Props:**
- `children` (ReactNode, optional) - Title content
- `className` (string, optional) - Additional CSS classes

#### CardSubtitle
```jsx
import { CardSubtitle } from './components/ui/Card'

<CardSubtitle>Subtitle</CardSubtitle>
```

**CardSubtitle Props:**
- `children` (ReactNode, optional) - Subtitle content
- `className` (string, optional) - Additional CSS classes

**Complete Usage:**
```jsx
<Card elevation="medium">
  <CardHeader>
    <CardTitle>Card Title</CardTitle>
    <CardSubtitle>Card subtitle description</CardSubtitle>
  </CardHeader>
  <CardBody>
    <p>Card content goes here</p>
  </CardBody>
  <CardFooter>
    <Button>Action Button</Button>
  </CardFooter>
</Card>
```

---

### Icon

Wrapper component for Lucide React icons.

**Props:**
- `name` (string, required) - Icon name from Lucide React
- `size` (number, optional) - Icon size in pixels (default: 20)
- `className` (string, optional) - Additional CSS classes
- `color` (string, optional) - Custom color (overrides default)

**Available Icons:**
- `'calendar'` - Calendar icon
- `'bell'` - Bell icon
- `'settings'` - Settings icon
- `'user'` - User icon
- `'edit'` - Edit icon
- `'heart'` - Heart icon
- `'search'` - Search icon
- `'filter'` - Filter icon
- `'plus'` - Plus icon
- `'edit3'` - Edit3 icon
- `'message-square'` - Message icon
- `'log-out'` - Log out icon
- `'mapPin'` - Location pin
- `'star'` - Star icon
- `'check'` - Check icon
- `'x'` - Close icon
- `'chevron-right'` - Chevron right
- `'clock'` - Clock icon

**Usage:**
```jsx
<Icon name="calendar" size={20} />
<Icon name="bell" size={24} color="#4DA3FF" />
<Icon name="settings" className="mr-2" />
```

**Available Names:** All names from Lucide React

---

## Layout Components

### Header

Top navigation header with logo, notifications, and user profile.

**Props:**
- `onMenuClick` (function, optional) - Callback when menu button is clicked
- `className` (string, optional) - Additional CSS classes

**Features:**
- Sticky header with blur effect
- Notification badge
- User avatar with dropdown support
- Responsive design

**Usage:**
```jsx
import { Header } from './components/layout/Header'

<Header onMenuClick={() => setMenuOpen(!menuOpen)} />
```

**Responsive Behavior:**
- Shows notification badge when user has notifications
- Hides user email on small screens
- Collapsible menu on mobile

---

### Sidebar

Left sidebar navigation with active state tracking.

**Props:**
- `activeItem` (string, optional) - Currently active navigation item (default: 'bookings')
- `onNavigate` (function, optional) - Callback when navigation item is clicked
- `className` (string, optional) - Additional CSS classes

**Features:**
- Active state highlighting
- Hover effects
- Icon integration
- Smooth transitions

**Navigation Items:**
```javascript
{
  id: 'bookings',
  label: 'My Bookings',
  icon: 'calendar'
},
{
  id: 'doctors',
  label: 'Find Doctors',
  icon: 'users'
},
{
  id: 'profile',
  label: 'Profile',
  icon: 'user'
},
{
  id: 'settings',
  label: 'Settings',
  icon: 'settings'
}
```

**Usage:**
```jsx
<Sidebar 
  activeItem="bookings"
  onNavigate={(itemId) => console.log('Navigate to:', itemId)}
/>
```

**Custom Items:**
```jsx
<Sidebar
  activeItem="custom"
  onNavigate={handleNavigate}
  customItems={[
    { id: 'custom', label: 'Custom', icon: 'star' }
  ]}
/>
```

---

### MainContent

Main content area wrapper with spacing and scroll support.

**Props:**
- `children` (ReactNode, optional) - Content to render
- `className` (string, optional) - Additional CSS classes

**Features:**
- Proper spacing for content
- Scrollable area
- Max-width container
- Responsive width

**Usage:**
```jsx
import { MainContent } from './components/layout/MainContent'

<MainContent>
  <div className="p-6">
    <h1>Content</h1>
    {/* Content goes here */}
  </div>
</MainContent>
```

**With Section Helpers:**
```jsx
import { MainContent, Section, SectionHeader } from './components/layout/MainContent'

<MainContent>
  <SectionHeader 
    title="My Bookings"
    subtitle="Manage your appointments"
  />
  <Section>
    {/* Content */}
  </Section>
</MainContent>
```

**Section Props:**
- `children` (ReactNode, optional) - Section content
- `className` (string, optional) - Additional CSS classes

**SectionHeader Props:**
- `title` (string, optional) - Section title
- `subtitle` (string, optional) - Section subtitle
- `className` (string, optional) - Additional CSS classes

---

## Booking Components

### BookingCard

Individual appointment card with doctor information and actions.

**Props:**
- `doctorName` (string, required) - Doctor's full name
- `specialty` (string, required) - Doctor's specialty
- `clinic` (string, required) - Clinic or hospital name
- `location` (string, required) - Clinic location/address
- `date` (string, required) - Appointment date (YYYY-MM-DD)
- `time` (string, required) - Appointment time
- `status` (string, required) - Booking status
  - `'upcoming'` - Upcoming appointment
  - `'completed'` - Completed appointment
  - `'canceled'` - Canceled appointment
- `avatar` (string, optional) - Doctor's avatar URL
- `onReschedule` (function, optional) - Callback when reschedule button is clicked
- `onCancel` (function, optional) - Callback when cancel button is clicked

**Status Colors:**
- `'upcoming'` - Primary dark background with white text
- `'completed'` - Green background with dark green text
- `'canceled'` - Red background with dark red text

**Usage:**
```jsx
<BookingCard
  doctorName="Dr. Sarah Johnson"
  specialty="Cardiologist"
  clinic="Heart Care Center"
  location="123 Medical Ave, New York"
  date="2026-02-15"
  time="10:00 AM"
  status="upcoming"
  avatar="avatar.jpg"
  onReschedule={handleReschedule}
  onCancel={handleCancel}
/>
```

**Loading State:**
```jsx
import { BookingCardSkeleton } from './components/booking/BookingCard'

<BookingCardSkeleton />
```

**Layout:**
- Doctor avatar (left)
- Doctor name and specialty (center)
- Date, time, and status (right)
- Action buttons (bottom for upcoming bookings)

---

### BookingTabs

Tab navigation for booking categories.

**Props:**
- `activeTab` (string, required) - Currently active tab
- `onTabChange` (function, required) - Callback when tab is changed
- `className` (string, optional) - Additional CSS classes

**Tabs:**
- `'upcoming'` - Upcoming appointments
- `'completed'` - Completed appointments
- `'canceled'` - Canceled appointments

**Features:**
- Active state with underline indicator
- Hover effects
- Smooth transitions
- Responsive text size

**Usage:**
```jsx
<BookingTabs 
  activeTab={activeTab}
  onTabChange={setActiveTab}
/>
```

**Custom Tabs:**
```jsx
<BookingTabs
  activeTab={activeTab}
  onTabChange={setActiveTab}
  tabs={[
    { id: 'custom1', label: 'Custom Tab 1' },
    { id: 'custom2', label: 'Custom Tab 2' }
  ]}
/>
```

---

### BookingList

Container for booking cards with loading and empty states.

**Props:**
- `appointments` (Array, optional) - Array of appointment objects
- `loading` (boolean, optional) - Loading state (default: false)
- `onViewDetails` (function, optional) - Callback when card is clicked
- `onReschedule` (function, optional) - Callback when reschedule button is clicked
- `onCancel` (function, optional) - Callback when cancel button is clicked

**Features:**
- Loading skeleton animation
- Empty state with illustration
- Error handling
- Responsive layout

**Loading State:**
- Shows 3 skeleton cards with shimmer effect
- Preserves scroll position

**Empty State:**
- Calendar icon
- "No appointments found" message
- Subtext about no bookings in category

**Usage:**
```jsx
<BookingList
  appointments={appointments}
  loading={loading}
  onViewDetails={handleViewDetails}
  onReschedule={handleReschedule}
  onCancel={handleCancel}
/>
```

**Required Props for Cards:**
```javascript
interface Appointment {
  id: number;
  doctorName: string;
  specialty: string;
  clinic: string;
  location: string;
  date: string;
  time: string;
  status: 'upcoming' | 'completed' | 'canceled';
  avatar?: string;
}
```

---

## Profile Components

### ProfileNav

Profile navigation menu (extends Sidebar with profile-specific items).

**Props:**
- `activeItem` (string, optional) - Currently active item
- `onNavigate` (function, optional) - Navigation callback
- `className` (string, optional) - Additional CSS classes

**Features:**
- Profile-specific navigation items
- Favorites management
- Recent activity tracking

**Usage:**
```jsx
import { ProfileNav } from './components/profile/ProfileNav'

<ProfileNav
  activeItem="edit-profile"
  onNavigate={(itemId) => console.log('Navigate:', itemId)}
/>
```

**Navigation Items:**
- `'edit-profile'` - Edit profile
- `'favorite'` - Favorites
- `'notifications'` - Notifications
- `'settings'` - Settings
- `'help'` - Help and support
- `'terms'` - Terms and conditions
- `'logout'` - Log out

---

## Common Props Reference

### Size Variants

#### Button Sizes
- `small`: 32px height, 16px padding
- `medium`: 40px height, 20px padding
- `large`: 48px height, 24px padding

#### Avatar Sizes
- `32`: Small avatar (32px)
- `40`: Medium avatar (40px)
- `56`: Large avatar (56px)
- `80`: Extra large avatar (80px)

#### Icon Sizes
- `16`: Small icons (16px)
- `18`: Medium icons (18px)
- `20`: Default icons (20px)
- `24`: Large icons (24px)

### Color Palettes

#### Primary Colors
- `#1E2A38` - Primary dark (primary buttons, headers)
- `#2C3E50` - Primary light (hover states)
- `#4DA3FF` - Accent (icons, highlights)

#### Background Colors
- `#F2F2F2` - App background
- `#FFFFFF` - Content background

#### Text Colors
- `#1E1E1E` - Primary text
- `#6B7280` - Secondary text
- `#9CA3AF` - Muted text

### Spacing Values

- `4px` - Extra small
- `8px` - Small
- `12px` - Medium small
- `16px` - Medium (default padding)
- `24px` - Large
- `32px` - Extra large

---

## Accessibility

All components include:
- Proper ARIA labels
- Keyboard navigation support
- Focus states
- Semantic HTML
- Screen reader support

### Keyboard Navigation

- Tab: Navigate through interactive elements
- Enter/Space: Activate buttons and links
- Escape: Close modals and dropdowns
- Arrow keys: Navigate within lists

### Screen Reader Support

- Proper heading hierarchy
- Alt text for images
- ARIA labels for icons
- Live regions for dynamic content

---

## Performance

- Memoized components where appropriate
- Efficient re-rendering
- Lazy loading where needed
- Code splitting support

---

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

---

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)
