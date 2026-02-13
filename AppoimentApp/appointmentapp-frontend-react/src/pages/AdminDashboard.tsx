import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../components/shared/ToastContext';
import { Calendar, Clock, DollarSign, LogOut, RefreshCw, Shield, Plus } from 'lucide-react';
import apiService from '../services/apiService';
import type { Slot } from '../types/api';
import { useNavigate } from 'react-router-dom';
import { MOCK_SLOTS, USE_MOCK_DATA } from '../data/mockData';

const AdminDashboard = () => {
  const { user, logout } = useAuth();
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  const [allSlots, setAllSlots] = useState<Slot[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [newSlot, setNewSlot] = useState({
    date: '',
    time: '09:00',
    doctorName: '',
    cost: ''
  });
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    loadAllSlots();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadAllSlots = async () => {
    try {
      setLoading(true);
      if (USE_MOCK_DATA) {
        setAllSlots(MOCK_SLOTS);
      } else {
        const slots = await apiService.getAllSlots();
        setAllSlots(slots);
      }
    } catch (error) {
      console.error('Error loading slots:', error);
      if (!USE_MOCK_DATA) {
        showError('Failed to load slots');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = async () => {
    if (refreshing) return;
    try {
      setRefreshing(true);
      await loadAllSlots();
      showSuccess('Data refreshed successfully');
    } catch (error) {
      showError('Failed to refresh data');
    } finally {
      setRefreshing(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleAddSlot = async () => {
    // Validation
    const newErrors: { [key: string]: string } = {};
    if (!newSlot.doctorName.trim()) {
      newErrors.doctorName = 'Doctor name is required';
    }
    if (!newSlot.date) {
      newErrors.date = 'Date is required';
    }
    if (!newSlot.time) {
      newErrors.time = 'Time is required';
    }
    if (!newSlot.cost || parseFloat(newSlot.cost) < 0) {
      newErrors.cost = 'Valid cost is required';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    try {
      setSubmitting(true);
      const slotData = {
        date: `${newSlot.date}T${newSlot.time}`,
        doctorId: 'admin',
        doctorName: newSlot.doctorName,
        cost: parseFloat(newSlot.cost)
      };

      if (USE_MOCK_DATA) {
        const newId = (Math.max(...MOCK_SLOTS.map(s => parseInt(s.id))) + 1).toString();
        const createdSlot: Slot = {
          id: newId,
          ...slotData,
          isReserved: false
        };
        MOCK_SLOTS.push(createdSlot);
        setAllSlots([...MOCK_SLOTS]);
        showSuccess('Slot created successfully');
      } else {
        await apiService.addSlot(slotData);
        await loadAllSlots();
        showSuccess('Slot created successfully');
      }

      setShowAddModal(false);
      setNewSlot({ date: '', time: '09:00', doctorName: '', cost: '' });
      setErrors({});
    } catch (error) {
      console.error('Error adding slot:', error);
      showError('Failed to create slot');
    } finally {
      setSubmitting(false);
    }
  };

  const formatTime = (time: string) => {
    return new Date(`2000-01-01T${time}`).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  const stats = {
    total: allSlots.length,
    reserved: allSlots.filter(s => s.isReserved).length,
    available: allSlots.filter(s => !s.isReserved).length,
    revenue: allSlots
      .filter(s => s.isReserved)
      .reduce((sum, s) => sum + s.cost, 0)
      .toFixed(2)
  };

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-40">
        <div className="max-w-6xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-slate-900 flex items-center justify-center">
                <Shield className="w-5 h-5 text-white" />
              </div>
              <div>
                <h1 className="text-lg font-semibold text-slate-900">Admin Dashboard</h1>
                <p className="text-xs text-slate-500">Welcome, <span className="font-medium">{user?.name}</span></p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowAddModal(true)}
                className="px-3 py-2 bg-slate-900 text-white text-sm font-medium rounded-lg hover:bg-slate-800 transition-colors"
              >
                <Plus className="w-4 h-4 mr-1.5" />
                <span className="hidden sm:inline">Add Slot</span>
              </button>
              <button
                onClick={handleRefresh}
                disabled={refreshing || loading}
                className="px-3 py-2 text-sm text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <RefreshCw className={`w-4 h-4 mr-1.5 ${refreshing ? 'animate-spin' : ''}`} />
                <span className="hidden sm:inline">Refresh</span>
              </button>
              <button
                onClick={handleLogout}
                className="px-3 py-2 text-sm text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-lg transition-colors"
              >
                <LogOut className="w-4 h-4 mr-1.5" />
                <span className="hidden sm:inline">Logout</span>
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-6xl mx-auto px-6 py-8 pb-16">
        {/* Stats Cards */}
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="bg-white rounded-xl border border-slate-200 p-5">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="h-3 bg-slate-200 rounded w-20 mb-2 animate-pulse" />
                    <div className="h-7 bg-slate-200 rounded w-12 animate-pulse" />
                  </div>
                  <div className="w-10 h-10 bg-slate-200 rounded-lg animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            {/* Total Slots */}
            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Total Slots</p>
                  <p className="text-2xl font-semibold text-slate-900 mt-1">{stats.total}</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center">
                  <Calendar className="w-5 h-5 text-slate-500" />
                </div>
              </div>
            </div>

            {/* Reserved */}
            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Reserved</p>
                  <p className="text-2xl font-semibold text-orange-600 mt-1">{stats.reserved}</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-orange-50 flex items-center justify-center">
                  <Clock className="w-5 h-5 text-orange-500" />
                </div>
              </div>
            </div>

            {/* Available */}
            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Available</p>
                  <p className="text-2xl font-semibold text-green-600 mt-1">{stats.available}</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
                  <Calendar className="w-5 h-5 text-green-500" />
                </div>
              </div>
            </div>

            {/* Revenue */}
            <div className="bg-white rounded-xl border border-slate-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">Revenue</p>
                  <p className="text-2xl font-semibold text-slate-900 mt-1">${stats.revenue}</p>
                </div>
                <div className="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center">
                  <DollarSign className="w-5 h-5 text-slate-500" />
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Slots Grid */}
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {[1, 2, 3, 4, 5, 6].map((i) => (
              <div key={i} className="bg-white rounded-xl border border-slate-200 p-5">
                <div className="h-24 animate-pulse" />
              </div>
            ))}
          </div>
        ) : allSlots.length === 0 ? (
          <div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
            <div className="w-12 h-12 rounded-lg bg-slate-100 flex items-center justify-center mx-auto mb-4">
              <Calendar className="w-6 h-6 text-slate-400" />
            </div>
            <h3 className="text-lg font-semibold text-slate-900 mb-2">No Slots Created Yet</h3>
            <p className="text-sm text-slate-500 mb-5 max-w-md mx-auto">
              Get started by creating your first appointment slot.
            </p>
            <button
              onClick={() => setShowAddModal(true)}
              className="inline-flex items-center px-4 py-2 bg-slate-900 text-white text-sm font-medium rounded-lg hover:bg-slate-800 transition-colors"
            >
              <Plus className="w-4 h-4 mr-2" />
              Create First Slot
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {allSlots.map((slot) => {
              const date = new Date(slot.date);
              const monthName = date.toLocaleDateString('en-US', { month: 'short' });
              const dayNum = date.getDate();
              const timeStr = formatTime(slot.date);

              return (
                <div key={slot.id}
                                  className={`bg-white rounded-xl border transition-all duration-200 overflow-hidden ${
                                    slot.isReserved
                                      ? 'border-orange-200 bg-orange-50/30'
                                      : 'border-slate-200 hover:border-indigo-300 hover:shadow-lg'
                                  }`}
                                >
                                  <div className={`flex items-start justify-between p-5 ${
                                    slot.isReserved
                                      ? 'bg-gradient-to-r from-orange-50 to-amber-50'
                                      : 'bg-gradient-to-r from-slate-50 to-slate-100'
                                  }`}>
                                    <div className="text-center">
                                      <div className="text-xs font-medium text-slate-500 uppercase tracking-wide">{monthName}</div>
                                      <div className="text-3xl font-bold text-slate-800">{dayNum}</div>
                                    </div>
                                    <div className={`w-10 h-10 rounded-lg flex items-center justify-center border ${
                                      slot.isReserved
                                        ? 'bg-orange-100 border-orange-200'
                                        : 'bg-white border-slate-200'
                                    }`}>
                                      <Clock className={`w-4 h-4 ${slot.isReserved ? 'text-orange-500' : 'text-slate-400'}`} />
                                    </div>
                                  </div>
                                  <div className="p-5">
                                    <div className="mb-4">
                                      <h3 className="text-base font-semibold text-slate-900 leading-snug">{slot.doctorName}</h3>
                                      <div className="text-xs text-slate-500 mt-1">
                                        <Clock className="w-3 h-3 inline-block mr-1" />
                                        {timeStr}
                                      </div>
                                    </div>
                                    <div className={`flex items-center justify-between pt-4 mt-4 border-t ${
                                      slot.isReserved ? 'border-orange-100' : 'border-slate-100'
                                    }`}>
                                      <div className="flex items-baseline gap-1">
                                        <span className="text-2xl font-semibold text-slate-900">${slot.cost}</span>
                                      </div>
                                      <span className={`text-xs font-medium px-2 py-1 rounded-full ${
                                        slot.isReserved
                                          ? 'bg-orange-100 text-orange-700'
                                          : 'bg-green-100 text-green-700'
                                      }`}>
                                        {slot.isReserved ? 'Reserved' : 'Available'}
                                      </span>
                                    </div>
                                  </div>
                                </div>
              );
            })}
          </div>
        )}
      </main>

      {/* Add Slot Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div
            className="absolute inset-0 bg-slate-900/50"
            onClick={() => {
              if (!submitting) {
                setShowAddModal(false);
                setNewSlot({ date: '', time: '09:00', doctorName: '', cost: '' });
                setErrors({});
              }
            }}
          />
          <div className="relative bg-white rounded-xl shadow-xl max-w-md w-full p-6 border border-slate-200">
            <div className="mb-5">
              <h3 className="text-lg font-semibold text-slate-900 mb-1">Add New Slot</h3>
              <p className="text-sm text-slate-500">Create a new appointment slot</p>
            </div>

            {/* Form */}
            <div className="space-y-4 mb-5">
              {/* Doctor Name */}
              <div>
                <label htmlFor="doctorName" className="block text-xs font-medium text-slate-700 mb-1.5">
                  Doctor Name
                </label>
                <input
                  id="doctorName"
                  type="text"
                  value={newSlot.doctorName}
                  onChange={(e) => {
                    setNewSlot({ ...newSlot, doctorName: e.target.value });
                    if (errors.doctorName) setErrors({ ...errors, doctorName: '' });
                  }}
                  className={`w-full px-3 py-2.5 border rounded-lg focus:ring-2 focus:ring-slate-500 focus:border-slate-500 outline-none text-sm ${
                    errors.doctorName ? 'border-red-300 bg-red-50' : 'border-slate-300'
                  }`}
                  placeholder="Dr. John Smith"
                  disabled={submitting}
                />
                {errors.doctorName && (
                  <p className="text-red-600 text-xs mt-1">{errors.doctorName}</p>
                )}
              </div>

              {/* Date */}
              <div>
                <label htmlFor="date" className="block text-xs font-medium text-slate-700 mb-1.5">
                  Date
                </label>
                <input
                  id="date"
                  type="date"
                  value={newSlot.date}
                  onChange={(e) => {
                    setNewSlot({ ...newSlot, date: e.target.value });
                    if (errors.date) setErrors({ ...errors, date: '' });
                  }}
                  className={`w-full px-3 py-2.5 border rounded-lg focus:ring-2 focus:ring-slate-500 focus:border-slate-500 outline-none text-sm ${
                    errors.date ? 'border-red-300 bg-red-50' : 'border-slate-300'
                  }`}
                  min={new Date().toISOString().split('T')[0]}
                  disabled={submitting}
                />
                {errors.date && (
                  <p className="text-red-600 text-xs mt-1">{errors.date}</p>
                )}
              </div>

              {/* Time */}
              <div>
                <label htmlFor="time" className="block text-xs font-medium text-slate-700 mb-1.5">
                  Time
                </label>
                <input
                  id="time"
                  type="time"
                  value={newSlot.time}
                  onChange={(e) => {
                    setNewSlot({ ...newSlot, time: e.target.value });
                    if (errors.time) setErrors({ ...errors, time: '' });
                  }}
                  className={`w-full px-3 py-2.5 border rounded-lg focus:ring-2 focus:ring-slate-500 focus:border-slate-500 outline-none text-sm ${
                    errors.time ? 'border-red-300 bg-red-50' : 'border-slate-300'
                  }`}
                  disabled={submitting}
                />
                {errors.time && (
                  <p className="text-red-600 text-xs mt-1">{errors.time}</p>
                )}
              </div>

              {/* Cost */}
              <div>
                <label htmlFor="cost" className="block text-xs font-medium text-slate-700 mb-1.5">
                  Consultation Fee ($)
                </label>
                <input
                  id="cost"
                  type="number"
                  value={newSlot.cost}
                  onChange={(e) => {
                    setNewSlot({ ...newSlot, cost: e.target.value });
                    if (errors.cost) setErrors({ ...errors, cost: '' });
                  }}
                  className={`w-full px-3 py-2.5 border rounded-lg focus:ring-2 focus:ring-slate-500 focus:border-slate-500 outline-none text-sm ${
                    errors.cost ? 'border-red-300 bg-red-50' : 'border-slate-300'
                  }`}
                  placeholder="50.00"
                  min="0"
                  step="0.01"
                  disabled={submitting}
                />
                {errors.cost && (
                  <p className="text-red-600 text-xs mt-1">{errors.cost}</p>
                )}
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3">
              <button
                onClick={() => {
                  if (!submitting) {
                    setShowAddModal(false);
                    setNewSlot({ date: '', time: '09:00', doctorName: '', cost: '' });
                    setErrors({});
                  }
                }}
                disabled={submitting}
                className="flex-1 px-4 py-2.5 border border-slate-300 text-slate-700 font-medium rounded-lg hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
              >
                Cancel
              </button>
              <button
                onClick={handleAddSlot}
                disabled={submitting}
                className="flex-1 px-4 py-2.5 bg-slate-900 text-white font-medium rounded-lg hover:bg-slate-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 text-sm"
              >
                {submitting ? (
                  <>
                    <div className="w-4 h-4 border-2 border-slate-300 border-t-slate-900 rounded-full animate-spin" />
                    <span>Creating...</span>
                  </>
                ) : (
                  <>
                    <Plus className="w-4 h-4" />
                    <span>Add Slot</span>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;