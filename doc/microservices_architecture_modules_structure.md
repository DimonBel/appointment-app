# Medical Appointment Platform – Microservices & Modules

This document describes **all microservices**, their **internal modules**, and **responsibilities**, based on the original project specification and the **latest updates after the meeting**.

The system is designed to be **scalable**, **domain-agnostic** (medical, legal, etc.), and **secure**, using **Microsoft Identity** for authentication.

---

## 1. Appointment / Order Service

> Core service responsible for managing bookings (appointments/orders) across multiple domains (medical, legal, etc.).

### Modules

#### 1.1 Order Management Module
- Create, update, cancel, and retrieve appointments/orders
- Supports multiple domains (doctor, jurist, consultant, etc.)
- Handles order lifecycle: `REQUESTED → APPROVED → DECLINED → CANCELLED`

#### 1.2 Availability & Schedule Module
- Professionals define working days and time slots
- Supports recurring schedules
- Validates availability before booking

#### 1.3 Order Approval Module
- Approve or decline requests
- Allows adding decline/approval reasons
- Emits events for notifications and chat updates

#### 1.4 Domain Configuration Module
- Configures **who can receive orders** (doctor, jurist, other specialists)
- Allows adding new professional types without changing core logic

#### 1.5 Pre-Order Data Collection Module
- Requests preliminary data from clients (via Chat Service)
- Validates completeness before order confirmation

#### 1.6 Order History & Audit Module
- Stores full order history
- Keeps audit logs for status changes

---

## 2. Notification Service

> Centralized service for managing all system notifications.

### Modules

#### 2.1 Notification Preferences Module
- Users configure which notifications they want to receive
- Supports channel-level control (email, in-app, push)

#### 2.2 Notification Delivery Module
- Sends notifications triggered by system events
- Supports async processing

#### 2.3 Notification Schedule Module
- Custom notification timing (reminders, delays)
- Configurable per user or per order type

#### 2.4 Template Management Module
- Manages notification templates
- Dynamic placeholders (date, name, order status)

#### 2.5 Event Listener Module
- Listens to events from other microservices
- Converts events into notifications

---

## 3. Chat Service

> Real-time and asynchronous communication layer.

### Modules

#### 3.1 One-to-One Chat Module
- Secure patient–professional messaging
- Chat linked to specific orders

#### 3.2 Message Persistence Module
- Stores messages with metadata
- Supports search and history

#### 3.3 Automated Chat Workflow Module
- Handles system-generated messages
- Requests documents or missing data automatically

#### 3.4 File & Document Sharing Module
- Upload and send documents in chat
- Secure access control

#### 3.5 Chat Context Module
- Maintains conversation context
- Used by AI/Bot service

---

## 4. AI / Bot Service (LLM-powered)

> Intelligent assistant that automates booking and communication.

### Modules

#### 4.1 Conversational Flow Module
- Guides users through booking
- Handles FAQ and order-related questions

#### 4.2 Booking Automation Module
- Creates orders automatically via Appointment Service
- Validates availability and rules

#### 4.3 Rule-Based Logic Module (Initial Phase)
- If/else logic for simple automation
- Acts as fallback if AI fails

#### 4.4 LLM Integration Module
- Connects to Large Language Models
- Natural conversation and intent detection

#### 4.5 Data Collection Module
- Collects preliminary client information
- Sends structured data to other services

---

## 5. Document Service

> Manages documents exchanged during orders and chats.

### Modules

#### 5.1 File Upload & Storage Module
- Secure document storage
- Versioning support

#### 5.2 Document Generation Module
- Auto-generates documents based on UI fields
- Uses predefined templates

#### 5.3 Document Access Control Module
- Ensures only authorized users can view files

#### 5.4 Document Metadata Module
- Stores document type, owner, linked order

---

## 6. User & Identity Service

> Central authentication and authorization service.

### Modules

#### 6.1 Authentication Module
- Uses **Microsoft Identity**
- OAuth2 / OpenID Connect

#### 6.2 Authorization & Roles Module
- Role-based access (patient, doctor, jurist, admin)
- Fine-grained permissions

#### 6.3 User Profile Module
- Stores user information and preferences
- Domain-specific profile extensions

#### 6.4 Security & Token Module
- Token validation
- Secure service-to-service communication

---

## 7. API Gateway

> Single entry point for frontend applications.

### Modules

#### 7.1 Routing Module
- Routes requests to correct microservice

#### 7.2 Authentication Middleware
- Validates tokens via Identity Service

#### 7.3 Rate Limiting & Throttling Module
- Prevents abuse

#### 7.4 Aggregation Module
- Combines data from multiple services

---

## 8. Event & Integration Layer (Optional but Recommended)

> Enables loose coupling and scalability.

### Modules

#### 8.1 Event Publisher Module
- Publishes domain events

#### 8.2 Event Consumer Module
- Subscribes to relevant events

#### 8.3 Retry & Dead Letter Module
- Handles failed messages safely

---

## Summary

This architecture:
- Supports **multiple professional domains**
- Enables **full automation via AI**
- Provides **fine-grained notification control**
- Ensures **enterprise-level security**
- Is **scalable and extensible by design**

This document can be used as:
- Architecture documentation
- Internship / university project deliverable
- Base for UML, C4, or deployment diagrams

