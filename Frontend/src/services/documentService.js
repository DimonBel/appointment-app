import { requestWithAuthRetry } from './httpClient'

const DOCUMENT_API_BASE = '/api/documents'

// Enum mapping for LinkedEntityType (matches backend enum)
const LinkedEntityType = {
  None: 0,
  Order: 1,
  Chat: 2,
  User: 3,
  Professional: 4
}

// Helper to convert string to enum value
const mapLinkedEntityType = (type) => {
  if (typeof type === 'number') return type
  return LinkedEntityType[type] || LinkedEntityType.None
}

/**
 * Document Service for file upload and management
 */
class DocumentService {
  /**
   * Update document's linked entity
   * @param {string} documentId - Document ID
   * @param {string} linkedEntityType - Linked entity type (Order, Chat, User, Professional)
   * @param {string} linkedEntityId - ID of linked entity
   * @param {string} token - Auth token
   * @returns {Promise<Object>} Updated document info
   */
  async updateDocumentLinkedEntity(documentId, linkedEntityType, linkedEntityId, token) {
    const payload = {
      LinkedEntityType: mapLinkedEntityType(linkedEntityType),
      LinkedEntityId: linkedEntityId
    }
    console.log('updateDocumentLinkedEntity called:', { documentId, payload })
    const result = await requestWithAuthRetry({
      method: 'patch',
      url: `${DOCUMENT_API_BASE}/${documentId}/linked-entity`,
      data: payload
    }, token)
    console.log('updateDocumentLinkedEntity result:', result)
    return result
  }

