import React, { useEffect, useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useSelector } from 'react-redux'
import documentService from '../../services/documentService'
import { FileText, Download, ArrowLeft, X } from 'lucide-react'

export const DocumentPreview = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const token = useSelector((state) => state.auth.token)
  const [previewUrl, setPreviewUrl] = useState(null)
  const [document, setDocument] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    const docData = location.state?.document
    if (!docData) {
      setError('No document data provided')
      setLoading(false)
      return
    }

    setDocument(docData)
    loadDocumentPreview(docData)
  }, [location.state])

  const loadDocumentPreview = async (doc) => {
    setLoading(true)
    try {
      if (!documentService.canPreview(doc.contentType)) {
        setError('This file type cannot be previewed. Please download it.')
        setLoading(false)
        return
      }

      const blob = await documentService.downloadDocument(doc.id, token)
      const url = window.URL.createObjectURL(blob)
      setPreviewUrl(url)
    } catch (err) {
      console.error('Failed to load document:', err)
      setError('Failed to load document')
    } finally {
      setLoading(false)
    }
  }

  const handleDownload = async () => {
    if (!document) return
    try {
      await documentService.downloadAndSave(document.id, document.originalFileName, token)
    } catch (error) {
      console.error('Failed to download document:', error)
      alert('Failed to download document')
    }
  }

  const handleBack = () => {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl)
    }
    navigate('/management', { state: { activeTab: 'documents' } })
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 max-w-md w-full text-center">
          <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <X size={32} className="text-red-600" />
          </div>
          <h2 className="text-xl font-bold text-gray-900 mb-2">Preview Error</h2>
          <p className="text-gray-600 mb-6">{error}</p>
          <button
            onClick={handleBack}
            className="inline-flex items-center gap-2 px-6 py-3 bg-gray-900 text-white font-medium rounded-xl hover:bg-gray-800 transition-colors"
          >
            <ArrowLeft size={18} />
            Back to Documents
          </button>
        </div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-primary-accent border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-gray-600">Loading document...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 sticky top-0 z-10 shadow-sm">
        <div className="max-w-full mx-auto px-4 md:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16 md:h-20">
            <div className="flex items-center gap-4 flex-1 min-w-0">
              <button
                onClick={handleBack}
                className="flex-shrink-0 p-2 hover:bg-gray-100 rounded-xl transition-colors"
                title="Back to documents"
              >
                <ArrowLeft size={20} className="text-gray-600" />
              </button>
              <div className="flex items-center gap-3 flex-1 min-w-0">
                <div className="flex-shrink-0 p-2 bg-gradient-to-br from-primary-accent to-primary-accent/80 rounded-xl">
                  <FileText size={20} className="text-white" />
                </div>
                <div className="flex-1 min-w-0">
                  <h1 className="text-lg md:text-xl font-bold text-gray-900 truncate">
                    {document?.originalFileName}
                  </h1>
                  <div className="flex items-center gap-3 mt-0.5">
                    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-primary-accent/10 text-primary-accent border border-primary-accent/20">
                      {document?.documentType}
                    </span>
                    <span className="text-xs text-gray-500">
                      {documentService.formatFileSize(document?.fileSize || 0)}
                    </span>
                  </div>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <button
                onClick={handleDownload}
                className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-primary-accent to-primary-accent/90 text-white font-medium rounded-xl hover:from-primary-accent hover:to-primary-accent transition-all duration-200 hover:scale-105 active:scale-95 shadow-lg shadow-primary-accent/25"
              >
                <Download size={18} />
                <span className="hidden sm:inline">Download</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Preview Content */}
      <div className="flex-1 overflow-hidden bg-gradient-to-br from-gray-50 to-gray-100/50">
        <div className="h-full flex items-center justify-center p-4 md:p-6 lg:p-8">
          <div className="relative w-full h-full flex items-center justify-center bg-white rounded-2xl shadow-inner border border-gray-200/50">
            {document?.contentType.startsWith('image/') && (
              <img
                src={previewUrl}
                alt={document.originalFileName}
                className="max-w-full max-h-[calc(100vh-120px)] object-contain rounded-lg"
              />
            )}
            {document?.contentType === 'application/pdf' && (
              <iframe
                src={previewUrl}
                title={document.originalFileName}
                className="w-full h-[calc(100vh-120px)] rounded-lg border-0"
              />
            )}
            {document?.contentType.startsWith('video/') && (
              <video
                src={previewUrl}
                controls
                className="max-w-full max-h-[calc(100vh-120px)] rounded-lg shadow-lg"
              />
            )}
            {document?.contentType.startsWith('audio/') && (
              <div className="w-full max-w-md p-8 text-center">
                <div className="flex items-center justify-center mb-6">
                  <div className="p-8 bg-gradient-to-br from-primary-accent to-primary-accent/70 rounded-full shadow-2xl animate-pulse">
                    <span className="text-6xl">🎵</span>
                  </div>
                </div>
                <h3 className="text-xl font-bold text-gray-900 mb-4">
                  {document.originalFileName}
                </h3>
                <audio src={previewUrl} controls className="w-full" />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}