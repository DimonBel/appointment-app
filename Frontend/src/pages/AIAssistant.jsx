import React, { useState, useEffect, useRef } from 'react'
import { Send, Bot, User, Loader2, Sparkles, Calendar, Clock, CheckCircle, X, House, Plus, MessageSquare, MoreVertical, Trash2, Edit3 } from 'lucide-react'
import { automationService } from '../services/automationService'
import { useSelector } from 'react-redux'

export const AIAssistant = () => {
  const [messages, setMessages] = useState([])
  const [inputMessage, setInputMessage] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [isStreaming, setIsStreaming] = useState(false)
  const [conversationId, setConversationId] = useState(null)
  const [conversations, setConversations] = useState([])
  const [suggestedOptions, setSuggestedOptions] = useState([])
  const [isBookingComplete, setIsBookingComplete] = useState(false)
  const [error, setError] = useState(null)
  const [streamingContent, setStreamingContent] = useState('')
  const [showNewChatButton, setShowNewChatButton] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const messagesEndRef = useRef(null)
  const connectionRef = useRef(null)
  const streamingMessageIdRef = useRef(null)
  const isConnectingRef = useRef(false)
  const joinedConversationRef = useRef(null)
  const sendInFlightRef = useRef(false)
  const token = useSelector((state) => state.auth.token)
  const user = useSelector((state) => state.auth.user)

  // Initialize conversation
  useEffect(() => {
    if (token) {
      initializeConversation()
      loadConversations()
    }
  }, [token])

  const loadConversations = async () => {
    try {
      const convs = await automationService.listConversations()
      setConversations(convs)
    } catch (error) {
      console.error('Failed to load conversations:', error)
    }
  }

  // Scroll to bottom when messages change
  useEffect(() => {
    scrollToBottom()
  }, [messages, streamingContent])

  // Setup SignalR connection for streaming
  useEffect(() => {
    if (token) {
      setupSignalRConnection()
    }
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop()
      }
      connectionRef.current = null
      isConnectingRef.current = false
      joinedConversationRef.current = null
    }
  }, [token])

  useEffect(() => {
    const joinConversation = async () => {
      if (!conversationId || !connectionRef.current) return
      if (joinedConversationRef.current === conversationId) return
      if (connectionRef.current.state !== 'Connected') return

      try {
        await connectionRef.current.invoke('JoinConversation', conversationId)
        joinedConversationRef.current = conversationId
      } catch (err) {
        console.error('Failed to join conversation:', err)
      }
    }

    joinConversation()
  }, [conversationId])

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  const setupSignalRConnection = async () => {
    try {
      if (isConnectingRef.current) return
      if (connectionRef.current?.state === 'Connected') return

      isConnectingRef.current = true

      const { HubConnectionBuilder, HttpTransportType } = await import('@microsoft/signalr')
      const baseUrl = globalThis.location.origin
      const connection = new HubConnectionBuilder()
        .withUrl(`${baseUrl}/automationhub`, {
          accessTokenFactory: () => token,
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withAutomaticReconnect()
        .build()

      connection.onreconnecting(() => {
        setIsLoading(false)
        setIsStreaming(false)
        setStreamingContent('')
      })

      connection.onreconnected(async () => {
        setIsLoading(false)
        setIsStreaming(false)
        setStreamingContent('')
        if (conversationId) {
          try {
            await connection.invoke('JoinConversation', conversationId)
            joinedConversationRef.current = conversationId
          } catch (err) {
            console.error('Failed to rejoin conversation after reconnect:', err)
          }
        }
      })

      connection.onclose(() => {
        setIsLoading(false)
        setIsStreaming(false)
        setStreamingContent('')
      })

      connection.on('ReceiveStreamChunk', (data) => {
        const chunk = typeof data === 'string'
          ? data
          : (data?.chunk ?? data?.Chunk ?? '')
        const isComplete = typeof data === 'object'
          ? (data?.isComplete ?? data?.IsComplete ?? false)
          : false

        if (chunk) {
          setIsStreaming(true)
          setIsLoading(false)
          setShowNewChatButton(false)
          setStreamingContent(prev => prev === '...' ? chunk : (prev || '') + chunk)

          setMessages(prev => {
            let streamId = streamingMessageIdRef.current
            const next = [...prev]

            if (!streamId || !next.some(msg => msg.id === streamId)) {
              streamId = `stream-${Date.now()}`
              streamingMessageIdRef.current = streamId
              next.push({
                id: streamId,
                content: '',
                isFromUser: false,
                suggestedOptions: [],
                selectedOption: null,
                timestamp: new Date()
              })
            }

            return next.map(msg =>
              msg.id === streamId
                ? { ...msg, content: (msg.content || '') + chunk, timestamp: new Date() }
                : msg
            )
          })
        }

        if (isComplete) {
          setIsStreaming(false)
          setIsLoading(false)
          setStreamingContent('')
          setShowNewChatButton(true)
        }
      })

      connection.on('ReceiveMessage', (data) => {
        if (data?.message) {
          const finalMessage = {
            id: data.message.id || Date.now(),
            content: data.message.content || '',
            isFromUser: false,
            suggestedOptions: data.message.suggestedOptions || [],
            selectedOption: null,
            timestamp: data.message.sentAt ? new Date(data.message.sentAt) : new Date()
          }

          setMessages(prev => {
            const streamId = streamingMessageIdRef.current
            if (streamId && prev.some(msg => msg.id === streamId)) {
              return prev.map(msg => msg.id === streamId ? finalMessage : msg)
            }
            return [...prev, finalMessage]
          })

          streamingMessageIdRef.current = null
          setSuggestedOptions(data.message.suggestedOptions || [])
        }
        setStreamingContent('')
        setIsStreaming(false)
        setIsLoading(false)
        setShowNewChatButton(true)
      })

      connection.on('TypingIndicator', (isTyping) => {
        setIsLoading(isTyping)
      })

      await connection.start()

      if (conversationId) {
        await connection.invoke('JoinConversation', conversationId)
        joinedConversationRef.current = conversationId
      }

      connectionRef.current = connection
    } catch (err) {
      console.error('SignalR connection error:', err)
    } finally {
      isConnectingRef.current = false
    }
  }

  const initializeConversation = async () => {
    try {
      setError(null)
      const conversation = await automationService.getActiveConversation()
      if (conversation) {
        setConversationId(conversation.id)
        // Load existing messages
        const existingMessages = await automationService.getConversationMessages(conversation.id)
        setMessages(existingMessages.map(msg => ({
          id: msg.id,
          content: msg.content,
          isFromUser: msg.isFromUser,
          suggestedOptions: msg.suggestedOptions,
          selectedOption: msg.selectedOption,
          timestamp: new Date(msg.sentAt)
        })))
      } else {
        // Start new conversation
        const newConversation = await automationService.startConversation()
        setConversationId(newConversation.id)
        // Add AI greeting
        addMessage("Hello! I'm your AI booking assistant. How can I help you today? You can tell me you want to book an appointment, check availability, or ask any questions.", false, [
          "Book a new appointment",
          "Check availability",
          "View my appointments",
          "Ask a question"
        ])
      }
    } catch (error) {
      console.error('Failed to initialize conversation:', error)
      setError('Unable to connect to the AI assistant. Please try again later.')
    }
  }

  const addMessage = (content, isFromUser, options = [], selectedOption = null) => {
    const newMessage = {
      id: Date.now(),
      content,
      isFromUser,
      suggestedOptions: options,
      selectedOption,
      timestamp: new Date()
    }
    setMessages(prev => [...prev, newMessage])
    return newMessage
  }

  const handleSendMessage = async (messageOverride = null, conversationIdOverride = null) => {
    if (sendInFlightRef.current) return

    const messageToSend = (messageOverride ?? inputMessage).trim()
    if (!messageToSend || isLoading || isStreaming) return

    const effectiveConversationId = conversationIdOverride ?? conversationId

    sendInFlightRef.current = true

    const userMessage = messageToSend
    if (messageOverride === null) {
      setInputMessage('')
    }
    setSuggestedOptions([])
    setError(null)
    streamingMessageIdRef.current = null
    setStreamingContent('')
    setShowNewChatButton(false)

    // Add user message immediately
    addMessage(userMessage, true)

    // Start streaming immediately (even before LLM responds)
    setIsLoading(true)
    setIsStreaming(true)
    
    // Show typing indicator immediately
    setStreamingContent('...')

    try {
      // The streaming will be handled by SignalR
      const response = await automationService.sendMessage(userMessage, effectiveConversationId)
      
      // Update conversation ID if new
      if (response.conversationId && !effectiveConversationId) {
        setConversationId(response.conversationId)
      }

      // Once streaming is complete, add the final message
      setSuggestedOptions(response.suggestedOptions || [])
      setIsBookingComplete(response.isBookingComplete || false)
    } catch (error) {
      console.error('Failed to send message:', error)
      setError('Failed to send message. Please try again.')
      setStreamingContent('')
      addMessage("Sorry, I'm having trouble connecting. Please try again.", false)
      setIsStreaming(false)
    } finally {
      setIsLoading(false)
      sendInFlightRef.current = false
    }
  }

  const startNewBookingConversation = async () => {
    if (isLoading || isStreaming || sendInFlightRef.current) return

    try {
      setError(null)
      setIsBookingComplete(false)
      setSuggestedOptions([])
      setStreamingContent('')
      setShowNewChatButton(false)
      streamingMessageIdRef.current = null

      const newConversation = await automationService.startNewConversation()
      const newConversationId = newConversation.id

      setConversationId(newConversationId)
      setMessages([])

      if (connectionRef.current?.state === 'Connected') {
        try {
          await connectionRef.current.invoke('JoinConversation', newConversationId)
          joinedConversationRef.current = newConversationId
        } catch (err) {
          console.error('Failed to join new booking conversation:', err)
        }
      }

      await loadConversations()
      await handleSendMessage('Book a new appointment', newConversationId)
    } catch (error) {
      console.error('Failed to start a new booking conversation:', error)
      setError('Unable to start a new booking. Please try again.')
    }
  }

  const handleSelectOption = async (option) => {
    if (typeof option === 'string' && option.trim().toLowerCase() === 'book another appointment') {
      await startNewBookingConversation()
      return
    }

    await handleSendMessage(option)
  }

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSendMessage()
    }
  }

  const handleNewChat = async () => {
    try {
      // Clear UI immediately for instant feedback
      setStreamingContent('')
      setShowNewChatButton(false)
      setConversationId(null)
      setMessages([])
      setSuggestedOptions([])
      setIsBookingComplete(false)
      setError(null)
      
      // Add greeting immediately
      addMessage("Hello! I'm your AI booking assistant. How can I help you today? You can tell me you want to book an appointment, check availability, or ask any questions.", false, [
        "Book a new appointment",
        "Check availability",
        "View my appointments",
        "Ask a question"
      ])
      
      // Create conversation in background
      const newConversation = await automationService.startNewConversation()
      setConversationId(newConversation.id)
      await loadConversations()
    } catch (error) {
      console.error('Failed to create new conversation:', error)
      setError('Unable to start a new conversation. Please try again.')
    }
  }

  const handleSelectConversation = async (convId) => {
    try {
      setStreamingContent('')
      setShowNewChatButton(false)
      streamingMessageIdRef.current = null
      setConversationId(convId)
      const existingMessages = await automationService.getConversationMessages(convId)
      setMessages(existingMessages.map(msg => ({
        id: msg.id,
        content: msg.content,
        isFromUser: msg.isFromUser,
        suggestedOptions: msg.suggestedOptions,
        selectedOption: msg.selectedOption,
        timestamp: new Date(msg.sentAt)
      })))
      setSuggestedOptions([])
      setIsBookingComplete(false)
      setError(null)
    } catch (error) {
      console.error('Failed to load conversation:', error)
      setError('Unable to load conversation. Please try again.')
    }
  }

  const handleDeleteConversation = async (convId) => {
    try {
      await automationService.deleteConversation(convId)
      const updatedConversations = conversations.filter(conv => conv.id !== convId)
      setConversations(updatedConversations)

      if (conversationId === convId) {
        if (updatedConversations.length > 0) {
          await handleSelectConversation(updatedConversations[0].id)
        } else {
          await handleNewChat()
        }
      }
    } catch (error) {
      console.error('Failed to delete conversation:', error)
      setError('Unable to delete conversation. Please try again.')
    }
  }

  const handleNewBooking = () => {
    handleNewChat()
  }

  const getAvatarUrl = () => user?.avatarUrl || null
  const getDisplayName = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName} ${user.lastName}`
    }
    return user?.userName || user?.email || 'User'
  }

  return (
    <div className="fixed inset-0 mt-16 flex bg-background-app">
      {/* Sidebar - ChatGPT Style */}
      <div className={`${sidebarOpen ? 'w-72' : 'w-0'} bg-gray-900 text-white flex flex-col transition-all duration-300 overflow-hidden`}>
        {/* Sidebar Header */}
        <div className="p-4 border-b border-gray-700">
          <button
            onClick={handleNewChat}
            className="w-full flex items-center gap-3 px-4 py-3 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors text-left"
          >
            <Plus size={20} />
            <span className="font-medium">New chat</span>
          </button>
        </div>

        {/* Conversations List */}
        <div className="flex-1 overflow-y-auto p-3 space-y-2">
          {conversations.map((conv) => (
            <div
              key={conv.id}
              className={`w-full flex items-center gap-3 px-2 py-2 rounded-lg transition-colors group ${
                conversationId === conv.id ? 'bg-gray-700' : 'hover:bg-gray-800'
              }`}
            >
              <button
                onClick={() => handleSelectConversation(conv.id)}
                className="flex-1 min-w-0 flex items-center gap-3 px-2 py-1 text-left"
              >
                <MessageSquare size={18} className="text-gray-400" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm truncate">{conv.title || 'New conversation'}</p>
                  <p className="text-xs text-gray-500">
                    {new Date(conv.startedAt || conv.createdAt).toLocaleDateString()}
                  </p>
                </div>
              </button>
              <button
                onClick={() => handleDeleteConversation(conv.id)}
                className="opacity-100 sm:opacity-0 sm:group-hover:opacity-100 p-1 hover:bg-gray-600 rounded transition-opacity"
                title="Delete chat"
              >
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>

        {/* Sidebar Footer */}
        <div className="p-4 border-t border-gray-700">
          <div className="flex items-center gap-3 px-3 py-2">
            <div className="w-8 h-8 rounded-full bg-primary-accent flex items-center justify-center">
              {getAvatarUrl() ? (
                <img src={getAvatarUrl()} alt={getDisplayName()} className="w-full h-full rounded-full object-cover" />
              ) : (
                <User size={16} className="text-white" />
              )}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium truncate">{getDisplayName()}</p>
              <p className="text-xs text-gray-500 truncate">{user?.email}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col bg-white">
        {/* Header */}
        <div className="p-4 border-b border-gray-200 flex items-center justify-between bg-gradient-to-r from-primary-light/10 to-primary-accent/10">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setSidebarOpen(!sidebarOpen)}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors lg:hidden"
            >
              <MessageSquare size={20} className="text-primary-accent" />
            </button>
            <div className="w-10 h-10 rounded-full bg-primary-accent flex items-center justify-center">
              <Bot size={20} className="text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">AI Booking Assistant</h2>
              <p className="text-sm text-gray-500">Let me help you book an appointment</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => window.history.back()}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
              title="Go Back"
            >
              <House size={20} className="text-primary-accent" />
            </button>
          </div>
        </div>

        {/* Messages Area */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {error && (
            <div className="flex justify-center">
              <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 flex items-center gap-2 text-red-700 max-w-md">
                <X size={18} />
                <span className="text-sm">{error}</span>
              </div>
            </div>
          )}

          {messages.length === 0 && !isLoading && !error && (
            <div className="flex flex-col items-center justify-center h-full text-center">
              <div className="w-20 h-20 rounded-full bg-primary-light/20 flex items-center justify-center mb-6">
                <Sparkles size={40} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Welcome to AI Assistant</h3>
              <p className="text-gray-500 max-w-md mb-6">
                I can help you book appointments, check availability, and answer questions about our services.
                Just type your message below to get started!
              </p>
              <div className="flex flex-wrap gap-2 justify-center">
                {["Book a new appointment", "Check availability", "Ask a question"].map((option, index) => (
                  <button
                    key={index}
                    onClick={() => handleSelectOption(option)}
                    className="px-4 py-2 bg-primary-light/10 text-primary-dark rounded-full text-sm font-medium hover:bg-primary-light/20 transition-colors"
                  >
                    {option}
                  </button>
                ))}
              </div>
            </div>
          )}

          {messages.map((message) => (
            <div
              key={message.id}
              className={`flex gap-3 ${message.isFromUser ? 'justify-end' : 'justify-start'}`}
            >
              {!message.isFromUser && (
                <div className="w-10 h-10 rounded-full bg-primary-accent flex items-center justify-center flex-shrink-0">
                  <Bot size={20} className="text-white" />
                </div>
              )}

              <div className={`flex flex-col ${message.isFromUser ? 'items-end' : 'items-start'} max-w-[70%] lg:max-w-[50%]`}>
                {!message.isFromUser && (
                  <h4 className="text-xs font-medium text-gray-700 mb-1">AI Assistant</h4>
                )}
                <div
                  className={`px-4 py-3 rounded-2xl ${
                    message.isFromUser
                      ? 'bg-primary-accent text-white'
                      : 'bg-gray-100 text-gray-900'
                  }`}
                >
                  <p className="text-sm whitespace-pre-wrap leading-relaxed">{message.content}</p>
                </div>
                
                {message.suggestedOptions && message.suggestedOptions.length > 0 && !message.isFromUser && (
                  <div className="mt-2 flex flex-wrap gap-2">
                    {message.suggestedOptions.map((option, idx) => (
                      <button
                        key={idx}
                        onClick={() => handleSelectOption(option)}
                        className="px-3 py-1.5 bg-white border border-primary-accent/30 text-primary-dark rounded-lg text-sm font-medium hover:bg-primary-light/10 hover:border-primary-accent transition-all shadow-sm"
                      >
                        {option}
                      </button>
                    ))}
                  </div>
                )}
                
                {message.selectedOption && (
                  <div className="text-xs text-gray-500 mt-1 italic ml-2">
                    Selected: {message.selectedOption}
                  </div>
                )}
                
                <div className="text-xs text-gray-400 mt-1 flex items-center gap-1">
                  <Clock size={12} />
                  {message.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </div>
              </div>

              {message.isFromUser && (
                <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center flex-shrink-0">
                  {getAvatarUrl() ? (
                    <img src={getAvatarUrl()} alt={getDisplayName()} className="w-full h-full rounded-full object-cover" />
                  ) : (
                    <User size={20} className="text-gray-600" />
                  )}
                </div>
              )}
            </div>
          ))}

          {/* Streaming response */}
          {isStreaming && streamingContent && !streamingMessageIdRef.current && (
            <div className="flex gap-3">
              <div className="w-10 h-10 rounded-full bg-primary-accent flex items-center justify-center flex-shrink-0">
                <Bot size={20} className="text-white" />
              </div>
              <div className="flex flex-col items-start max-w-[70%] lg:max-w-[50%]">
                <h4 className="text-xs font-medium text-gray-700 mb-1">AI Assistant</h4>
                <div className="px-4 py-3 rounded-2xl bg-gray-100 text-gray-900">
                  {streamingContent === '...' ? (
                    <div className="flex gap-1">
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></span>
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></span>
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></span>
                    </div>
                  ) : (
                    <p className="text-sm whitespace-pre-wrap leading-relaxed">{streamingContent}<span className="inline-block w-2 h-4 bg-primary-accent ml-1 animate-pulse"></span></p>
                  )}
                </div>
              </div>
            </div>
          )}

          {isLoading && !isStreaming && (
            <div className="flex gap-3">
              <div className="w-10 h-10 rounded-full bg-primary-accent flex items-center justify-center flex-shrink-0">
                <Bot size={20} className="text-white" />
              </div>
              <div className="bg-gray-100 rounded-2xl px-4 py-3">
                <Loader2 size={20} className="animate-spin text-primary-accent" />
              </div>
            </div>
          )}

          {isBookingComplete && (
            <div className="flex justify-center">
              <div className="bg-green-50 border border-green-200 rounded-xl px-6 py-4 flex items-center gap-3 text-green-700">
                <CheckCircle size={24} />
                <div>
                  <span className="font-medium">Booking completed successfully!</span>
                  <p className="text-sm text-green-600">You can start a new booking anytime.</p>
                </div>
              </div>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Suggested Options */}
        {suggestedOptions.length > 0 && !isLoading && !isBookingComplete && (
          <div className="px-4 py-2 bg-gray-50 border-t border-gray-200">
            <div className="flex flex-wrap gap-2">
              {suggestedOptions.map((option, index) => (
                <button
                  key={index}
                  onClick={() => handleSelectOption(option)}
                  className="px-4 py-2 bg-white border border-gray-200 text-primary-dark rounded-full text-sm font-medium hover:bg-primary-light/10 hover:border-primary-accent transition-all"
                >
                  {option}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Input Area */}
        <div className="p-4 border-t border-gray-200 bg-white">
          <div className="flex gap-3">
            <input
              type="text"
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder="Type your message..."
              disabled={isLoading || isBookingComplete}
              className="flex-1 px-4 py-3 border border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed text-sm"
            />
            <button
              onClick={handleSendMessage}
              disabled={!inputMessage.trim() || isLoading || isBookingComplete}
              className="px-6 py-3 bg-primary-accent text-white rounded-xl hover:bg-primary-dark transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 font-medium"
            >
              {isLoading ? (
                <Loader2 size={20} className="animate-spin" />
              ) : (
                <Send size={20} />
              )}
              <span className="hidden sm:inline">Send</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}