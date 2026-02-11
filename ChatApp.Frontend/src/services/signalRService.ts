import * as signalR from '@microsoft/signalr';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private messageHandlers: Map<string, (data: any) => void> = new Map();
  private connectionHandlers: ((connected: boolean) => void)[] = [];
  private connectionPromise: Promise<void> | null = null;

  constructor() {
    // Don't connect automatically in constructor
  }

  public async connect(): Promise<void> {
    console.log('🔄 Attempting to connect to SignalR...');

    if (this.connectionPromise) {
      console.log('⏳ Connection already in progress');
      return this.connectionPromise;
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('✅ Already connected to SignalR');
      return;
    }

    this.connectionPromise = this.establishConnection();
    return this.connectionPromise;
  }

  private async establishConnection() {
    const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5157';
    
    // Wait a bit for cookies to be properly set after login
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Get auth token for additional security
    const token = localStorage.getItem('token');
    
    // Configure connection options with both cookie and token auth
    const connectionOptions: signalR.IHttpConnectionOptions = {
      skipNegotiation: false,
      withCredentials: true, // Important: This sends authentication cookies
      accessTokenFactory: () => token || '', // Provide token as backup
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
    };
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/chathub`, connectionOptions)
      .withAutomaticReconnect([0, 2000, 10000, 30000]) // Custom retry intervals
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();

    try {
      console.log('🔌 Starting SignalR connection...');
      await this.connection.start();
      console.log('✅ SignalR connected successfully');
      console.log('🔗 Connection ID:', this.connection.connectionId);
      this.connectionHandlers.forEach(handler => handler(true));
    } catch (error) {
      console.warn('❌ SignalR connection failed:', error);
      console.log('🔍 Debug info - State:', this.connection?.state);
      console.log('🍪 Cookies available:', document.cookie);
      console.log('🔑 Token available:', !!localStorage.getItem('token'));
      this.connectionHandlers.forEach(handler => handler(false));
      this.connectionPromise = null;
      
      // Don't throw error, just log it - allow fallback to REST API
      console.log('⚠️ Will continue with REST API fallback for messaging');
    }
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.on('ReceiveMessage', (senderId: string, message: string, messageId: string, createdAt: string) => {
      console.log(`📥 Received SignalR message from ${senderId}: ${message} (ID: ${messageId})`);
      const handler = this.messageHandlers.get('ReceiveMessage');
      if (handler) {
        handler({ senderId, message, messageId, createdAt });
      } else {
        console.warn('⚠️ No handler registered for ReceiveMessage');
      }
    });

    this.connection.on('MessageSent', (receiverId: string, message: string, messageId: string, createdAt: string) => {
      console.log(`✅ Message sent confirmation to ${receiverId}: ${message} (ID: ${messageId})`);
      const handler = this.messageHandlers.get('MessageSent');
      if (handler) {
        handler({ receiverId, message, messageId, createdAt });
      } else {
        console.warn('⚠️ No handler registered for MessageSent');
      }
    });

    this.connection.on('MessageError', (error: any) => {
      console.error('❌ SignalR message error:', error);
      const handler = this.messageHandlers.get('MessageError');
      if (handler) {
        handler({ error });
      }
    });

    this.connection.on('UserTyping', (senderId: string) => {
      const handler = this.messageHandlers.get('UserTyping');
      if (handler) {
        handler({ senderId });
      }
    });

    this.connection.on('UserOnline', (userId: string) => {
      const handler = this.messageHandlers.get('UserOnline');
      if (handler) {
        handler({ userId });
      }
    });

    this.connection.on('UserOffline', (userId: string) => {
      const handler = this.messageHandlers.get('UserOffline');
      if (handler) {
        handler({ userId });
      }
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
      this.connectionHandlers.forEach(handler => handler(false));
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectionHandlers.forEach(handler => handler(true));
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectionHandlers.forEach(handler => handler(false));
    });
  }

  public async sendMessage(receiverId: string, message: string) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        console.log(`📤 Sending SignalR message to ${receiverId}: ${message}`);
        await this.connection.invoke('SendMessage', receiverId, message);
        console.log('✅ SignalR message sent successfully');
      } catch (error) {
        console.error('❌ Error sending SignalR message:', error);
        throw error;
      }
    } else {
      console.warn('⚠️ Cannot send SignalR message - not connected. State:', this.connection?.state);
      throw new Error('SignalR not connected');
    }
  }

  public async sendTypingNotification(receiverId: string) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('SendTypingNotification', receiverId);
      } catch (error) {
        console.error('Error sending typing notification:', error);
      }
    }
  }

  public onMessage(event: string, handler: (data: any) => void) {
    this.messageHandlers.set(event, handler);
  }

  public onConnectionChange(handler: (connected: boolean) => void) {
    this.connectionHandlers.push(handler);
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state || signalR.HubConnectionState.Disconnected;
  }

  public async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();