  /**
   * Upload a document
   * @param {File} file - The file to upload
   * @param {Object} options - Upload options
   * @param {string} options.documentType - Type of document (Avatar, ChatFile, BookingFile, ProfileDocument, Other)
   * @param {string} options.linkedEntityType - Linked entity type (Order, Chat, User, Professional)
   * @param {string} options.linkedEntityId - ID of linked entity
   * @param {string} token - Auth token
   * @returns {Promise<Object>} Uploaded document info
   */
  async uploadDocument(file, options, token) {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('documentType', options.documentType || 'Other')
    
    if (options.linkedEntityType) {
      formData.append('linkedEntityType', mapLinkedEntityType(options.linkedEntityType))
    }
    if (options.linkedEntityId) {
      formData.append('linkedEntityId', options.linkedEntityId)
    }

    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${DOCUMENT_API_BASE}/upload`,
      data: formData,
      headers: { 'Content-Type': 'multipart/form-data' }
    }, token)

    console.log('Document upload response:', response)
    return response.data
  }

  /**
   * Get document by ID
   * @param {string} documentId - Document ID
   * @param {string} token - Auth token
   * @returns {Promise<Object>} Document info
   */
  async getDocument(documentId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${DOCUMENT_API_BASE}/${documentId}`
    }, token)
    return response.data
  }

  /**
   * Download document
   * @param {string} documentId - Document ID
   * @param {string} token - Auth token
   * @returns {Promise<Blob>} Document file as blob
   */
  async downloadDocument(documentId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${DOCUMENT_API_BASE}/${documentId}/download`,
      responseType: 'blob'
    }, token)
    return response
  }

  /**
   * Get all documents (admin/management only)
   * @param {Object} params - Query parameters
   * @param {number} params.page - Page number
   * @param {number} params.pageSize - Page size
   * @param {string} params.documentType - Filter by document type
   * @param {string} token - Auth token
   * @returns {Promise<Array>} List of documents
   */
  async getAllDocuments(params = {}, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: DOCUMENT_API_BASE,
      params
    }, token)
    return response.data
  }

  /**
   * Get documents by owner
   * @param {string} ownerId - User ID
   * @param {Object} params - Query parameters
   * @param {number} params.page - Page number
   * @param {number} params.pageSize - Page size
   * @param {string} token - Auth token
   * @returns {Promise<Array>} List of documents
   */
  async getDocumentsByOwner(ownerId, params = {}, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${DOCUMENT_API_BASE}/owner/${ownerId}`,
      params
    }, token)
    return response.data
  }

  /**
   * Get documents by linked entity
   * @param {string} entityType - Entity type (Order, Chat, User, Professional)
   * @param {string} entityId - Entity ID
   * @param {string} token - Auth token
   * @returns {Promise<Array>} List of documents
   */
  async getDocumentsByLinkedEntity(entityType, entityId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${DOCUMENT_API_BASE}/linked/${entityType}/${entityId}`
    }, token)
    return response.data
  }

  /**
   * Delete document
   * @param {string} documentId - Document ID
   * @param {string} token - Auth token
   * @returns {Promise<void>}
   */
  async deleteDocument(documentId, token) {
    return await requestWithAuthRetry({
      method: 'delete',
      url: `${DOCUMENT_API_BASE}/${documentId}`
    }, token)
  }

  /**
   * Grant access to document
   * @param {string} documentId - Document ID
   * @param {string} userId - User ID to grant access to
   * @param {string} accessType - Access type (View, Download, Full)
   * @param {string} token - Auth token
   * @returns {Promise<void>}
   */
  async grantAccess(documentId, userId, accessType, token) {
    return await requestWithAuthRetry({
      method: 'post',
      url: `${DOCUMENT_API_BASE}/${documentId}/access`,
      data: { userId, accessType }
    }, token)
  }

  /**
   * Revoke access from document
   * @param {string} documentId - Document ID
   * @param {string} userId - User ID to revoke access from
   * @param {string} token - Auth token
   * @returns {Promise<void>}
   */
  async revokeAccess(documentId, userId, token) {
    return await requestWithAuthRetry({
      method: 'delete',
      url: `${DOCUMENT_API_BASE}/${documentId}/access/${userId}`
    }, token)
  }

  /**
   * Get presigned URL for document (for direct download)
   * @param {string} documentId - Document ID
   * @param {string} token - Auth token
   * @returns {Promise<Object>} Object with downloadUrl
   */
  async getPresignedUrl(documentId, token) {
    return await requestWithAuthRetry({
      method: 'get',
      url: `${DOCUMENT_API_BASE}/${documentId}/presigned-url`
    }, token)
  }

  /**
   * Format file size to human readable format
   * @param {number} bytes - File size in bytes
   * @returns {string} Formatted file size
   */
  formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  /**
   * Get file icon based on content type
   * @param {string} contentType - MIME type
   * @returns {string} Icon name or emoji
   */
  getFileIcon(contentType) {
    if (!contentType) return '📄'
    
    if (contentType.startsWith('image/')) return '🖼️'
    if (contentType.startsWith('video/')) return '🎬'
    if (contentType.startsWith('audio/')) return '🎵'
    if (contentType.includes('pdf')) return '📕'
    if (contentType.includes('word') || contentType.includes('document')) return '📘'
    if (contentType.includes('excel') || contentType.includes('spreadsheet')) return '📗'
    if (contentType.includes('powerpoint') || contentType.includes('presentation')) return '📙'
    if (contentType.includes('zip') || contentType.includes('compressed')) return '📦'
    if (contentType.includes('text')) return '📝'
    
    return '📄'
  }

  /**
   * Check if file can be previewed
   * @param {string} contentType - MIME type
   * @returns {boolean}
   */
  canPreview(contentType) {
    if (!contentType) return false
    return contentType.startsWith('image/') || 
           contentType.startsWith('video/') || 
           contentType.startsWith('audio/') ||
           contentType === 'application/pdf'
  }

  /**
   * Download document and trigger browser download
   * @param {string} documentId - Document ID
   * @param {string} fileName - File name to save as
   * @param {string} token - Auth token
   * @returns {Promise<void>}
   */
  async downloadAndSave(documentId, fileName, token) {
    const blob = await this.downloadDocument(documentId, token)
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName || 'download'
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  }
}

export default new DocumentService()