# 🎨 UI Design System - Appointment App

## Overview
This application uses **Tailwind CSS** with custom components and utilities for a modern, professional, and user-friendly design.

## 🌈 Color Palette

### Primary Colors
- **Blue/Indigo**: Main brand color `from-blue-600 to-indigo-600`
- **Green/Emerald**: Success states `from-green-600 to-emerald-600`
- **Red/Rose**: Error states `from-red-600 to-rose-600`
- **Yellow/Amber**: Warning states `from-yellow-600 to-amber-600`

### Semantic Colors
- **Success**: Green shades (appointments confirmed)
- **Warning**: Yellow shades (pending states)
- **Error**: Red shades (cancelled, errors)
- **Info**: Blue shades (information)

## 🧱 Component Library

### Cards
```jsx
// Standard Card
<div className="card">Content</div>

// Interactive Card (with hover effect)
<div className="card-interactive">Content</div>

// Using Tailwind directly
<div className="bg-white rounded-2xl shadow-lg p-6 hover:shadow-xl transition-all">
  Content
</div>
```

### Buttons
```jsx
// Primary Button
<button className="btn-primary">
  <Icon className="w-5 h-5" />
  <span>Click Me</span>
</button>

// Secondary Button
<button className="btn-secondary">Cancel</button>

// Success Button
<button className="btn-success">Confirm</button>

// Danger Button
<button className="btn-danger">Delete</button>

// Using Tailwind directly
<button className="px-6 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-lg hover:from-blue-700 hover:to-indigo-700 transition-all shadow-md">
  Action
</button>
```

### Inputs
```jsx
// Standard Input
<input className="input" type="text" placeholder="Enter text..." />

// Error Input
<input className="input-error" type="text" />

// Using Tailwind directly
<input 
  className="w-full px-4 py-3 border-2 border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500" 
  type="text" 
/>
```

### Badges
```jsx
// Success Badge
<span className="badge-success">Available</span>

// Warning Badge
<span className="badge-warning">Pending</span>

// Danger Badge
<span className="badge-danger">Cancelled</span>

// Info Badge
<span className="badge-info">Scheduled</span>

// Using Tailwind directly
<span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
  Status
</span>
```

### Stat Cards
```jsx
<div className="stat-card">
  <div className="stat-icon bg-blue-100">
    <Icon className="w-6 h-6 text-blue-600" />
  </div>
  <div>
    <p className="text-gray-600">Label</p>
    <p className="text-3xl font-bold">123</p>
  </div>
</div>
```

## 🎭 Typography

### Font Family
- Primary: **Inter** (imported from Google Fonts)
- Fallback: system-ui, sans-serif

### Text Sizes (Tailwind classes)
- `text-xs`: 0.75rem
- `text-sm`: 0.875rem
- `text-base`: 1rem
- `text-lg`: 1.125rem
- `text-xl`: 1.25rem
- `text-2xl`: 1.5rem
- `text-3xl`: 1.875rem
- `text-4xl`: 2.25rem
- `text-5xl`: 3rem

### Font Weights
- `font-light`: 300
- `font-normal`: 400
- `font-medium`: 500
- `font-semibold`: 600
- `font-bold`: 700
- `font-extrabold`: 800

### Text Gradients
```jsx
<h1 className="text-gradient">Gradient Text</h1>
<h2 className="text-gradient-success">Success Gradient</h2>
```

## 📐 Layout Utilities

### Flexbox
```jsx
<div className="flex items-center justify-between space-x-4">
  <div>Item 1</div>
  <div>Item 2</div>
</div>
```

### Grid
```jsx
// Responsive Grid
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
  <div>Item 1</div>
  <div>Item 2</div>
  <div>Item 3</div>
</div>
```

### Spacing
- Padding: `p-{size}` (e.g., `p-6` = 1.5rem)
- Margin: `m-{size}` (e.g., `mt-4` = 1rem top margin)
- Gap: `gap-{size}` (e.g., `gap-4` = 1rem)
- Space: `space-x-{size}`, `space-y-{size}`

## 🎨 Special Effects

### Glass Morphism
```jsx
<div className="glass p-6 rounded-xl">
  Glassmorphic content
</div>

<div className="glass-dark p-6 rounded-xl text-white">
  Dark glassmorphic content
</div>
```

### Shadows
```jsx
<div className="shadow-sm">Small shadow</div>
<div className="shadow-md">Medium shadow</div>
<div className="shadow-lg">Large shadow</div>
<div className="shadow-xl">Extra large shadow</div>
<div className="shadow-2xl">2XL shadow</div>
```

### Border Radius
```jsx
<div className="rounded-sm">Small radius</div>
<div className="rounded-md">Medium radius</div>
<div className="rounded-lg">Large radius</div>
<div className="rounded-xl">Extra large radius</div>
<div className="rounded-2xl">2XL radius</div>
<div className="rounded-full">Full radius (circle/pill)</div>
```

