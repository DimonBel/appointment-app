import { useEffect, useRef } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { notificationHubService } from '../services/signalRService'
import {
  addNotification,
} from '../store/slices/notificationsSlice'
import { addFriendId } from '../store/slices/friendsSlice'

/**
 * Hook that connects to the NotificationHub via SignalR
 * and dispatches real-time notifications to the Redux store.
 * Also shows toast-style browser notifications.
 */
export function useNotificationHub() {
  const dispatch = useDispatch()
  const token = useSelector((state) => state.auth.token)
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)
  const connectedRef = useRef(false)
  const toastTimeoutRef = useRef(null)
  const toastRef = useRef(null)

  const showToast = (title, message, type) => {
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
          ${title}
        </div>
        <div style="font-size: 13px; color: #6b7280;">
          ${message}
        </div>
      </div>
    `

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

    const connectHub = async () => {
      if (connectedRef.current) return
      try {
        const hubUrl = import.meta.env.VITE_NOTIFICATION_HUB_URL || '/notificationhub'
        
        notificationHubService.on('ReceiveNotification', (notification) => {
          // Add to Redux store (addNotification already increments unreadCount)
          dispatch(addNotification(notification))

          // Show toast
          showToast(notification.title, notification.message, notification.type)

          // If friend request accepted, add to friend IDs
          if (notification.type === 'FriendRequestAccepted' && notification.metadata) {
            try {
              const meta = typeof notification.metadata === 'string'
                ? JSON.parse(notification.metadata)
                : notification.metadata
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
}
