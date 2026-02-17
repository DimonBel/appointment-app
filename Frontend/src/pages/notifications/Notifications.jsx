import React, { useEffect, useState } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { Bell, Check, CheckCheck, Trash2, Settings, Clock, AlertCircle, Calendar, MessageCircle, UserPlus, UserCheck, UserX } from 'lucide-react'
import { notificationService } from '../../services/notificationService'
import { friendService } from '../../services/friendService'
import { appointmentService } from '../../services/appointmentService'
import {
  setNotifications,
  markNotificationAsRead,
  markAllAsRead,
  removeNotification,
  setUnreadCount,
  setLoading,
} from '../../store/slices/notificationsSlice'
import { addFriendId } from '../../store/slices/friendsSlice'

const typeIcons = {
  OrderCreated: Calendar,
  OrderApproved: Check,
  OrderDeclined: AlertCircle,
  OrderCancelled: AlertCircle,
  OrderRescheduled: Clock,
  OrderCompleted: CheckCheck,
  ChatMessage: MessageCircle,
  AppointmentReminder: Bell,
  SystemAlert: AlertCircle,
  FriendRequest: UserPlus,
  FriendRequestAccepted: UserCheck,
  FriendRequestDeclined: UserX,
  PasswordChanged: Check,
  BookingConfirmation: Calendar,
}

const typeByValue = {
  0: 'OrderCreated',
  1: 'OrderApproved',
  2: 'OrderDeclined',
  3: 'OrderCancelled',
  4: 'OrderCompleted',
  5: 'OrderRescheduled',
  6: 'AppointmentReminder',
  7: 'ChatMessage',
  8: 'SystemAlert',
  9: 'ProfileUpdated',
  10: 'NewProfessionalRegistered',
  11: 'DocumentUploaded',
  12: 'FriendRequest',
  13: 'FriendRequestAccepted',
  14: 'FriendRequestDeclined',
  15: 'PasswordChanged',
  16: 'BookingConfirmation',
}

const statusByValue = {
  0: 'Pending',
  1: 'Sent',
  2: 'Read',
  3: 'Failed',
  4: 'Cancelled',
}

const priorityByValue = {
  0: 'Low',
  1: 'Normal',
  2: 'High',
  3: 'Urgent',
}

const priorityColors = {
  Low: 'text-gray-400',
  Normal: 'text-blue-500',
  High: 'text-orange-500',
  Urgent: 'text-red-600',
}