## 🎬 Animations

### Available Animations
- `animate-fade-in`: Fade in effect
- `animate-fade-in-scale`: Fade in with scale
- `animate-slide-up`: Slide up from bottom
- `animate-slide-down`: Slide down from top
- `animate-spin`: Continuous spin (for loading spinners)
- `animate-pulse`: Pulsing effect
- `animate-bounce`: Bouncing effect

### Usage
```jsx
<div className="animate-fade-in">
  Fades in on mount
</div>

<div className="animate-fade-in-scale">
  Fades in and scales up
</div>

// Loading Spinner
<div className="w-6 h-6 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin" />
```

## 🎯 Transitions
```jsx
// Smooth transitions
<div className="transition-all duration-300 hover:scale-105">
  Hover me
</div>

// Custom transitions
<button className="transition-colors duration-200 hover:bg-blue-700">
  Color transition
</button>

<div className="transition-transform duration-300 hover:translate-y-1">
  Transform transition
</div>
```

## 📱 Responsive Design

### Breakpoints
- `sm`: 640px
- `md`: 768px
- `lg`: 1024px
- `xl`: 1280px
- `2xl`: 1536px

### Usage
```jsx
<div className="text-sm md:text-base lg:text-lg xl:text-xl">
  Responsive text
</div>

<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
  Responsive grid
</div>

<div className="p-4 md:p-6 lg:p-8">
  Responsive padding
</div>
```

## 🎪 Common Patterns

### Modal Overlay
```jsx
<div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50">
  <div className="bg-white rounded-2xl shadow-xl max-w-md w-full p-8 animate-fade-in-scale">
    Modal content
  </div>
</div>
```

### Loading State
```jsx
<div className="flex items-center justify-center space-x-2">
  <div className="w-5 h-5 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin" />
  <span>Loading...</span>
</div>
```

### Empty State
```jsx
<div className="text-center py-12">
  <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-gray-100 mb-4">
    <Icon className="w-8 h-8 text-gray-400" />
  </div>
  <h3 className="text-lg font-semibold text-gray-900 mb-2">No items found</h3>
  <p className="text-gray-600">Get started by creating your first item.</p>
</div>
```

### Table
```jsx
<div className="overflow-x-auto rounded-xl border border-gray-200">
  <table className="min-w-full divide-y divide-gray-200">
    <thead className="bg-gray-50">
      <tr>
        <th className="px-6 py-4 text-left text-sm font-semibold text-gray-900">
          Header
        </th>
      </tr>
    </thead>
    <tbody className="divide-y divide-gray-200 bg-white">
      <tr className="hover:bg-gray-50 transition-colors">
        <td className="px-6 py-4 text-sm text-gray-900">
          Cell
        </td>
      </tr>
    </tbody>
  </table>
</div>
```

## 🔍 Accessibility

### Focus States
All interactive elements have focus states:
```jsx
<button className="focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2">
  Accessible button
</button>
```

### Screen Reader Only
```jsx
<span className="sr-only">Screen reader only text</span>
```

## 💡 Best Practices

1. **Use Tailwind classes directly** for maximum flexibility
2. **Use custom component classes** (`.btn-primary`, `.card`, etc.) for consistency
3. **Always include hover states** for interactive elements
4. **Use transitions** for smooth state changes
5. **Make it responsive** - test on mobile, tablet, and desktop
6. **Maintain accessibility** - include focus states and ARIA labels
7. **Use semantic colors** - green for success, red for errors, etc.
8. **Keep spacing consistent** - use the spacing scale (4, 6, 8, etc.)

## 🚀 Quick Start Examples

### Dashboard Card
```jsx
<div className="bg-white rounded-2xl shadow-lg p-6 border border-gray-100 hover:shadow-xl transition-all">
  <div className="flex items-center justify-between mb-4">
    <h3 className="text-lg font-semibold text-gray-900">Total Appointments</h3>
    <div className="w-12 h-12 rounded-xl bg-blue-100 flex items-center justify-center">
      <Calendar className="w-6 h-6 text-blue-600" />
    </div>
  </div>
  <p className="text-3xl font-bold text-gray-900">156</p>
  <p className="text-sm text-gray-600 mt-1">↑ 12% from last month</p>
</div>
```

### Form Input with Icon
```jsx
<div className="relative">
  <User className="absolute left-4 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
  <input
    type="text"
    className="w-full pl-11 pr-4 py-3 border-2 border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
    placeholder="Enter your name"
  />
</div>
```

### Action Button with Icon
```jsx
<button className="px-6 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-semibold rounded-lg hover:from-blue-700 hover:to-indigo-700 transition-all shadow-md hover:shadow-lg flex items-center space-x-2">
  <Plus className="w-5 h-5" />
  <span>Add New</span>
</button>
```

---

**Happy Styling! 🎨**
