import { useEffect, useRef } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { notificationHubService } from '../services/signalRService'
import {
  addNotification,
  setUnreadCount,
} from '../store/slices/notificationsSlice'
import { addFriendId } from '../store/slices/friendsSlice'
import { notificationService } from '../services/notificationService'

/**
 * Hook that connects to the NotificationHub via SignalR
 * and dispatches real-time notifications to the Redux store.
 * Also shows toast-style browser notifications.
 */
export function useNotificationHub() {
  const dispatch = useDispatch()
  const token = useSelector((state) => state.auth.token)
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)
  const userId = useSelector((state) => state.auth.user?.id)
  const notifications = useSelector((state) => state.notifications.notifications)
  const connectedRef = useRef(false)
  const toastTimeoutRef = useRef(null)
  const toastRef = useRef(null)

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
    12: 'FriendRequest',
    13: 'FriendRequestAccepted',
    14: 'FriendRequestDeclined',
    15: 'PasswordChanged',
    16: 'BookingConfirmation',
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

  const escapeHtml = (value) => String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')

  const formatBookingConfirmationMessageHtml = (message) => {
    const lines = String(message)
      .split('\n')
      .map((line) => line.trim())
      .filter(Boolean)

    let doctor = ''
    let date = ''
    let time = ''
    const intro = []
    const outro = []
    let detailsStarted = false

    for (const line of lines) {
      if (/^doctor\s*:/i.test(line)) {
        doctor = line.replace(/^doctor\s*:/i, '').trim()
        detailsStarted = true
        continue
      }

      if (/^date\s*:/i.test(line)) {
        date = line.replace(/^date\s*:/i, '').trim()
        detailsStarted = true
        continue
      }

      if (/^time\s*:/i.test(line)) {
        time = line.replace(/^time\s*:/i, '').trim()
        detailsStarted = true
        continue
      }

      if (!detailsStarted) {
        intro.push(line)
      } else {
        outro.push(line)
      }
    }

    const introHtml = intro.map((line) => `<div>${escapeHtml(line)}</div>`).join('')
    const outroHtml = outro.map((line) => `<div>${escapeHtml(line)}</div>`).join('')

    return `
      <div style="font-size: 13px; color: #6b7280; line-height: 1.45;">
        ${introHtml ? `<div style="margin-bottom: 10px;">${introHtml}</div>` : ''}
        <div style="margin-bottom: 10px;">
          <div style="margin-bottom: 2px;"><strong style="color: #374151;">Doctor:</strong> ${escapeHtml(doctor || 'Doctor')}</div>
          <div style="margin-bottom: 2px;"><strong style="color: #374151;">Date:</strong> ${escapeHtml(date || '-')}</div>
          <div><strong style="color: #374151;">Time:</strong> ${escapeHtml(time || '-')}</div>
        </div>
        ${outroHtml ? `<div>${outroHtml}</div>` : ''}
      </div>
    `
  }

  const showToast = (title, message, type) => {
    const safeTitle = normalizeMessageText(title)
    const safeMessage = normalizeMessageText(message)

    let shortTitle = safeTitle
    let shortMessage = safeMessage

    if (type === 'OrderCreated') {
      shortTitle = 'Booking Pending'
      shortMessage = 'Booking request is pending doctor confirmation.'
    }
    if (type === 'OrderApproved') {
      shortTitle = 'Booking Confirmed'
      shortMessage = 'Your booking was confirmed by doctor.'
    }
    if (type === 'OrderDeclined') {
      shortTitle = 'Booking Declined'
      shortMessage = 'Your booking request was declined by doctor.'
    }

    const isBookingConfirmation = type === 'BookingConfirmation'
    const messageHtml = isBookingConfirmation
      ? formatBookingConfirmationMessageHtml(shortMessage)
      : escapeHtml(shortMessage)

    // Remove existing toast
    if (toastRef.current) {
      toastRef.current.remove()
    }
    if (toastTimeoutRef.current) {
      clearTimeout(toastTimeoutRef.current)
    }

    const toast = document.createElement('div')
    toast.className = 'notification-toast'
    toast.innerHTML = `
      <div style="
        position: fixed;
        top: 80px;
        right: 20px;
        z-index: 9999;
        background: white;
        border-radius: 12px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.15);
        padding: 16px 20px;
        max-width: 360px;
        border-left: 4px solid ${type === 'FriendRequest' ? '#3b82f6' : type === 'FriendRequestAccepted' ? '#22c55e' : '#6366f1'};
        animation: slideIn 0.3s ease-out;
      ">
        <div style="font-weight: 600; font-size: 14px; color: #1f2937; margin-bottom: 4px;">
          ${shortTitle}
        </div>
        <div class="notification-toast-message" style="font-size: 13px; color: #6b7280;">
          ${messageHtml}
        </div>
      </div>
    `

    const messageElement = toast.querySelector('.notification-toast-message')
    if (messageElement) {
      if (!isBookingConfirmation) {
        messageElement.style.whiteSpace = 'pre-line'
        messageElement.style.lineHeight = '1.45'
      }
    }

    // Add animation style
    if (!document.getElementById('toast-animation-style')) {
      const style = document.createElement('style')
      style.id = 'toast-animation-style'
      style.textContent = `
        @keyframes slideIn {
          from { transform: translateX(100%); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        @keyframes slideOut {
          from { transform: translateX(0); opacity: 1; }
          to { transform: translateX(100%); opacity: 0; }
        }
      `
      document.head.appendChild(style)
    }

    document.body.appendChild(toast)
    toastRef.current = toast

    toastTimeoutRef.current = setTimeout(() => {
      if (toast.firstElementChild) {
        toast.firstElementChild.style.animation = 'slideOut 0.3s ease-in forwards'
      }
      setTimeout(() => {
        toast.remove()
        if (toastRef.current === toast) toastRef.current = null
      }, 300)
    }, 5000)
  }

  useEffect(() => {
    if (!isAuthenticated || !token) return

    const normalizeIncomingNotification = (notification) => {
      const normalizedType = typeof notification?.type === 'number'
        ? (typeByValue[notification.type] || notification.type)
        : notification?.type

      return {
        ...notification,
        type: normalizedType,
      }
    }

    const connectHub = async () => {
      if (connectedRef.current) return
      try {
        const hubUrl = import.meta.env.VITE_NOTIFICATION_HUB_URL || '/notificationhub'
        
        notificationHubService.on('ReceiveNotification', (notification) => {
          const normalizedNotification = normalizeIncomingNotification(notification)

          // Add to Redux store (addNotification already increments unreadCount)
          dispatch(addNotification(normalizedNotification))

          // Show toast
          showToast(normalizedNotification.title, normalizedNotification.message, normalizedNotification.type)

          // If friend request accepted, add to friend IDs
          if (normalizedNotification.type === 'FriendRequestAccepted' && normalizedNotification.metadata) {
            try {
              const meta = typeof normalizedNotification.metadata === 'string'
                ? JSON.parse(normalizedNotification.metadata)
                : normalizedNotification.metadata
              if (meta.senderId) {
                dispatch(addFriendId(meta.senderId))
              }
            } catch { /* ignore */ }
          }
        })

        notificationHubService.on('NotificationMarkedRead', (notificationId) => {
          // Could handle this if needed
        })

        await notificationHubService.connect(token, hubUrl)
        connectedRef.current = true
        console.log('NotificationHub connected')
      } catch (err) {
        console.error('Failed to connect to NotificationHub:', err)
      }
    }

    connectHub()

    return () => {
      if (connectedRef.current) {
        notificationHubService.disconnect()
        connectedRef.current = false
      }
      if (toastRef.current) {
        toastRef.current.remove()
      }
      if (toastTimeoutRef.current) {
        clearTimeout(toastTimeoutRef.current)
      }
    }
  }, [isAuthenticated, token])

  useEffect(() => {
    if (!isAuthenticated || !token || !userId) return

    let cancelled = false

    const pollMissedNotifications = async () => {
      try {
        const unread = await notificationService.getUnreadNotifications(userId, token)
        const unreadList = Array.isArray(unread) ? unread : []
        const existingIds = new Set((Array.isArray(notifications) ? notifications : []).map((n) => n?.id))

        unreadList
          .slice()
          .sort((a, b) => new Date(a?.createdAt || 0).getTime() - new Date(b?.createdAt || 0).getTime())
          .forEach((item) => {
            if (!item?.id || existingIds.has(item.id)) return
            dispatch(addNotification(item))
          })

        if (!cancelled) {
          dispatch(setUnreadCount(unreadList.length))
        }
      } catch {
        // Ignore polling errors; SignalR still handles realtime path
      }
    }

    pollMissedNotifications()
    const intervalId = setInterval(pollMissedNotifications, 12000)

    return () => {
      cancelled = true
      clearInterval(intervalId)
    }
  }, [isAuthenticated, token, userId, notifications, dispatch])
}
