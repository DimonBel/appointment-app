/**
 * Utility functions for normalizing and formatting specialty names
 */

/**
 * Maps specialty variations to consistent names
 */
const SPECIALTY_MAPPING = {
  // Dermatology variations
  'dermitologist': 'Dermatologist',
  'dermitology': 'Dermatology',
  'dermatologist': 'Dermatologist',
  'dermatology': 'Dermatology',
  
  // Cardiology variations
  'cardiologist': 'Cardiologist',
  'cardiology': 'Cardiology',
  
  // Pediatrics variations
  'pediatrician': 'Pediatrician',
  'pediatrics': 'Pediatrics',
  
  // Orthopedics variations
  'orthopedic': 'Orthopedics',
  'orthopedics': 'Orthopedics',
  'orthopedist': 'Orthopedist',
  
  // Neurology variations
  'neurologist': 'Neurologist',
  'neurology': 'Neurology',
  
  // Ophthalmology variations
  'ophthalmologist': 'Ophthalmologist',
  'ophthalmology': 'Ophthalmology',
  
  // ENT variations
  'ent': 'ENT Specialist',
  'otorhinolaryngologist': 'ENT Specialist',
  'otorhinolaryngology': 'ENT',
  
  // Gynecology variations
  'gynecologist': 'Gynecologist',
  'gynecology': 'Gynecology',
  
  // Other common specialties
  'general practitioner': 'General Practitioner',
  'general': 'General Practitioner',
  'family medicine': 'Family Medicine',
  'family doctor': 'Family Medicine',
  'internal medicine': 'Internal Medicine',
  'surgeon': 'Surgeon',
  'surgery': 'Surgery',
  'psychiatrist': 'Psychiatrist',
  'psychiatry': 'Psychiatry',
  'psychologist': 'Psychologist',
  'psychology': 'Psychology',
}

/**
 * Normalizes a specialty name to a consistent format
 * @param {string} specialty - The specialty name to normalize
 * @returns {string} The normalized specialty name
 */
export const normalizeSpecialty = (specialty) => {
  if (!specialty) return 'General Practitioner'
  
  const normalized = specialty.toLowerCase().trim()
  
  // Check if there's a direct mapping
  if (SPECIALTY_MAPPING[normalized]) {
    return SPECIALTY_MAPPING[normalized]
  }
  
  // If no mapping, capitalize the first letter of each word
  return specialty
    .toLowerCase()
    .split(' ')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}

/**
 * Standard specialty categories for display
 */
export const STANDARD_SPECIALTIES = [
  { id: 'dermatology', name: 'Dermatology', icon: '🩺', description: 'Skin, hair, and nail care' },
  { id: 'cardiology', name: 'Cardiology', icon: '❤️', description: 'Heart and cardiovascular health' },
  { id: 'pediatrics', name: 'Pediatrics', icon: '👶', description: 'Children\'s healthcare' },
  { id: 'orthopedics', name: 'Orthopedics', icon: '🦴', description: 'Bones, joints, and muscles' },
  { id: 'neurology', name: 'Neurology', icon: '🧠', description: 'Brain and nervous system' },
  { id: 'ophthalmology', name: 'Ophthalmology', icon: '👁️', description: 'Eye care and vision' },
  { id: 'ent', name: 'ENT Specialist', icon: '👂', description: 'Ear, nose, and throat' },
  { id: 'gynecology', name: 'Gynecology', icon: '🩺', description: 'Women\'s health' },
  { id: 'general', name: 'General Practice', icon: '👨‍⚕️', description: 'Primary healthcare' },
  { id: 'mental-health', name: 'Mental Health', icon: '🧘', description: 'Psychological wellness' },
]

/**
 * Gets specialty icon by name
 * @param {string} specialty - The specialty name
 * @returns {string} Emoji icon for the specialty
 */
export const getSpecialtyIcon = (specialty) => {
  const normalized = normalizeSpecialty(specialty).toLowerCase()
  const found = STANDARD_SPECIALTIES.find(s => 
    s.name.toLowerCase() === normalized || 
    s.id === normalized
  )
  return found ? found.icon : '👨‍⚕️'
}

/**
 * Gets specialty description by name
 * @param {string} specialty - The specialty name
 * @returns {string} Description of the specialty
 */
export const getSpecialtyDescription = (specialty) => {
  const normalized = normalizeSpecialty(specialty).toLowerCase()
  const found = STANDARD_SPECIALTIES.find(s => 
    s.name.toLowerCase() === normalized || 
    s.id === normalized
  )
  return found ? found.description : 'Healthcare services'
}