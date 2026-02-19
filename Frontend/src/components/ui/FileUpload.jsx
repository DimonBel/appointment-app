import React, { useRef, useState } from 'react'
import { useSelector } from 'react-redux'
import { Upload, X, FileText, Image as ImageIcon, Video, Music, FileArchive } from 'lucide-react'
import documentService from '../../services/documentService'

const FileUpload = ({ onFileUploaded, documentType = 'ChatFile', linkedEntityType, linkedEntityId, disabled = false }) => {
  const token = useSelector((state) => state.auth.token)
  const fileInputRef = useRef(null)
  const [uploading, setUploading] = useState(false)
  const [uploadedFile, setUploadedFile] = useState(null)

  const handleFileSelect = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    // Check file size (max 10MB)
    const MAX_FILE_SIZE = 10 * 1024 * 1024
    if (file.size > MAX_FILE_SIZE) {
      alert('File size must be less than 10MB')
      return
    }

    setUploading(true)
    try {
      const result = await documentService.uploadDocument(
        file,
        {
          documentType,
          linkedEntityType,
          linkedEntityId
        },
        token
      )

      setUploadedFile(result)
      onFileUploaded?.(result)
    } catch (error) {
      console.error('Failed to upload file:', error)
      alert('Failed to upload file. Please try again.')
    } finally {
      setUploading(false)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  const handleRemoveFile = () => {
    setUploadedFile(null)
    onFileUploaded?.(null)
  }

  const getFileIcon = (contentType) => {
    return documentService.getFileIcon(contentType)
  }

  return (
    <div className="relative">
      <input
        ref={fileInputRef}
        type="file"
        onChange={handleFileSelect}
        className="hidden"
        accept="image/*,video/*,audio/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar"
        disabled={disabled || uploading}
      />

      {!uploadedFile ? (
        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={disabled || uploading}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
            disabled || uploading
              ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          {uploading ? (
            <>
              <div className="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin" />
              Uploading...
            </>
          ) : (
            <>
              <Upload size={18} />
              Upload File
            </>
          )}
        </button>
      ) : (
        <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200">
          <span className="text-2xl">{getFileIcon(uploadedFile.contentType)}</span>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-text-primary truncate">
              {uploadedFile.originalFileName}
            </p>
            <p className="text-xs text-text-secondary">
              {documentService.formatFileSize(uploadedFile.fileSize)}
            </p>
          </div>
          <button
            onClick={handleRemoveFile}
            disabled={disabled}
            className="p-1 hover:bg-gray-200 rounded transition-colors text-gray-500 hover:text-gray-700"
          >
            <X size={18} />
          </button>
        </div>
      )}
    </div>
  )
}

export default FileUpload