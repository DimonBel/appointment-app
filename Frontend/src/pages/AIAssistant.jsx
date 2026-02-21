import React, { useState, useEffect, useRef } from 'react'
import { Send, Bot, User, Loader2, Sparkles, Calendar, Clock, CheckCircle, X, House } from 'lucide-react'
import { automationService } from '../services/automationService'
import { useSelector } from 'react-redux'

export const AIAssistant = () => {
  const [messages, setMessages] = useState([])
  const [inputMessage, setInputMessage] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [conversationId, setConversationId] = useState(null)
  const [suggestedOptions, setSuggestedOptions] = useState([])
  const [isBookingComplete, setIsBookingComplete] = useState(false)
  const [error, setError] = useState(null)
  const messagesEndRef = useRef(null)
  const token = useSelector((state) => state.auth.token)
  const user = useSelector((state) => state.auth.user)

  // Initialize conversation
  useEffect(() => {
    if (token) {
      initializeConversation()
    }
  }, [token])

  // Scroll to bottom when messages change
  useEffect(() => {
    scrollToBottom()
  }, [messages])

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
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

  const handleSendMessage = async () => {
    if (!inputMessage.trim() || isLoading) return

    const userMessage = inputMessage.trim()
    setInputMessage('')
    setSuggestedOptions([])
    setError(null)

    // Add user message immediately
    addMessage(userMessage, true)

    setIsLoading(true)
    try {
      const response = await automationService.sendMessage(userMessage, conversationId)
      
      // Update conversation ID if new
      if (response.conversationId && !conversationId) {
        setConversationId(response.conversationId)
      }

      // Add AI response
      addMessage(response.responseText, false, response.suggestedOptions)
      setSuggestedOptions(response.suggestedOptions || [])
      setIsBookingComplete(response.isBookingComplete || false)
    } catch (error) {
      console.error('Failed to send message:', error)
      setError('Failed to send message. Please try again.')
      addMessage("Sorry, I'm having trouble connecting. Please try again.", false)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSelectOption = async (option) => {
    setInputMessage(option)
    // Auto-send the selected option
    await handleSendMessage()
  }

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSendMessage()
    }
  }

  const handleNewBooking = () => {
    setConversationId(null)
    setMessages([])
    setSuggestedOptions([])
    setIsBookingComplete(false)
    setError(null)
    initializeConversation()
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
      {/* Main Chat Area - Full Width */}
      <div className="flex-1 flex flex-col bg-white">
        {/* Header */}
        <div className="p-4 border-b border-gray-200 flex items-center justify-between bg-gradient-to-r from-primary-light/10 to-primary-accent/10">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-full bg-primary-accent flex items-center justify-center">
              <Bot size={24} className="text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">AI Booking Assistant</h2>
              <p className="text-sm text-gray-500">Let me help you book an appointment</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {isBookingComplete && (
              <button
                onClick={handleNewBooking}
                className="flex items-center gap-2 px-4 py-2 bg-primary-accent text-white rounded-lg hover:bg-primary-dark transition-colors text-sm font-medium"
              >
                <Calendar size={16} />
                New Booking
              </button>
            )}
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

          {isLoading && (
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