import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSelector, useDispatch } from 'react-redux'
import { MainContent } from '../../components/layout/MainContent'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Input } from '../../components/ui/Input'
import FileUpload from '../../components/ui/FileUpload'
import { chatService } from '../../services/chatService'
import { friendService } from '../../services/friendService'
import { chatHubService } from '../../services/signalRService'
import documentService from '../../services/documentService'
import { setChats, selectChat, addChat, updateChat, clearUnreadCount } from '../../store/slices/chatsSlice'
import { addMessage, setMessages, deleteMessage } from '../../store/slices/messagesSlice'
import { setFriendIds as setFriendIdsAction, addFriendId } from '../../store/slices/friendsSlice'
import { Search, Send, MoreVertical, UserPlus, X, House, Clock, UserCheck, UserX, Paperclip } from 'lucide-react'

export const Chat = () => {
  const navigate = useNavigate()
  const [message, setMessage] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [showUserSearch, setShowUserSearch] = useState(false)
  const [userSearchQuery, setUserSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState([])
  const [allPeople, setAllPeople] = useState([])
  const [friendStatuses, setFriendStatuses] = useState({}) // userId -> {status, friendshipId}
  const [searchMode, setSearchMode] = useState('all')
  const [searching, setSearching] = useState(false)
  const [sendingRequest, setSendingRequest] = useState(null) // userId currently sending request to
  const [attachedFile, setAttachedFile] = useState(null)
  const [showFileUpload, setShowFileUpload] = useState(false)
  const dispatch = useDispatch()
  
  const token = useSelector((state) => state.auth.token)
  const user = useSelector((state) => state.auth.user)
  const chats = useSelector((state) => state.chats.chats)
  const selectedChatId = useSelector((state) => state.chats.selectedChatId)
  const messagesByChatId = useSelector((state) => state.messages.messagesByChatId)
  const friendIds = useSelector((state) => state.friends.friendIds)

  const selectedChat = chats.find(c => c.id === selectedChatId)
  const messages = selectedChatId ? messagesByChatId[selectedChatId] || [] : []
  const canSendToSelectedChat = selectedChatId ? friendIds.includes(selectedChatId) : false

  const getDisplayName = (item) => (
    (item?.firstName && item?.lastName
      ? `${item.firstName} ${item.lastName}`
      : item?.firstName || item?.lastName) ||
    item?.userName ||
    item?.email ||
    'User'
  )

  const enrichChatUser = async (chatUser) => {
    try {
      const identityUser = await chatService.getUser(chatUser.id, token)
      return {
        ...chatUser,
        firstName: identityUser?.firstName,
        lastName: identityUser?.lastName,
        userName: identityUser?.userName || chatUser.userName,
        email: identityUser?.email || chatUser.email,
        avatarUrl: identityUser?.avatarUrl || chatUser.avatarUrl,
        isOnline: typeof identityUser?.isOnline === 'boolean' ? identityUser.isOnline : chatUser.isOnline,
      }
    } catch {
      return chatUser
    }
  }

  const refreshChatNames = async () => {
    if (!token || !chats.length) return

    const refreshed = await Promise.all(
      chats.map(async (chat) => {
        const merged = await enrichChatUser(chat)
        return {
          ...chat,
          name: getDisplayName(merged),
          userName: merged.userName,
          email: merged.email,
          avatar: merged.avatarUrl,
          isOnline: !!merged.isOnline,
        }
      })
    )

    dispatch(setChats(refreshed))
  }

  useEffect(() => {
    if (token) {
      bootstrapChat()
    }
  }, [token])

  const bootstrapChat = async () => {
    const ids = await loadFriendIds()
    await initializeChat(ids)
  }

  const loadFriendIds = async () => {
    try {
      const ids = await friendService.getFriendIds(token)
      const normalized = Array.isArray(ids) ? ids : []
      dispatch(setFriendIdsAction(normalized))
      return normalized
    } catch (error) {
      console.error('Error loading friend IDs:', error)
      return []
    }
  }

  useEffect(() => {
    const handleProfileUpdated = () => {
      refreshChatNames()
    }

    window.addEventListener('profile-updated', handleProfileUpdated)
    return () => window.removeEventListener('profile-updated', handleProfileUpdated)
  }, [token, chats])

  const initializeChat = async (availableFriendIds = []) => {
    try {
      // Fetch chats
      const chatsData = await chatService.getChats(token)
      const sourceChats = Array.isArray(chatsData) ? chatsData : []

      const resolvedChats = await Promise.all(
        sourceChats.map((chatUser) => enrichChatUser(chatUser))
      )

      const normalizedChats = resolvedChats
        .filter((chatUser) => availableFriendIds.includes(chatUser.id))
        .map((chatUser) => ({
          id: chatUser.id,
          name: getDisplayName(chatUser),
          userName: chatUser.userName,
          email: chatUser.email,
          avatar: chatUser.avatarUrl,
          lastMessage: '',
          lastMessageTime: '',
          unreadCount: 0,
          isOnline: !!chatUser.isOnline,
        }))

      dispatch(setChats(normalizedChats))

      // Set up message handlers before connecting so early server events are handled
      chatHubService.on('ReceiveMessage', async (senderId, content, messageId, createdAt) => {
        let chatName = 'User'
        let chatEmail = ''
        let chatAvatar = null
        let isOnline = false

        const existingChat = chats.find((chat) => chat.id === senderId)

        if (existingChat) {
          chatName = existingChat.name
          chatEmail = existingChat.email || ''
          chatAvatar = existingChat.avatar || null
          isOnline = !!existingChat.isOnline
        } else {
          try {
            const senderUser = await chatService.getUser(senderId, token)
            chatName = getDisplayName(senderUser)
            chatEmail = senderUser?.email || ''
            chatAvatar = senderUser?.avatarUrl || null
            isOnline = !!senderUser?.isOnline
          } catch {
            chatName = `User ${String(senderId).slice(0, 8)}`
          }

          dispatch(addChat({
            id: senderId,
            name: chatName,
            userName: chatName,
            email: chatEmail,
            avatar: chatAvatar,
            lastMessage: '',
            lastMessageTime: '',
            unreadCount: 0,
            isOnline,
          }))
        }

        dispatch(addMessage({
          id: messageId,
          chatId: senderId,
          senderId,
          content,
          timestamp: createdAt,
          status: 'delivered',
        }))

        dispatch(updateChat({
          id: senderId,
          lastMessage: content,
          lastMessageTime: new Date(createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          unreadCount: selectedChatId === senderId ? 0 : 1,
        }))
      })

      chatHubService.on('MessageSent', (receiverId, content, messageId, createdAt) => {
        dispatch(updateChat({
          id: receiverId,
          lastMessage: content,
          lastMessageTime: new Date(createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        }))
      })

      chatHubService.on('UserOnline', (onlineUserId) => {
        dispatch(updateChat({ id: onlineUserId, isOnline: true }))
      })

      chatHubService.on('UserOffline', (offlineUserId) => {
        dispatch(updateChat({ id: offlineUserId, isOnline: false }))
      })

      chatHubService.on('MessageError', (errorMessage) => {
        console.error('SignalR message error:', errorMessage)
      })

      // Connect to SignalR
      const hubUrl = import.meta.env.VITE_CHAT_HUB_URL || '/chathub'
      await chatHubService.connect(token, hubUrl)
    } catch (error) {
      console.error('Error initializing chat:', error)
    }
  }

  const handleSendMessage = async () => {
    if ((!message.trim() && !attachedFile) || !selectedChatId) return

    if (!canSendToSelectedChat) {
      alert('You can only send messages to friends. Send a friend request first!')
      return
    }

    const tempMessageId = Date.now().toString()
    try {
      // Build message content with file attachment if present
      let messageContent = message
      if (attachedFile) {
        const fileInfo = {
          type: 'file',
          fileName: attachedFile.originalFileName,
          fileSize: attachedFile.fileSize,
          contentType: attachedFile.contentType,
          documentId: attachedFile.id,
          downloadUrl: attachedFile.downloadUrl
        }
        messageContent = JSON.stringify({ text: message, file: fileInfo })
      }

      const newMessage = {
        id: tempMessageId,
        chatId: selectedChatId,
        senderId: user.id,
        content: messageContent,
        timestamp: new Date().toISOString(),
        status: 'sending',
      }

      dispatch(addMessage(newMessage))
      setMessage('')
      setAttachedFile(null)
      setShowFileUpload(false)

      await chatService.sendMessage(selectedChatId, messageContent, token)
    } catch (error) {
      console.error('Error sending message:', error)
      dispatch(deleteMessage({ chatId: selectedChatId, messageId: tempMessageId }))

      const apiError = error?.response?.data?.error
      if (typeof apiError === 'string' && apiError.trim()) {
        alert(apiError)
      }
    }
  }

  const handleChatSelect = async (chatId) => {
    dispatch(selectChat(chatId))
    dispatch(clearUnreadCount(chatId))
    
    try {
      const messagesData = await chatService.getMessages(chatId, token)
      const normalizedMessages = (Array.isArray(messagesData) ? messagesData : [])
        .map((item) => ({
          id: item.id,
          chatId,
          senderId: item.senderId,
          content: item.content,
          timestamp: item.createdAt,
          status: 'delivered',
        }))
        .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime())
      dispatch(setMessages({ chatId, messages: normalizedMessages }))
    } catch (error) {
      console.error('Error loading messages:', error)
    }
  }

  const filteredChats = chats.filter((chat) => {
    const q = searchQuery.toLowerCase()
    return (
      (chat.name || '').toLowerCase().includes(q) ||
      (chat.userName || '').toLowerCase().includes(q) ||
      (chat.email || '').toLowerCase().includes(q)
    )
  })

  const handleSearchUsers = async () => {
    setSearching(true)
    try {
      let source = allPeople

      if (!source.length || userSearchQuery.trim()) {
        const results = await chatService.searchUsers(userSearchQuery.trim(), token)
        source = (Array.isArray(results) ? results : []).filter((item) => item.id !== user?.id)
      }

      const normalized = source.map((item) => ({
        ...item,
        displayName: getDisplayName(item),
      }))

      if (!allPeople.length && !userSearchQuery.trim()) {
        setAllPeople(normalized)
      }

      const q = userSearchQuery.toLowerCase()
      const inMode = normalized.filter((person) =>
        searchMode === 'all' ? true : friendIds.includes(person.id)
      )

      const filtered = inMode.filter((person) =>
        !q ||
        (person.displayName || '').toLowerCase().includes(q) ||
        (person.userName || '').toLowerCase().includes(q) ||
        (person.email || '').toLowerCase().includes(q)
      )

      setSearchResults(filtered)

      // Load friendship statuses for non-friend users in "all" mode
      if (searchMode === 'all') {
        const unknownUsers = filtered.filter(p => !friendIds.includes(p.id) && !friendStatuses[p.id])
        const statusPromises = unknownUsers.map(async (p) => {
          try {
            const status = await friendService.getFriendshipStatus(p.id, token)
            return { userId: p.id, ...status }
          } catch {
            return { userId: p.id, status: 'none', friendshipId: null }
          }
        })
        const statuses = await Promise.all(statusPromises)
        const newStatuses = {}
        statuses.forEach(s => { newStatuses[s.userId] = { status: s.status, friendshipId: s.friendshipId } })
        setFriendStatuses(prev => ({ ...prev, ...newStatuses }))
      }
    } catch (error) {
      console.error('Error searching users:', error)
      setSearchResults([])
    } finally {
      setSearching(false)
    }
  }

  const handleStartChat = async (userId, userName, email, avatarUrl, isOnline) => {
    // Only allow chatting with friends
    if (!friendIds.includes(userId)) {
      alert('You can only chat with friends. Send a friend request first!')
      return
    }

    // Check if chat already exists
    const existingChat = chats.find(chat => chat.id === userId)
    if (existingChat) {
      handleChatSelect(userId)
    } else {
      // Add new chat to the list
      const newChat = {
        id: userId,
        name: userName,
        userName,
        email,
        avatar: avatarUrl || null,
        lastMessage: '',
        lastMessageTime: '',
        unreadCount: 0,
        isOnline: !!isOnline
      }
      dispatch(addChat(newChat))
      handleChatSelect(userId)
    }
    setShowUserSearch(false)
    setUserSearchQuery('')
    setSearchResults([])
  }

  useEffect(() => {
    if (!showUserSearch) {
      return
    }

    const timeoutId = setTimeout(() => {
      handleSearchUsers()
    }, 250)
    return () => clearTimeout(timeoutId)
  }, [userSearchQuery, searchMode, showUserSearch])

  const handleAddFriend = async (person) => {
    if (!person?.id) return
    if (sendingRequest === person.id) return

    setSendingRequest(person.id)
    try {
      const statusInfo = friendStatuses[person.id]
      if (statusInfo?.status === 'pending_received' && statusInfo?.friendshipId) {
        await friendService.acceptFriendRequest(statusInfo.friendshipId, token)
        dispatch(addFriendId(person.id))
        setFriendStatuses(prev => ({
          ...prev,
          [person.id]: { status: 'friends', friendshipId: statusInfo.friendshipId }
        }))
        return
      }

      await friendService.sendFriendRequest(person.id, token)
      setFriendStatuses(prev => ({
        ...prev,
        [person.id]: { status: 'pending_sent', friendshipId: null }
      }))
    } catch (error) {
      console.error('Error sending friend request:', error)
      const apiMessage = error?.response?.data?.error || error?.response?.data?.message
      if (apiMessage) {
        alert(apiMessage)
      }
    } finally {
      setSendingRequest(null)
    }
  }

  return (
    <div className="fixed inset-0 mt-16 flex bg-background-app">
      {/* Chat List */}
      <div className="w-80 bg-white border-r border-gray-200 flex flex-col">
        <div className="p-4 border-b border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-text-primary">Messages</h2>
            <div className="flex items-center gap-2">
              <button
                onClick={() => navigate('/')}
                className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                title="Go Home"
              >
                <House size={20} className="text-primary-accent" />
              </button>
              <button
                onClick={() => setShowUserSearch(!showUserSearch)}
                className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                title="Search for users"
              >
                <UserPlus size={20} className="text-primary-accent" />
              </button>
            </div>
          </div>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-text-muted" size={18} />
            <input
              type="text"
              placeholder="Search conversations..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent text-sm"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          {filteredChats.map((chat) => (
            <div
              key={chat.id}
              onClick={() => handleChatSelect(chat.id)}
              className={`p-4 border-b border-gray-100 cursor-pointer transition-colors ${
                selectedChatId === chat.id ? 'bg-primary-accent/10' : 'hover:bg-gray-50'
              }`}
            >
              <div className="flex items-start gap-3">
                <Avatar src={chat.avatar} alt={chat.name} size={48} />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between mb-1">
                    <h3 className="font-semibold text-text-primary truncate">{chat.name}</h3>
                    <span className="text-xs text-text-muted">{chat.lastMessageTime}</span>
                  </div>
                  <p className="text-sm text-text-secondary truncate">{chat.lastMessage}</p>
                  {chat.unreadCount > 0 && (
                    <span className="inline-block mt-1 px-2 py-0.5 bg-primary-accent text-white text-xs rounded-full">
                      {chat.unreadCount}
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Chat Window */}
      <div className="flex-1 flex flex-col bg-white">
        {selectedChat ? (
          <>
            {/* Chat Header */}
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Avatar src={selectedChat.avatar} alt={selectedChat.name} size={40} />
                <div>
                  <h3 className="font-semibold text-text-primary">{selectedChat.name}</h3>
                  <p className="text-sm text-text-secondary">
                    {selectedChat.isOnline ? 'Online' : 'Offline'}
                  </p>
                </div>
              </div>
              <button className="p-2 hover:bg-gray-100 rounded-lg">
                <MoreVertical size={20} />
              </button>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {messages.map((msg) => (
                <div
                  key={msg.id}
                  className={`flex ${msg.senderId === user.id ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-xs lg:max-w-md px-4 py-2 rounded-2xl ${
                      msg.senderId === user.id
                        ? 'bg-primary-accent text-white'
                        : 'bg-gray-100 text-text-primary'
                    }`}
                  >
                    <p className="text-sm">{msg.content}</p>
                    <span className="text-xs opacity-70 mt-1 block">
                      {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                </div>
              ))}
            </div>

            {/* Message Input */}
            <div className="p-4 border-t border-gray-200">
              {showFileUpload && (
                <div className="mb-2">
                  <FileUpload
                    onFileUploaded={(file) => {
                      setAttachedFile(file)
                      setShowFileUpload(false)
                    }}
                    documentType="ChatFile"
                    linkedEntityType="Chat"
                    linkedEntityId={selectedChatId}
                    disabled={!canSendToSelectedChat}
                  />
                </div>
              )}
              {attachedFile && (
                <div className="mb-2 flex items-center gap-2 p-2 bg-gray-50 rounded-lg">
                  <span className="text-lg">{documentService.getFileIcon(attachedFile.contentType)}</span>
                  <span className="text-sm text-text-primary flex-1 truncate">{attachedFile.originalFileName}</span>
                  <span className="text-xs text-text-secondary">{documentService.formatFileSize(attachedFile.fileSize)}</span>
                  <button
                    onClick={() => setAttachedFile(null)}
                    className="p-1 hover:bg-gray-200 rounded text-gray-500"
                  >
                    <X size={16} />
                  </button>
                </div>
              )}
              <div className="flex gap-2">
                <button
                  onClick={() => setShowFileUpload(!showFileUpload)}
                  disabled={!canSendToSelectedChat}
                  className={`p-2 rounded-lg transition-colors ${
                    showFileUpload
                      ? 'bg-primary-accent text-white'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  } ${!canSendToSelectedChat ? 'opacity-50 cursor-not-allowed' : ''}`}
                  title="Attach file"
                >
                  <Paperclip size={20} />
                </button>
                <input
                  type="text"
                  placeholder="Type a message..."
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleSendMessage()}
                  disabled={!canSendToSelectedChat}
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
                />
                <Button onClick={handleSendMessage} variant="primary" disabled={!canSendToSelectedChat || (!message.trim() && !attachedFile)}>
                  <Send size={20} />
                </Button>
              </div>
              {!canSendToSelectedChat && (
                <p className="mt-2 text-xs text-amber-600">You can only send messages to friends.</p>
              )}
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center text-text-muted">
            <div className="text-center">
              <Search size={64} className="mx-auto mb-4 opacity-50" />
              <p>Select a conversation to start messaging</p>
            </div>
          </div>
        )}
      </div>

      {/* User Search Modal */}
      {showUserSearch && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg w-full max-w-md mx-4">
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="text-lg font-semibold text-text-primary">Search Users</h3>
              <button
                onClick={() => {
                  setShowUserSearch(false)
                  setUserSearchQuery('')
                  setSearchResults([])
                }}
                className="p-1 hover:bg-gray-100 rounded"
              >
                <X size={20} />
              </button>
            </div>
            
            <div className="p-4">
              <div className="flex gap-2 mb-3">
                <Button
                  type="button"
                  size="sm"
                  variant={searchMode === 'all' ? 'primary' : 'outline'}
                  onClick={() => setSearchMode('all')}
                >
                  All People
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant={searchMode === 'friends' ? 'primary' : 'outline'}
                  onClick={() => setSearchMode('friends')}
                >
                  Friends
                </Button>
              </div>

              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-text-muted" size={18} />
                <input
                  type="text"
                  placeholder={searchMode === 'all' ? 'Search by name, username or email...' : 'Search in friends...'}
                  value={userSearchQuery}
                  onChange={(e) => setUserSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent text-sm"
                  autoFocus
                />
              </div>
              
              <div className="max-h-96 overflow-y-auto">
                {searching ? (
                  <div className="text-center py-8 text-text-muted">
                    <p>Searching...</p>
                  </div>
                ) : searchResults.length > 0 ? (
                  searchResults.map((searchUser) => (
                    <div
                      key={searchUser.id}
                      className="p-3 border-b border-gray-100 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center gap-3 justify-between">
                        <div
                          onClick={() =>
                            handleStartChat(
                              searchUser.id,
                              searchUser.firstName && searchUser.lastName
                                ? `${searchUser.firstName} ${searchUser.lastName}`
                                : searchUser.userName,
                              searchUser.email,
                              searchUser.avatarUrl,
                              searchUser.isOnline
                            )
                          }
                          className="flex items-center gap-3 min-w-0 flex-1 cursor-pointer"
                        >
                        <Avatar src={searchUser.avatarUrl} alt={searchUser.userName} size={40} />
                        <div className="flex-1 min-w-0">
                          <h4 className="font-medium text-text-primary truncate">
                            {searchUser.firstName && searchUser.lastName
                              ? `${searchUser.firstName} ${searchUser.lastName}`
                              : searchUser.userName}
                          </h4>
                          <p className="text-sm text-text-secondary truncate">{searchUser.email}</p>
                        </div>
                        </div>

                        {searchMode === 'all' && !friendIds.includes(searchUser.id) && (
                          (() => {
                            const fs = friendStatuses[searchUser.id]
                            const status = fs?.status || 'none'
                            if (status === 'pending_sent') {
                              return (
                                <span className="flex items-center gap-1 text-xs text-amber-600 bg-amber-50 px-2 py-1 rounded-lg">
                                  <Clock size={14} /> Pending
                                </span>
                              )
                            }
                            if (status === 'pending_received') {
                              return (
                                <Button
                                  type="button"
                                  size="sm"
                                  variant="outline"
                                  onClick={() => handleAddFriend(searchUser)}
                                  disabled={sendingRequest === searchUser.id}
                                >
                                  {sendingRequest === searchUser.id ? '...' : 'Accept'}
                                </Button>
                              )
                            }
                            return (
                              <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={() => handleAddFriend(searchUser)}
                                disabled={sendingRequest === searchUser.id}
                              >
                                {sendingRequest === searchUser.id ? '...' : 'Add Friend'}
                              </Button>
                            )
                          })()
                        )}
                        {friendIds.includes(searchUser.id) && (
                          <span className="flex items-center gap-1 text-xs text-green-600 bg-green-50 px-2 py-1 rounded-lg">
                            <UserCheck size={14} /> Friends
                          </span>
                        )}
                      </div>
                    </div>
                  ))
                ) : userSearchQuery ? (
                  <div className="text-center py-8 text-text-muted">
                    <Search size={48} className="mx-auto mb-2 opacity-50" />
                    <p>No users found</p>
                  </div>
                ) : (
                  <div className="text-center py-8 text-text-muted">
                    <p>{searchMode === 'all' ? 'Type to search people' : 'No friends yet or no matches'}</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
