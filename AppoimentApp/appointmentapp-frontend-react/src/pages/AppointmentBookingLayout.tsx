import { useState } from 'react';
import { 
  Calendar, 
  Clock, 
  Users, 
  Settings, 
  LogOut, 
  Menu, 
  X, 
  Search,
  Bell,
  ChevronRight
} from 'lucide-react';

interface NavItem {
  id: string;
  label: string;
  icon: React.ElementType;
  badge?: number;
}

const AppointmentBookingLayout = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeNav, setActiveNav] = useState('dashboard');

  const navItems: NavItem[] = [
    { id: 'dashboard', label: 'Dashboard', icon: Calendar },
    { id: 'appointments', label: 'My Appointments', icon: Clock, badge: 3 },
    { id: 'doctors', label: 'Find Doctors', icon: Users },
    { id: 'settings', label: 'Settings', icon: Settings },
  ];

  const upcomingAppointments = [
    {
      id: 1,
      doctor: 'Dr. Sarah Johnson',
      specialty: 'Cardiologist',
      date: 'Feb 15, 2026',
      time: '10:00 AM',
      status: 'confirmed',
      avatar: 'SJ'
    },
    {
      id: 2,
      doctor: 'Dr. Michael Chen',
      specialty: 'Dermatologist',
      date: 'Feb 18, 2026',
      time: '2:30 PM',
      status: 'pending',
      avatar: 'MC'
    },
    {
      id: 3,
      doctor: 'Dr. Emily Rodriguez',
      specialty: 'Neurologist',
      date: 'Feb 22, 2026',
      time: '11:00 AM',
      status: 'confirmed',
      avatar: 'ER'
    }
  ];

  const availableSlots = [
    {
      id: 1,
      doctor: 'Dr. James Williams',
      specialty: 'Orthopedic Surgeon',
      date: 'Feb 14',
      day: 'Fri',
      slots: ['9:00 AM', '10:30 AM', '2:00 PM', '4:00 PM']
    },
    {
      id: 2,
      doctor: 'Dr. Olivia Martinez',
      specialty: 'Pediatrician',
      date: 'Feb 15',
      day: 'Sat',
      slots: ['8:30 AM', '11:00 AM', '1:00 PM', '3:30 PM']
    },
    {
      id: 3,
      doctor: 'Dr. David Kim',
      specialty: 'General Practitioner',
      date: 'Feb 16',
      day: 'Sun',
      slots: ['9:30 AM', '12:00 PM', '2:30 PM', '5:00 PM']
    }
  ];

  return (
    <div className="min-h-screen bg-slate-50 flex">
      {/* Sidebar */}
      <aside 
        className={`fixed lg:static inset-y-0 left-0 z-50 bg-white border-r border-slate-200 transition-all duration-300 ${
          sidebarOpen ? 'w-64' : 'w-20'
        }`}
      >
        {/* Logo */}
        <div className="h-16 flex items-center px-4 border-b border-slate-200">
          <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center flex-shrink-0">
            <Calendar className="w-5 h-5 text-white" />
          </div>
          {sidebarOpen && (
            <span className="ml-3 font-semibold text-slate-900 truncate">HealthBook</span>
          )}
        </div>

        {/* Navigation */}
        <nav className="p-4 space-y-1">
          {navItems.map((item) => (
            <button
              key={item.id}
              onClick={() => setActiveNav(item.id)}
              className={`w-full flex items-center px-3 py-2.5 rounded-lg transition-colors ${
                activeNav === item.id
                  ? 'bg-indigo-50 text-indigo-700'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
              }`}
            >
              <item.icon className="w-5 h-5 flex-shrink-0" />
              {sidebarOpen && (
                <>
                  <span className="ml-3 text-sm font-medium">{item.label}</span>
                  {item.badge && (
                    <span className="ml-auto bg-red-500 text-white text-xs px-2 py-0.5 rounded-full">
                      {item.badge}
                    </span>
                  )}
                </>
              )}
            </button>
          ))}
        </nav>

        {/* User Profile */}
        <div className="absolute bottom-0 left-0 right-0 p-4 border-t border-slate-200">
          <div className="flex items-center">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white font-medium flex-shrink-0">
              JD
            </div>
            {sidebarOpen && (
              <div className="ml-3 flex-1 min-w-0">
                <p className="text-sm font-medium text-slate-900 truncate">John Doe</p>
                <p className="text-xs text-slate-500 truncate">john@example.com</p>
              </div>
            )}
            {sidebarOpen && (
              <button className="text-slate-400 hover:text-slate-600">
                <LogOut className="w-5 h-5" />
              </button>
            )}
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="bg-white border-b border-slate-200 h-16 flex items-center justify-between px-4 lg:px-6">
          <div className="flex items-center gap-4">
            <button
              onClick={() => setSidebarOpen(!sidebarOpen)}
              className="p-2 rounded-lg hover:bg-slate-100 text-slate-600 lg:hidden"
            >
              {sidebarOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
            <button
              onClick={() => setSidebarOpen(!sidebarOpen)}
              className="p-2 rounded-lg hover:bg-slate-100 text-slate-600 hidden lg:block"
            >
              <Menu className="w-5 h-5" />
            </button>

            {/* Search */}
            <div className="hidden sm:flex items-center bg-slate-100 rounded-lg px-3 py-2 w-64">
              <Search className="w-4 h-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search doctors, appointments..."
                className="bg-transparent border-none outline-none text-sm ml-2 w-full"
              />
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button className="relative p-2 rounded-lg hover:bg-slate-100 text-slate-600">
              <Bell className="w-5 h-5" />
              <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
            </button>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 p-4 lg:p-6 overflow-auto">
          {/* Welcome Section */}
          <div className="mb-8">
            <h1 className="text-2xl font-semibold text-slate-900 mb-1">
              Welcome back, John!
            </h1>
            <p className="text-slate-500">
              Here's what's happening with your appointments today.
            </p>
          </div>

          {/* Stats Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Total Appointments</p>
                  <p className="text-2xl font-semibold text-slate-900 mt-1">24</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-indigo-50 flex items-center justify-center">
                  <Calendar className="w-5 h-5 text-indigo-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Upcoming</p>
                  <p className="text-2xl font-semibold text-green-600 mt-1">3</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
                  <Clock className="w-5 h-5 text-green-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Doctors Visited</p>
                  <p className="text-2xl font-semibold text-purple-600 mt-1">8</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-purple-50 flex items-center justify-center">
                  <Users className="w-5 h-5 text-purple-600" />
                </div>
              </div>
            </div>

            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Pending</p>
                  <p className="text-2xl font-semibold text-orange-600 mt-1">1</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-orange-50 flex items-center justify-center">
                  <Clock className="w-5 h-5 text-orange-600" />
                </div>
              </div>
            </div>
          </div>

          {/* Two Column Layout */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Upcoming Appointments */}
            <div className="bg-white rounded-xl border border-slate-200">
              <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
                <h2 className="font-semibold text-slate-900">Upcoming Appointments</h2>
                <button className="text-sm text-indigo-600 hover:text-indigo-700 font-medium">
                  View All
                </button>
              </div>
              <div className="p-4 space-y-3">
                {upcomingAppointments.map((apt) => (
                  <div
                    key={apt.id}
                    className="flex items-center gap-4 p-4 rounded-xl border border-slate-200 hover:border-indigo-300 hover:shadow-md transition-all cursor-pointer"
                  >
                    <div className="w-12 h-12 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white font-medium flex-shrink-0">
                      {apt.avatar}
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium text-slate-900 truncate">{apt.doctor}</h3>
                      <p className="text-sm text-slate-500">{apt.specialty}</p>
                      <div className="flex items-center gap-3 mt-1">
                        <span className="text-xs text-slate-600 flex items-center">
                          <Calendar className="w-3 h-3 mr-1" />
                          {apt.date}
                        </span>
                        <span className="text-xs text-slate-600 flex items-center">
                          <Clock className="w-3 h-3 mr-1" />
                          {apt.time}
                        </span>
                      </div>
                    </div>
                    <div className="flex-shrink-0">
                      <span
                        className={`text-xs font-medium px-2.5 py-1 rounded-full ${
                          apt.status === 'confirmed'
                            ? 'bg-green-100 text-green-700'
                            : 'bg-orange-100 text-orange-700'
                        }`}
                      >
                        {apt.status === 'confirmed' ? 'Confirmed' : 'Pending'}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Available Slots */}
            <div className="bg-white rounded-xl border border-slate-200">
              <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
                <h2 className="font-semibold text-slate-900">Available Slots</h2>
                <button className="text-sm text-indigo-600 hover:text-indigo-700 font-medium">
                  Book Now
                </button>
              </div>
              <div className="p-4 space-y-3">
                {availableSlots.map((slot) => (
                  <div
                    key={slot.id}
                    className="p-4 rounded-xl border border-slate-200 hover:border-indigo-300 hover:shadow-md transition-all"
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <h3 className="font-medium text-slate-900">{slot.doctor}</h3>
                        <p className="text-sm text-slate-500">{slot.specialty}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-lg font-semibold text-slate-900">{slot.date}</p>
                        <p className="text-xs text-slate-500">{slot.day}</p>
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {slot.slots.map((time) => (
                        <button
                          key={time}
                          className="px-3 py-1.5 text-sm border border-slate-300 rounded-lg hover:bg-indigo-50 hover:border-indigo-300 hover:text-indigo-700 transition-colors"
                        >
                          {time}
                        </button>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="mt-6 bg-gradient-to-r from-indigo-500 to-purple-600 rounded-xl p-6 text-white">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-lg font-semibold mb-1">Need to book a new appointment?</h2>
                <p className="text-indigo-100 text-sm">
                  Find the right doctor and book your appointment in minutes.
                </p>
              </div>
              <button className="flex items-center gap-2 bg-white text-indigo-600 px-5 py-2.5 rounded-lg font-medium hover:bg-indigo-50 transition-colors">
                Book Appointment
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </main>
      </div>

      {/* Mobile Sidebar Overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-slate-900/50 z-40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
    </div>
  );
};

export default AppointmentBookingLayout;