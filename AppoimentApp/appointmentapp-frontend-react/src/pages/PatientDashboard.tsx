import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../components/shared/ToastContext';
import { Calendar, Clock, LogOut, RefreshCw, Stethoscope } from 'lucide-react';
import apiService from '../services/apiService';
import type { Slot, AppointmentBooking } from '../types/api';
import { useNavigate } from 'react-router-dom';
import { MOCK_SLOTS, USE_MOCK_DATA } from '../data/mockData';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Card, CardContent } from '../components/ui/Card';
import { Avatar } from '../components/ui/Avatar';

const PatientDashboard = () => {
  const { user, logout } = useAuth();
  const { showSuccess, showError, showInfo } = useToast();
  const navigate = useNavigate();
  const [availableSlots, setAvailableSlots] = useState<Slot[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);
  const [showBookingModal, setShowBookingModal] = useState(false);
  const [patientName, setPatientName] = useState(user?.name || '');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadAvailableSlots();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadAvailableSlots = async () => {
    try {
      if (!refreshing) setLoading(true);
      
      if (USE_MOCK_DATA) {
        await new Promise(resolve => setTimeout(resolve, 500));
        setAvailableSlots(MOCK_SLOTS);
        showInfo('Using demo data');
      } else {
        try {
          const slots = await apiService.getAvailableSlots();
          setAvailableSlots(slots);
        } catch {
          setAvailableSlots(MOCK_SLOTS);
          showInfo('Demo mode');
        }
      }
    } catch (error) {
      console.error('Error loading slots:', error);
      showError('Failed to load slots');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    setRefreshing(true);
    loadAvailableSlots();
  };

  const handleBookAppointment = async () => {
    if (!selectedSlot || !patientName.trim()) {
      showError('Please enter your name');
      return;
    }

    try {
      setSubmitting(true);
      const booking: Omit<AppointmentBooking, 'id'> = {
        reservedAt: new Date().toISOString(),
        slotId: selectedSlot.id,
        patientId: user?.id || crypto.randomUUID(),
        patientName: patientName.trim(),
        doctorName: selectedSlot.doctorName
      };

      if (USE_MOCK_DATA) {
        await new Promise(resolve => setTimeout(resolve, 500));
        const slot = MOCK_SLOTS.find(s => s.id === selectedSlot.id);
        if (slot) slot.isReserved = true;
        await loadAvailableSlots();
        showSuccess('Booked successfully!');
      } else {
        await apiService.bookAppointment(booking);
        await loadAvailableSlots();
        showSuccess('Booked successfully!');
      }
      setShowBookingModal(false);
      setSelectedSlot(null);
      setPatientName(user?.name || '');
    } catch (error) {
      showError('Booking failed');
    } finally {
      setSubmitting(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const formatTime = (time: string) => {
    return new Date(`2000-01-01T${time}`).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-100 to-slate-200">
      {/* Header */}
      <header className="bg-white shadow-md">
        <div className="mx-auto px-6 py-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-lg bg-indigo-600 flex items-center justify-center">
                <Stethoscope className="w-6 h-6 text-white" />
              </div>
              <span className="text-xl font-bold text-slate-800">HealthCare</span>
            </div>
            <div className="flex items-center gap-4">
              <span className="text-base text-slate-600 hidden sm:block">{user?.name}</span>
              <Avatar size="sm" fallback={user?.name?.charAt(0) || 'U'} />
              <Button variant="ghost" size="sm" onClick={handleLogout} className="text-slate-500">
                <LogOut className="w-5 h-5" />
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main */}
      <main className="mx-auto px-6 py-6">
        {/* Title & Refresh */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-xl font-bold text-slate-900">Find a Doctor</h1>
            <p className="text-sm text-slate-500">Book your appointment</p>
          </div>
          <Button variant="outline" size="sm" onClick={handleRefresh} disabled={refreshing}>
            <RefreshCw className={`w-4 h-4 mr-1 ${refreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          <Card className="bg-white">
            <CardContent className="p-3 flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-indigo-100 flex items-center justify-center">
                <Calendar className="w-4 h-4 text-indigo-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Slots</p>
                <p className="text-lg font-bold text-slate-900">{availableSlots.length}</p>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-white">
            <CardContent className="p-3 flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-emerald-100 flex items-center justify-center">
                <Stethoscope className="w-4 h-4 text-emerald-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Doctors</p>
                <p className="text-lg font-bold text-slate-900">{new Set(availableSlots.map(s => s.doctorName)).size}</p>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-white">
            <CardContent className="p-3 flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-amber-100 flex items-center justify-center">
                <Clock className="w-4 h-4 text-amber-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Today</p>
                <p className="text-lg font-bold text-slate-900">
                  {availableSlots.filter(s => new Date(s.date).toDateString() === new Date().toDateString()).length}
                </p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Grid */}
        <h2 className="text-base font-semibold text-slate-900 mb-3">Available Appointments</h2>
        
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {[1,2,3,4,5,6,7,8].map(i => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-4"><div className="h-24 bg-slate-200 rounded"/></CardContent>
              </Card>
            ))}
          </div>
        ) : availableSlots.length === 0 ? (
          <Card><CardContent className="p-8 text-center"><p className="text-slate-500">No slots available</p></CardContent></Card>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {availableSlots.map(slot => {
              const date = new Date(slot.date);
              return (
                <Card 
                  key={slot.id} 
                  className="hover:shadow-md transition-shadow cursor-pointer bg-white"
                  onClick={() => { setSelectedSlot(slot); setShowBookingModal(true); }}
                >
                  <CardContent className="p-0">
                    <div className="flex">
                      {/* Date Box */}
                      <div className="w-16 bg-indigo-600 rounded-l-lg flex flex-col items-center justify-center py-4">
                        <span className="text-xs text-indigo-200 font-medium">{date.toLocaleDateString('en-US', { month: 'short' })}</span>
                        <span className="text-2xl font-bold text-white">{date.getDate()}</span>
                        <span className="text-xs text-indigo-200">{date.toLocaleDateString('en-US', { weekday: 'short' })}</span>
                      </div>
                      
                      {/* Info */}
                      <div className="flex-1 p-3">
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <Avatar size="sm" fallback={slot.doctorName.charAt(0)} />
                            <span className="font-semibold text-slate-900 text-sm">Dr. {slot.doctorName}</span>
                          </div>
                          <span className="text-lg font-bold text-emerald-600">${slot.cost}</span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-xs text-slate-500 flex items-center gap-1">
                            <Clock className="w-3 h-3" />
                            {date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })}
                          </span>
                          <Button 
                            size="sm" 
                            className="bg-indigo-600 hover:bg-indigo-700 h-7 text-xs px-3"
                            onClick={(e) => { e.stopPropagation(); setSelectedSlot(slot); setShowBookingModal(true); }}
                          >
                            Book
                          </Button>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </main>

      {/* Modal */}
      {showBookingModal && selectedSlot && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50" onClick={() => !submitting && setShowBookingModal(false)}>
          <Card className="w-full max-w-sm" onClick={e => e.stopPropagation()}>
            <CardContent className="p-4">
              <h3 className="text-lg font-bold text-slate-900 mb-3">Confirm Booking</h3>
              
              <div className="space-y-2 mb-4">
                <div className="p-2 bg-slate-50 rounded flex justify-between text-sm">
                  <span className="text-slate-500">Doctor</span>
                  <span className="font-medium">Dr. {selectedSlot.doctorName}</span>
                </div>
                <div className="p-2 bg-slate-50 rounded flex justify-between text-sm">
                  <span className="text-slate-500">Date</span>
                  <span className="font-medium">{formatDate(selectedSlot.date)}</span>
                </div>
                <div className="p-2 bg-slate-50 rounded flex justify-between text-sm">
                  <span className="text-slate-500">Time</span>
                  <span className="font-medium">{formatTime(selectedSlot.date)}</span>
                </div>
                <div className="p-2 bg-slate-50 rounded flex justify-between text-sm">
                  <span className="text-slate-500">Fee</span>
                  <span className="font-bold text-lg">${selectedSlot.cost}</span>
                </div>
              </div>

              <Input
                value={patientName}
                onChange={e => setPatientName(e.target.value)}
                placeholder="Your name"
                className="mb-3"
                disabled={submitting}
              />

              <div className="flex gap-2">
                <Button variant="outline" className="flex-1" onClick={() => setShowBookingModal(false)} disabled={submitting}>Cancel</Button>
                <Button className="flex-1 bg-indigo-600" onClick={handleBookAppointment} disabled={submitting || !patientName.trim()}>
                  {submitting ? 'Booking...' : 'Confirm'}
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
};

export default PatientDashboard;
