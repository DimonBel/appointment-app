import * as Icons from 'lucide-react'

export const Icon = ({ name, size = 20, className = '', color = null }) => {
  const IconComponent = Icons[name]
  if (!IconComponent) {
    return null
  }

  const colorStyle = color ? { color } : {}
  
  return (
    <IconComponent 
      size={size} 
      className={className}
      {...colorStyle}
    />
  )
}

export const iconNames = {
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
  notifications: 'Bell'
}
