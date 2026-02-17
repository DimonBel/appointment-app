export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          dark: '#1E2A38',
          light: '#2C3E50',
          accent: '#4DA3FF'
        },
        background: {
          app: '#F2F2F2',
          content: '#FFFFFF',
          sidebar: '#FFFFFF',
          card: '#FFFFFF'
        },
        text: {
          primary: '#1E1E1E',
          secondary: '#6B7280',
          muted: '#9CA3AF'
        },
        button: {
          primary: '#1E2A38',
          secondary: '#E5E7EB'
        }
      },
      borderRadius: {
        '2xl': '1rem',
        '3xl': '1.5rem',
      },
      boxShadow: {
        'light': '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        'medium': '0 4px 12px rgba(0, 0, 0, 0.08)',
        'hover': '0 4px 12px rgba(0, 0, 0, 0.08) hover:shadow-lg transition-shadow duration-200'
      }
    },
  },
  plugins: [],
}
