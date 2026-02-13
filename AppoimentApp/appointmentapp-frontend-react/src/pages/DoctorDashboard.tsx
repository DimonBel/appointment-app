import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../components/shared/ToastContext';
import { Calendar, Clock, LogOut, RefreshCw, Shield, CheckCircle, XCircle } from 'lucide-react';
import apiService from '../services/apiService';
import type { Slot } from '../types/api';
import { useNavigate } from 'react-router-dom';
import { MOCK_SLOTS, USE_MOCK_DATA } from '../data/mockData';
import { Button } from '../components/ui/Button';
import { Card, CardContent } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { Avatar } from '../components/ui/Avatar';

const DoctorDashboard = () => {
  const { user, logout } = useAuth();
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  const [allSlots, setAllSlots] = useState<Slot[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [filter, setFilter] = useState<'all' | 'reserved' | 'available'>('all');
  const [confirm, setConfirm] = useState<{open: boolean; type: string; id: string}>({open: false, type: '', id: ''});

  useEffect(() => { loadAllSlots(); }, []);

  const loadAllSlots = async () => {
    try {
      setLoading(true);
      if (USE_MOCK_DATA) setAllSlots(MOCK_SLOTS);
      else setAllSlots(await apiService.getAllSlots());
    } catch { showError('Failed to load'); }
    finally { setLoading(false); }
  };

  const handleRefresh = async () => { setRefreshing(true); await loadAllSlots(); setRefreshing(false); };
  const handleLogout = () => { logout(); navigate('/login'); };

  const updateSlot = async (id: string, action: 'complete' | 'cancel') => {
    try {
      if (USE_MOCK_DATA) {
        const slot = MOCK_SLOTS.find(s => s.id === id);
        if (slot) slot.isReserved = false;
        await loadAllSlots();
        showSuccess(action === 'complete' ? 'Completed!' : 'Cancelled!');
      } else {
        if (action === 'complete') await apiService.completeAppointment(id);
        else await apiService.cancelAppointment(id);
        setAllSlots(allSlots.map(s => s.id === id ? {...s, isReserved: false} : s));
        showSuccess(action === 'complete' ? 'Completed!' : 'Cancelled!');
      }
      setConfirm({open: false, type: '', id: ''});
    } catch { showError('Failed'); }
  };

  const filtered = allSlots.filter(s => filter === 'all' || (filter === 'reserved' ? s.isReserved : !s.isReserved));
  const stats = { total: allSlots.length, reserved: allSlots.filter(s => s.isReserved).length, available: allSlots.filter(s => !s.isReserved).length };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-100 to-slate-200">
      {/* Header */}
      <header className="bg-white shadow-md">
        <div className="mx-auto px-6 py-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-lg bg-emerald-600 flex items-center justify-center">
                <Shield className="w-6 h-6 text-white" />
              </div>
              <span className="text-xl font-bold text-slate-800">Doctor Dashboard</span>
            </div>
            <div className="flex items-center gap-4">
              <span className="text-base text-slate-600 hidden sm:block">Dr. {user?.name}</span>
              <Avatar size="sm" fallback={user?.name?.charAt(0) || 'D'} />
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
            <h1 className="text-xl font-bold text-slate-900">Appointments</h1>
            <p className="text-sm text-slate-500">Manage your schedule</p>
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
              <div className="w-9 h-9 rounded-lg bg-slate-100 flex items-center justify-center">
                <Calendar className="w-4 h-4 text-slate-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Total</p>
                <p className="text-lg font-bold text-slate-900">{stats.total}</p>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-white">
            <CardContent className="p-3 flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-amber-100 flex items-center justify-center">
                <Clock className="w-4 h-4 text-amber-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Reserved</p>
                <p className="text-lg font-bold text-amber-600">{stats.reserved}</p>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-white">
            <CardContent className="p-3 flex items-center gap-3">
              <div className="w-9 h-9 rounded-lg bg-emerald-100 flex items-center justify-center">
                <CheckCircle className="w-4 h-4 text-emerald-600" />
              </div>
              <div>
                <p className="text-xs text-slate-500">Available</p>
                <p className="text-lg font-bold text-emerald-600">{stats.available}</p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filter */}
        <div className="flex gap-2 mb-4">
          {(['all', 'available', 'reserved'] as const).map(f => (
            <button key={f} onClick={() => setFilter(f)}
              className={`px-3 py-1.5 text-xs font-medium rounded ${filter === f ? 'bg-indigo-600 text-white' : 'bg-white text-slate-600 border border-slate-200'}`}>
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>

        {/* Grid */}
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {[1,2,3,4,5,6,7,8].map(i => <Card key={i} className="animate-pulse"><CardContent className="p-4"><div className="h-24 bg-slate-200 rounded"/></CardContent></Card>)}
          </div>
        ) : filtered.length === 0 ? (
          <Card><CardContent className="p-8 text-center"><p className="text-slate-500">No appointments found</p></CardContent></Card>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filtered.map(slot => {
              const date = new Date(slot.date);
              const isReserved = slot.isReserved;
              return (
                <Card key={slot.id} className={`bg-white ${isReserved ? 'ring-2 ring-amber-400' : ''}`}>
                  <CardContent className="p-0">
                    <div className="flex">
                      <div className={`w-16 rounded-l-lg flex flex-col items-center justify-center py-4 ${isReserved ? 'bg-amber-500' : 'bg-emerald-500'}`}>
                        <span className={`text-xs ${isReserved ? 'text-amber-200' : 'text-emerald-200'}`}>{date.toLocaleDateString('en-US', { month: 'short' })}</span>
                        <span className="text-2xl font-bold text-white">{date.getDate()}</span>
                        <span className={`text-xs ${isReserved ? 'text-amber-200' : 'text-emerald-200'}`}>{date.toLocaleDateString('en-US', { weekday: 'short' })}</span>
                      </div>
                      <div className="flex-1 p-3">
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <Avatar size="sm" fallback={slot.doctorName.charAt(0)} />
                            <span className="font-semibold text-slate-900 text-sm">Dr. {slot.doctorName}</span>
                          </div>
                          <Badge variant={isReserved ? 'warning' : 'success'}>{isReserved ? 'Reserved' : 'Available'}</Badge>
                        </div>
                        <div className="flex items-center justify-between mb-2">
                          <span className="text-xs text-slate-500 flex items-center gap-1">
                            <Clock className="w-3 h-3" />
                            {date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })}
                          </span>
                          <span className="text-lg font-bold text-slate-900">${slot.cost}</span>
                        </div>
                        {isReserved && (
                          <div className="flex gap-2">
                            <Button size="sm" className="flex-1 bg-emerald-600 h-7 text-xs" onClick={() => setConfirm({open: true, type: 'complete', id: slot.id})}>
                              <CheckCircle className="w-3 h-3 mr-1" />Done
                            </Button>
                            <Button size="sm" variant="outline" className="flex-1 h-7 text-xs border-red-200 text-red-600 hover:bg-red-50" onClick={() => setConfirm({open: true, type: 'cancel', id: slot.id})}>
                              <XCircle className="w-3 h-3 mr-1" />Cancel
                            </Button>
                          </div>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </main>

      {/* Confirm Modal */}
      {confirm.open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50">
          <Card className="w-full max-w-xs">
            <CardContent className="p-4 text-center">
              <div className={`w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-3 ${confirm.type === 'complete' ? 'bg-emerald-100' : 'bg-red-100'}`}>
                {confirm.type === 'complete' ? <CheckCircle className="w-6 h-6 text-emerald-600"/> : <XCircle className="w-6 h-6 text-red-600"/>}
              </div>
              <h3 className="font-semibold text-slate-900 mb-2">{confirm.type === 'complete' ? 'Complete?' : 'Cancel?'}</h3>
              <p className="text-xs text-slate-500 mb-3">{confirm.type === 'complete' ? 'Mark as completed' : 'Cancel this appointment'}</p>
              <div className="flex gap-2">
                <Button variant="outline" className="flex-1" onClick={() => setConfirm({open: false, type: '', id: ''})}>Back</Button>
                <Button className={`flex-1 ${confirm.type === 'complete' ? 'bg-emerald-600' : 'bg-red-600'}`} onClick={() => updateSlot(confirm.id, confirm.type as 'complete' | 'cancel')}>Confirm</Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
};

export default DoctorDashboard;
