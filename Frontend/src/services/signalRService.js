import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.handlers = new Map()
    this.connectPromise = null
    this.currentHubUrl = null
    this.currentAccessToken = null
  }

  async connect(accessToken, hubUrl) {
    if (!accessToken || !hubUrl) return

    const sameTarget = this.currentHubUrl === hubUrl && this.currentAccessToken === accessToken

    if (this.connectPromise) {
      await this.connectPromise
      return
    }

    // If already connected, just return
    if (this.connection?.state === signalR.HubConnectionState.Connected && sameTarget) {
      console.log('SignalR already connected')
      return
    }

    // If currently connecting/reconnecting to same target, don't start another cycle
    if (this.connection && (
      this.connection.state === signalR.HubConnectionState.Connecting ||
      this.connection.state === signalR.HubConnectionState.Reconnecting
    ) && sameTarget) {
      return
    }

    // If target changed or stale state exists, stop and recreate
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.disconnect()
    }

    this.currentHubUrl = hubUrl
    this.currentAccessToken = accessToken

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => accessToken,
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // Register all handlers
    this.handlers.forEach((callback, event) => {
      this.connection.on(event, callback)
    })

    this.connectPromise = (async () => {
      try {
        await this.connection.start()
        console.log('SignalR Connected')
      } catch (err) {
        console.error('SignalR Connection Error:', err)
        throw err
      } finally {
        this.connectPromise = null
      }
    })()

    await this.connectPromise
  }

  async disconnect() {
    if (this.connectPromise) {
      try {
        await this.connectPromise
      } catch {
        // Ignore connect errors during teardown
      }
    }

    if (this.connection) {
      if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
        await this.connection.stop()
      }
      console.log('SignalR Disconnected')
      this.connection = null
    }
    this.currentHubUrl = null
    this.currentAccessToken = null
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