const normalizeMessageText = (value) => {
  if (!value) return ''

  const withLineBreaks = String(value)
    .replace(/<\s*br\s*\/?>/gi, '\n')
    .replace(/<\s*\/\s*(p|div|tr|h1|h2|h3|h4|h5|h6|li|table)\s*>/gi, '\n')

  const withoutTags = withLineBreaks.replace(/<[^>]*>/g, ' ')

  const normalized = withoutTags
    .replace(/&nbsp;/gi, ' ')
    .replace(/&amp;/gi, '&')
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>')
    .replace(/&quot;/gi, '"')
    .replace(/&#39;/gi, "'")

  return normalized
    .split('\n')
    .map((line) => line.replace(/\s+/g, ' ').trim())
    .filter((line) => line.length > 0)
    .join('\n')
    .trim()
}

export const Notifications = () => {
  const dispatch = useDispatch()
  const { notifications, unreadCount, isLoading } = useSelector((state) => state.notifications)
  const token = useSelector((state) => state.auth.token)
  const userId = useSelector((state) => state.auth.user?.id)
  const [filter, setFilter] = useState('all') // all, unread, read
  const [processingFriendRequest, setProcessingFriendRequest] = useState(null) // notificationId
  const [friendRequestResults, setFriendRequestResults] = useState({}) // notificationId -> 'accepted'|'declined'
  const [processingBookingRequest, setProcessingBookingRequest] = useState(null) // notificationId
  const [bookingRequestResults, setBookingRequestResults] = useState({}) // notificationId -> 'accepted'|'declined'
  const [bookingRequestErrors, setBookingRequestErrors] = useState({}) // notificationId -> error

  const normalizeNotification = (notification) => ({
    ...notification,
    type: typeof notification.type === 'number' ? (typeByValue[notification.type] || notification.type) : notification.type,
    status: typeof notification.status === 'number' ? (statusByValue[notification.status] || notification.status) : notification.status,
    priority: typeof notification.priority === 'number' ? (priorityByValue[notification.priority] || notification.priority) : notification.priority,
    message: normalizeMessageText(notification.message),
  })

  useEffect(() => {
    if (!token || !userId) return
    loadNotifications()
  }, [token, userId])

  const loadNotifications = async () => {
    if (!token || !userId) return

    dispatch(setLoading(true))
    try {
      const data = await notificationService.getNotifications(userId, token)
      const normalized = (Array.isArray(data) ? data : []).map(normalizeNotification)
      dispatch(setNotifications(normalized))

      const countData = await notificationService.getUnreadCount(userId, token)
      dispatch(setUnreadCount(typeof countData === 'number' ? countData : countData?.count || 0))
    } catch (err) {
      console.error('Failed to load notifications:', err)
      dispatch(setNotifications([]))
    }
  }

  const handleMarkAsRead = async (id) => {
    try {
      await notificationService.markAsRead(id, token)
      dispatch(markNotificationAsRead(id))
    } catch (err) {
      console.error('Failed to mark as read:', err)
    }
  }

  const handleMarkAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead(userId, token)
      dispatch(markAllAsRead())
    } catch (err) {
      console.error('Failed to mark all as read:', err)
    }
  }

  const handleDelete = async (id) => {
    try {
      await notificationService.deleteNotification(id, token)
      dispatch(removeNotification(id))
    } catch (err) {
      console.error('Failed to delete notification:', err)
    }
  }

  const parseFriendRequestMetadata = (notification) => {
    try {
      if (notification.metadata) {
        const meta = typeof notification.metadata === 'string'
          ? JSON.parse(notification.metadata)
          : notification.metadata
        return { friendshipId: meta.friendshipId, senderId: meta.senderId }
      }
    } catch { /* ignore */ }
    // Fallback: try referenceId as friendshipId
    return { friendshipId: notification.referenceId, senderId: null }
  }

  const parseBookingRequestOrderId = (notification) => {
    if (notification?.referenceId) {
      return notification.referenceId
    }

    try {
      if (notification?.metadata) {
        const meta = typeof notification.metadata === 'string'
          ? JSON.parse(notification.metadata)
          : notification.metadata
        return meta?.orderId || null
      }
    } catch {
      return null
    }

    return null
  }

  const parseNotificationMetadata = (notification) => {
    if (!notification?.metadata) return null

    try {
      return typeof notification.metadata === 'string'
        ? JSON.parse(notification.metadata)
        : notification.metadata
    } catch {
      return null
    }
  }

  const isDoctorBookingRequest = (notification) => {
    if (notification?.type !== 'OrderCreated') return false

    const metadata = parseNotificationMetadata(notification)
    if (metadata?.action === 'booking_request') return true

    if (notification?.referenceType === 'Order' && /new appointment request/i.test(notification?.title || '')) {
      return true
    }

    return /new appointment request/i.test(notification?.message || '')
  }

  const handleAcceptFriendRequest = async (notification) => {
    const { friendshipId, senderId } = parseFriendRequestMetadata(notification)
    if (!friendshipId) return

    setProcessingFriendRequest(notification.id)
    try {
      await friendService.acceptFriendRequest(friendshipId, token)
      setFriendRequestResults(prev => ({ ...prev, [notification.id]: 'accepted' }))
      if (senderId) {
        dispatch(addFriendId(senderId))
      }
      // Mark notification as read
      if (notification.status !== 'Read') {
        await notificationService.markAsRead(notification.id, token)
        dispatch(markNotificationAsRead(notification.id))
      }
    } catch (err) {
      console.error('Failed to accept friend request:', err)
    } finally {
      setProcessingFriendRequest(null)
    }
  }

  const handleDeclineFriendRequest = async (notification) => {
    const { friendshipId } = parseFriendRequestMetadata(notification)
    if (!friendshipId) return

    setProcessingFriendRequest(notification.id)
    try {
      await friendService.declineFriendRequest(friendshipId, token)
      setFriendRequestResults(prev => ({ ...prev, [notification.id]: 'declined' }))
      // Mark notification as read
      if (notification.status !== 'Read') {
        await notificationService.markAsRead(notification.id, token)
        dispatch(markNotificationAsRead(notification.id))
      }
    } catch (err) {
      console.error('Failed to decline friend request:', err)
    } finally {
      setProcessingFriendRequest(null)
    }
  }

  const handleApproveBookingRequest = async (notification) => {
    const orderId = parseBookingRequestOrderId(notification)
    if (!orderId) return

    setProcessingBookingRequest(notification.id)
    setBookingRequestErrors(prev => ({ ...prev, [notification.id]: null }))
    try {
      await appointmentService.approveOrder(orderId, null, token)
      setBookingRequestResults(prev => ({ ...prev, [notification.id]: 'accepted' }))

      if (notification.status !== 'Read') {
        await notificationService.markAsRead(notification.id, token)
        dispatch(markNotificationAsRead(notification.id))
      }

      await notificationService.deleteNotification(notification.id, token)
      dispatch(removeNotification(notification.id))

      await loadNotifications()
    } catch (err) {
      console.error('Failed to approve booking request:', err)
      const apiMessage = err?.response?.data?.message || err?.response?.data?.title || 'Failed to accept request'
      setBookingRequestErrors(prev => ({ ...prev, [notification.id]: apiMessage }))
    } finally {
      setProcessingBookingRequest(null)
    }
  }

  const handleDeclineBookingRequest = async (notification) => {
    const orderId = parseBookingRequestOrderId(notification)
    if (!orderId) return

    setProcessingBookingRequest(notification.id)
    setBookingRequestErrors(prev => ({ ...prev, [notification.id]: null }))
    try {
      await appointmentService.declineOrder(orderId, 'Declined by doctor', token)
      setBookingRequestResults(prev => ({ ...prev, [notification.id]: 'declined' }))

      if (notification.status !== 'Read') {
        await notificationService.markAsRead(notification.id, token)
        dispatch(markNotificationAsRead(notification.id))
      }

      await notificationService.deleteNotification(notification.id, token)
      dispatch(removeNotification(notification.id))

      await loadNotifications()
    } catch (err) {
      console.error('Failed to decline booking request:', err)
      const apiMessage = err?.response?.data?.message || err?.response?.data?.title || 'Failed to decline request'
      setBookingRequestErrors(prev => ({ ...prev, [notification.id]: apiMessage }))
    } finally {
      setProcessingBookingRequest(null)
    }
  }

  const filteredNotifications = notifications
    .map(normalizeNotification)
    .filter((n) => {
    if (filter === 'unread') return n.status !== 'Read'
    if (filter === 'read') return n.status === 'Read'
    return true
    })

  const formatDate = (dateStr) => {
    const date = new Date(dateStr)
    const now = new Date()
    const diff = now - date
    if (diff < 60000) return 'Just now'
    if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`
    if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`
    return date.toLocaleDateString()
  }

  return (
    <div className="max-w-3xl mx-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Bell size={24} className="text-primary-dark" />
          <h1 className="text-2xl font-bold text-gray-900">Notifications</h1>
          {unreadCount > 0 && (
            <span className="bg-primary-accent text-white text-sm px-2 py-0.5 rounded-full">
              {unreadCount}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-lg bg-gray-100 hover:bg-gray-200 transition-colors"
            >
              <CheckCheck size={14} />
              Mark all read
            </button>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-2 mb-4">
        {['all', 'unread', 'read'].map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-3 py-1.5 text-sm rounded-lg capitalize transition-colors ${
              filter === f
                ? 'bg-primary-dark text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {f}
          </button>
        ))}
      </div>

      {/* Notifications list */}
      {isLoading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-dark" />
        </div>
      ) : filteredNotifications.length === 0 ? (
        <div className="text-center py-12 text-gray-500">
          <Bell size={48} className="mx-auto mb-3 opacity-30" />
          <p className="text-lg font-medium">No notifications</p>
          <p className="text-sm mt-1">You're all caught up!</p>
        </div>
      ) : (
        <div className="space-y-2">
          {filteredNotifications.map((notification) => {
            const IconComponent = typeIcons[notification.type] || Bell
            const isUnread = notification.status !== 'Read'
            const priorityColor = priorityColors[notification.priority] || 'text-gray-400'

            return (
              <div
                key={notification.id}
                className={`flex items-start gap-3 p-4 rounded-xl border transition-colors ${
                  isUnread
                    ? 'bg-blue-50/50 border-blue-100'
                    : 'bg-white border-gray-100'
                }`}
              >
                <div className={`mt-0.5 ${priorityColor}`}>
                  <IconComponent size={20} />
                </div>
                <div className="flex-1 min-w-0">
                  <p className={`text-sm ${isUnread ? 'font-semibold text-gray-900' : 'text-gray-700'}`}>
                    {notification.title}
                  </p>
                  <p className="text-sm text-gray-500 mt-0.5 whitespace-pre-line">{notification.message}</p>

                  {/* Friend Request Actions */}
                  {notification.type === 'FriendRequest' && (
                    <div className="mt-2">
                      {friendRequestResults[notification.id] === 'accepted' ? (
                        <span className="inline-flex items-center gap-1 text-xs text-green-600 bg-green-50 px-2 py-1 rounded-lg">
                          <UserCheck size={14} /> Accepted
                        </span>
                      ) : friendRequestResults[notification.id] === 'declined' ? (
                        <span className="inline-flex items-center gap-1 text-xs text-red-600 bg-red-50 px-2 py-1 rounded-lg">
                          <UserX size={14} /> Declined
                        </span>
                      ) : (
                        <div className="flex gap-2">
                          <button
                            onClick={() => handleAcceptFriendRequest(notification)}
                            disabled={processingFriendRequest === notification.id}
                            className="inline-flex items-center gap-1 px-3 py-1 text-xs font-medium rounded-lg bg-green-500 text-white hover:bg-green-600 disabled:opacity-50 transition-colors"
                          >
                            <UserCheck size={14} />
                            {processingFriendRequest === notification.id ? '...' : 'Accept'}
                          </button>
                          <button
                            onClick={() => handleDeclineFriendRequest(notification)}
                            disabled={processingFriendRequest === notification.id}
                            className="inline-flex items-center gap-1 px-3 py-1 text-xs font-medium rounded-lg bg-red-100 text-red-600 hover:bg-red-200 disabled:opacity-50 transition-colors"
                          >
                            <UserX size={14} />
                            {processingFriendRequest === notification.id ? '...' : 'Decline'}
                          </button>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Doctor Booking Request Actions */}
                  {isDoctorBookingRequest(notification) && isUnread && (
                    <div className="mt-2">
                      {bookingRequestResults[notification.id] === 'accepted' ? (
                        <span className="inline-flex items-center gap-1 text-xs text-green-600 bg-green-50 px-2 py-1 rounded-lg">
                          <Check size={14} /> Accepted
                        </span>
                      ) : bookingRequestResults[notification.id] === 'declined' ? (
                        <span className="inline-flex items-center gap-1 text-xs text-red-600 bg-red-50 px-2 py-1 rounded-lg">
                          <UserX size={14} /> Declined
                        </span>
                      ) : (
                        <div className="flex gap-2">
                          <button
                            onClick={() => handleApproveBookingRequest(notification)}
                            disabled={processingBookingRequest === notification.id}
                            className="inline-flex items-center gap-1 px-3 py-1 text-xs font-medium rounded-lg bg-green-500 text-white hover:bg-green-600 disabled:opacity-50 transition-colors"
                          >
                            <Check size={14} />
                            {processingBookingRequest === notification.id ? '...' : 'Accept'}
                          </button>
                          <button
                            onClick={() => handleDeclineBookingRequest(notification)}
                            disabled={processingBookingRequest === notification.id}
                            className="inline-flex items-center gap-1 px-3 py-1 text-xs font-medium rounded-lg bg-red-100 text-red-600 hover:bg-red-200 disabled:opacity-50 transition-colors"
                          >
                            <UserX size={14} />
                            {processingBookingRequest === notification.id ? '...' : 'Decline'}
                          </button>
                        </div>
                      )}

                      {bookingRequestErrors[notification.id] && (
                        <p className="mt-2 text-xs text-red-600">{bookingRequestErrors[notification.id]}</p>
                      )}
                    </div>
                  )}

                  <p className="text-xs text-gray-400 mt-1">{formatDate(notification.createdAt)}</p>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  {isUnread && (
                    <button
                      onClick={() => handleMarkAsRead(notification.id)}
                      className="p-1.5 rounded hover:bg-gray-100 text-gray-400 hover:text-blue-500"
                      title="Mark as read"
                    >
                      <Check size={14} />
                    </button>
                  )}
                  <button
                    onClick={() => handleDelete(notification.id)}
                    className="p-1.5 rounded hover:bg-gray-100 text-gray-400 hover:text-red-500"
                    title="Delete"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
