# Document Service

A microservice for managing document uploads, storage, and access control in the appointment application.

## Features

### 1. File Upload & Storage Module
- Secure document storage using MinIO (S3-compatible object storage)
- Support for multiple file types: images, videos, audio, PDFs, documents, archives
- Versioning support (document metadata tracking)
- Automatic bucket creation and management

### 2. Document Access Control Module
- Role-based access control (Owner, View, Download, Full)
- Owner can grant/revoke access to other users
- Access logs with timestamps and grantor information
- Secure download endpoint with permission checks

### 3. Document Metadata Module
- Document type classification (Avatar, ChatFile, BookingFile, ProfileDocument, Other)
- Linked entity tracking (Order, Chat, User, Professional)
- Owner information and timestamps
- File size, content type, and original filename tracking

## Architecture

### 5-Layer Architecture

```
Document/
├── DocumentApp.API/          # Layer 1: API Endpoints (Minimal APIs)
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Endpoints/             # Endpoint handlers
│   ├── Services/              # API-specific services
│   └── Program.cs             # Startup & DI configuration
├── DocumentApp.Domain/        # Layer 2: Domain Layer
│   ├── Entity/                # Domain entities (Document, DocumentAccess)
│   ├── Enums/                 # Enums (DocumentType, LinkedEntityType, AccessControlType)
│   └── Interfaces/            # Service interfaces
├── DocumentApp.Repository/    # Layer 3: Repository Interfaces
│   └── Interfaces/            # Repository interfaces
├── DocumentApp.Postgres/      # Layer 4: Data Access
│   ├── Data/                  # DbContext
│   ├── Repositories/          # Repository implementations
│   └── Migrations/            # EF Core migrations
└── DocumentApp.Service/       # Layer 5: Business Logic
    └── Services/              # Service implementations (MinIO, DocumentService)
```

## API Endpoints

### Document Operations

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/documents/upload` | Upload a new document | Required |
| GET | `/api/documents/{id}` | Get document metadata | Required |
| GET | `/api/documents/{id}/download` | Download document file | Required |
| GET | `/api/documents` | Get all documents (admin) | Admin |
| GET | `/api/documents/owner/{ownerId}` | Get documents by owner | Required |
| GET | `/api/documents/linked/{entityType}/{entityId}` | Get documents by linked entity | Required |
| DELETE | `/api/documents/{id}` | Delete document | Owner |
| POST | `/api/documents/{id}/access` | Grant access to document | Owner |
| DELETE | `/api/documents/{id}/access/{userId}` | Revoke access from document | Owner |
| GET | `/api/documents/{id}/presigned-url` | Get presigned URL | Required |

## Document Types

- **Avatar**: User profile avatars
- **ChatFile**: Files shared in chat messages
- **BookingFile**: Documents attached to appointments/orders
- **ProfileDocument**: User profile documents (ID, certificates, etc.)
- **Other**: Miscellaneous documents

## Linked Entity Types

- **Order**: Documents linked to appointment orders
- **Chat**: Documents linked to chat conversations
- **User**: Documents linked to user profiles
- **Professional**: Documents linked to professional profiles

## Access Control Types

- **View**: User can view document metadata
- **Download**: User can download the document
- **Full**: User has full access (view, download, manage)

## Frontend Integration

### Document Service

The `documentService.js` provides the following methods:

```javascript
// Upload document
await documentService.uploadDocument(file, options, token)

// Get document
await documentService.getDocument(documentId, token)

// Download document
await documentService.downloadDocument(documentId, token)
await documentService.downloadAndSave(documentId, fileName, token)

// Get documents
await documentService.getAllDocuments(params, token)
await documentService.getDocumentsByOwner(ownerId, params, token)
await documentService.getDocumentsByLinkedEntity(entityType, entityId, token)

// Access control
await documentService.grantAccess(documentId, userId, accessType, token)
await documentService.revokeAccess(documentId, userId, token)

// Utilities
documentService.formatFileSize(bytes)
documentService.getFileIcon(contentType)
documentService.canPreview(contentType)
```

### Usage Examples

#### Chat File Upload
```jsx
import FileUpload from '../../components/ui/FileUpload'

<FileUpload
  onFileUploaded={(file) => handleFileUploaded(file)}
  documentType="ChatFile"
  linkedEntityType="Chat"
  linkedEntityId={chatId}
