import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.handlers = new Map()
  }

  async connect(accessToken, hubUrl) {
    // If already connected, just return
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected')
      return
    }

    // If connection exists but not connected, stop it first
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.disconnect()
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => accessToken,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // Register all handlers
    this.handlers.forEach((callback, event) => {
      this.connection.on(event, callback)
    })

    try {
      await this.connection.start()
      console.log('SignalR Connected')
    } catch (err) {
      console.error('SignalR Connection Error:', err)
      throw err
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      console.log('SignalR Disconnected')
      this.connection = null
    }
    // Don't clear handlers - they should persist for reconnects
  }

  on(event, callback) {
    this.handlers.set(event, callback)
    if (this.connection) {
      this.connection.on(event, callback)
    }
  }

  off(event) {
    this.handlers.delete(event)
    if (this.connection) {
      this.connection.off(event)
    }
  }

  async invoke(method, ...args) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return await this.connection.invoke(method, ...args)
    }
    throw new Error('SignalR connection is not established')
  }
}

// Create instances for both hubs
export const chatHubService = new SignalRService()
export const orderHubService = new SignalRService()
export const notificationHubService = new SignalRService()
