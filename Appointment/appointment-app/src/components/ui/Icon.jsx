import * as Icons from 'lucide-react'

const iconMap = {
  edit: 'Edit',
  heart: 'Heart',
  bell: 'Bell',
  settings: 'Settings',
  'help-circle': 'HelpCircle',
  'file-text': 'FileText',
  'log-out': 'LogOut',
  calendar: 'Calendar',
  clock: 'Clock',
  mapPin: 'MapPin',
  star: 'Star',
  check: 'Check',
  x: 'X',
  user: 'User',
  search: 'Search',
  filter: 'Filter',
  plus: 'Plus',
  edit3: 'Edit3',
  messageSquare: 'MessageSquare',
  notifications: 'Bell',
  menu: 'Menu',
  award: 'Award',
  dollarSign: 'DollarSign'
}

export const Icon = ({ name, size = 20, className = '', color = null }) => {
  const iconName = iconMap[name] || name
  const IconComponent = Icons[iconName]
  
  if (!IconComponent) {
    console.warn(`Icon "${name}" not found`)
    return null
  }

  const colorStyle = color ? { color } : {}
  
  return (
    <IconComponent 
      size={size} 
      className={className}
      style={colorStyle}
    />
  )
}

export const iconNames = iconMap