/>
```

#### Booking File Upload
```jsx
import FileUpload from '../../components/ui/FileUpload'

<FileUpload
  onFileUploaded={(file) => handleFileUploaded(file)}
  documentType="BookingFile"
  linkedEntityType="Order"
  linkedEntityId={orderId}
/>
```

## Database Schema

### Documents Table
- `id` (GUID, PK)
- `fileName` (VARCHAR 255)
- `originalFileName` (VARCHAR 255)
- `contentType` (VARCHAR 100)
- `fileSize` (BIGINT)
- `minioPath` (VARCHAR 500)
- `minioBucket` (VARCHAR 100)
- `documentType` (INT)
- `linkedEntityType` (INT)
- `linkedEntityId` (GUID, nullable)
- `ownerId` (GUID)
- `ownerName` (VARCHAR 255, nullable)
- `isDeleted` (BOOLEAN)
- `createdAt` (DATETIME)
- `updatedAt` (DATETIME)

### DocumentAccess Table
- `id` (GUID, PK)
- `documentId` (GUID, FK)
- `userId` (GUID, FK)
- `userName` (VARCHAR 255, nullable)
- `accessType` (INT)
- `grantedAt` (DATETIME)
- `grantedBy` (GUID, nullable)

## Configuration

### Environment Variables

```env
# Database
ConnectionStrings__DefaultConnection=Host=document-db;Port=5432;Database=DocumentDb;Username=postgres;Password=postgres

# JWT
Jwt__Secret=your-secret-key
Jwt__Issuer=appointment-app
Jwt__Audience=appointment-app

# MinIO
Minio__Endpoint=minio:9000
Minio__AccessKey=minioadmin
Minio__SecretKey=minioadmin
Minio__UseSSL=false
Minio__Bucket=documents
```

## Deployment

### Using Docker Compose

The Document service is included in the main docker-compose.yml:

```bash
docker-compose up document-service
```

### Manual Deployment

```bash
cd Document
dotnet restore
dotnet build
dotnet run --project DocumentApp.API
```

## Security Features

1. **Authentication**: JWT Bearer token required for all endpoints
2. **Authorization**: Role-based access (Admin, Management, Owner)
3. **File Size Limit**: 10MB maximum file size
4. **Access Control**: Owner-based access control with explicit grants
5. **Secure Storage**: Files stored in MinIO with separate buckets
6. **Audit Trail**: Document access and modifications are tracked

## Admin Features

The Document Management panel in the admin interface provides:

1. **View All Documents**: See all uploaded documents across the system
2. **Filter by Type**: Filter documents by document type
3. **Search**: Search by filename or owner name
4. **Preview**: Preview supported file types (images, PDF, videos, audio)
5. **Download**: Download any document
6. **Delete**: Delete documents (admin or owner only)

## File Preview Support

The following file types can be previewed:

- **Images**: PNG, JPG, GIF, WEBP, SVG, BMP
- **PDF**: PDF documents
- **Videos**: MP4, WebM, etc.
- **Audio**: MP3, WAV, etc.

Other file types can only be downloaded.

## Integration Points

### Chat Service
- File attachments in chat messages
- Files linked to chat conversations
- Real-time file sharing

### Appointment Service
- Booking-related documents (forms, reports, etc.)
- Documents linked to orders
- Patient records and medical documents

### Identity Service
- User profile avatars
- User profile documents (ID, certificates)

## Troubleshooting

### Common Issues

1. **Upload Failed**: Check MinIO connection and bucket permissions
2. **Download Forbidden**: Verify user has access permissions
3. **File Not Found**: Check if file was deleted or MinIO storage is corrupted
4. **Preview Not Working**: Verify file type is supported for preview

## Development

### Adding New Document Types

1. Add enum value to `DocumentType` in `DocumentApp.Domain/Enums/DocumentType.cs`
2. Update `DOCUMENT_TYPE_OPTIONS` in `DocumentManagement.jsx`

### Adding New Access Types

1. Add enum value to `AccessControlType` in `DocumentApp.Domain/Enums/AccessControlType.cs`
2. Update access control logic in `DocumentService.cs`

## License

Part of the Appointment App microservices architecture.