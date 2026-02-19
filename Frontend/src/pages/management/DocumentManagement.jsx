import React, { useEffect, useState } from 'react'
import { useSelector } from 'react-redux'
import { Card, CardContent } from '../../components/ui/Card'
import { Loader } from '../../components/ui/Loader'
import { Avatar } from '../../components/ui/Avatar'
import documentService from '../../services/documentService'
import { FileText, Download, Trash2, Eye, Search, Filter, X, Upload } from 'lucide-react'

const DOCUMENT_TYPE_OPTIONS = [
  { value: '', label: 'All Types' },
  { value: 'Avatar', label: 'Avatar' },
  { value: 'ChatFile', label: 'Chat File' },
  { value: 'BookingFile', label: 'Booking File' },
  { value: 'ProfileDocument', label: 'Profile Document' },
  { value: 'Other', label: 'Other' },
]

const ITEMS_PER_PAGE = 20

export const DocumentManagement = () => {
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)

  const [loading, setLoading] = useState(true)
  const [documents, setDocuments] = useState([])
  const [currentPage, setCurrentPage] = useState(1)
  const [documentTypeFilter, setDocumentTypeFilter] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedDocument, setSelectedDocument] = useState(null)
  const [showPreviewModal, setShowPreviewModal] = useState(false)
  const [previewUrl, setPreviewUrl] = useState(null)

  useEffect(() => {
    loadDocuments()
  }, [token, currentPage, documentTypeFilter])

  const loadDocuments = async () => {
    setLoading(true)
    try {
      const params = {
        page: currentPage,
        pageSize: ITEMS_PER_PAGE,
      }
      if (documentTypeFilter) {
        params.documentType = documentTypeFilter
      }
      const data = await documentService.getAllDocuments(params, token)
      setDocuments(Array.isArray(data) ? data : [])
    } catch (error) {
      console.error('Failed to load documents:', error)
      setDocuments([])
    } finally {
      setLoading(false)
    }
  }

  const handleDownload = async (document) => {
    try {
      await documentService.downloadAndSave(document.id, document.originalFileName, token)
    } catch (error) {
      console.error('Failed to download document:', error)
      alert('Failed to download document')
    }
  }

  const handlePreview = async (document) => {
    if (!documentService.canPreview(document.contentType)) {
      alert('This file type cannot be previewed. Please download it.')
      return
    }

    try {
      const blob = await documentService.downloadDocument(document.id, token)
      const url = window.URL.createObjectURL(blob)
      setPreviewUrl(url)
      setSelectedDocument(document)
      setShowPreviewModal(true)
    } catch (error) {
      console.error('Failed to preview document:', error)
      alert('Failed to preview document')
    }
  }

  const handleDelete = async (document) => {
    if (!confirm(`Are you sure you want to delete "${document.originalFileName}"?`)) {
      return
    }

    try {
      await documentService.deleteDocument(document.id, token)
      loadDocuments()
    } catch (error) {
      console.error('Failed to delete document:', error)
      alert('Failed to delete document')
    }
  }

  const closePreviewModal = () => {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl)
      setPreviewUrl(null)
    }
    setShowPreviewModal(false)
    setSelectedDocument(null)
  }

  const filteredDocuments = documents.filter((doc) => {
    if (!searchQuery) return true
    const query = searchQuery.toLowerCase()
    return (
      doc.originalFileName.toLowerCase().includes(query) ||
      (doc.ownerName && doc.ownerName.toLowerCase().includes(query))
    )
  })

  const totalPages = Math.ceil(filteredDocuments.length / ITEMS_PER_PAGE)
  const paginatedDocuments = filteredDocuments.slice(
    (currentPage - 1) * ITEMS_PER_PAGE,
    currentPage * ITEMS_PER_PAGE
  )

  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex flex-wrap items-center justify-between gap-4 mb-6">
          <div className="flex items-center gap-2">
            <FileText size={18} className="text-primary-dark" />
            <h3 className="text-lg font-semibold text-text-primary">Document Management</h3>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-2">
              <Search size={16} className="text-text-secondary" />
              <input
                type="text"
                placeholder="Search documents..."
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value)
                  setCurrentPage(1)
                }}
                className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
              {searchQuery && (
                <button
                  onClick={() => {
                    setSearchQuery('')
                    setCurrentPage(1)
                  }}
                  className="p-1 hover:bg-gray-100 rounded"
                >
                  <X size={14} />
                </button>
              )}
            </div>

            <div className="flex items-center gap-2">
              <Filter size={16} className="text-text-secondary" />
              <select
                value={documentTypeFilter}
                onChange={(e) => {
                  setDocumentTypeFilter(e.target.value)
                  setCurrentPage(1)
                }}
                className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              >
                {DOCUMENT_TYPE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              {documentTypeFilter && (
                <button
                  onClick={() => {
                    setDocumentTypeFilter('')
                    setCurrentPage(1)
                  }}
                  className="p-1 hover:bg-gray-100 rounded"
                >
                  <X size={14} />
                </button>
              )}
            </div>

            <span className="text-sm text-text-secondary">
              {filteredDocuments.length} documents
            </span>
          </div>
        </div>

        {loading ? (
          <div className="flex justify-center py-12">
            <Loader size="lg" />
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[1000px]">
                <thead>
                  <tr className="border-b border-gray-200 text-sm text-text-secondary">
                    <th className="text-left py-2 pr-2">File</th>
                    <th className="text-left py-2 pr-2">Type</th>
                    <th className="text-left py-2 pr-2">Owner</th>
                    <th className="text-left py-2 pr-2">Size</th>
                    <th className="text-left py-2 pr-2">Uploaded</th>
                    <th className="text-left py-2 pr-2">Linked To</th>
                    <th className="text-right py-2 pr-2">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedDocuments.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="py-8 text-center text-text-secondary">
                        No documents found
                      </td>
                    </tr>
                  ) : (
                    paginatedDocuments.map((doc) => (
                      <tr key={doc.id} className="border-b border-gray-100 text-sm">
                        <td className="py-3 pr-2">
                          <div className="flex items-center gap-2">
                            <span className="text-2xl">{documentService.getFileIcon(doc.contentType)}</span>
                            <div className="max-w-[200px]">
                              <p className="text-text-primary font-medium truncate" title={doc.originalFileName}>
                                {doc.originalFileName}
                              </p>
                              <p className="text-xs text-text-secondary">{doc.contentType}</p>
                            </div>
                          </div>
                        </td>
                        <td className="py-3 pr-2">
                          <span className="px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                            {doc.documentType}
                          </span>
                        </td>
                        <td className="py-3 pr-2">
                          <div className="flex items-center gap-2">
                            <Avatar src={doc.ownerAvatar} alt={doc.ownerName} size={32} />
                            <span className="text-text-primary">{doc.ownerName || 'Unknown'}</span>
                          </div>
                        </td>
                        <td className="py-3 pr-2 text-text-secondary">
                          {documentService.formatFileSize(doc.fileSize)}
                        </td>
                        <td className="py-3 pr-2 text-text-secondary">
                          {new Date(doc.createdAt).toLocaleDateString()}
                        </td>
                        <td className="py-3 pr-2 text-text-secondary">
                          {doc.linkedEntityType && doc.linkedEntityId ? (
                            <span className="text-xs">
                              {doc.linkedEntityType} ({doc.linkedEntityId.slice(0, 8)}...)
                            </span>
                          ) : (
                            <span className="text-xs text-gray-400">None</span>
                          )}
                        </td>
                        <td className="py-3 pr-2 text-right">
                          <div className="flex items-center justify-end gap-2">
                            {documentService.canPreview(doc.contentType) && (
                              <button
                                onClick={() => handlePreview(doc)}
                                className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors"
                                title="Preview"
                              >
                                <Eye size={16} />
                              </button>
                            )}
                            <button
                              onClick={() => handleDownload(doc)}
                              className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors"
                              title="Download"
                            >
                              <Download size={16} />
                            </button>
                            {(currentUser?.roles?.includes('Admin') || doc.ownerId === currentUser?.id) && (
                              <button
                                onClick={() => handleDelete(doc)}
                                className="p-1.5 hover:bg-red-100 text-red-600 rounded-lg transition-colors"
                                title="Delete"
                              >
                                <Trash2 size={16} />
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="flex items-center justify-between mt-4">
                <div className="text-sm text-text-secondary">
                  Showing {(currentPage - 1) * ITEMS_PER_PAGE + 1} to{' '}
                  {Math.min(currentPage * ITEMS_PER_PAGE, filteredDocuments.length)} of {filteredDocuments.length}
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                    className="px-3 py-1 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                  >
                    Previous
                  </button>
                  <span className="text-sm text-text-secondary">
                    Page {currentPage} of {totalPages}
                  </span>
                  <button
                    onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
                    disabled={currentPage === totalPages}
                    className="px-3 py-1 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </CardContent>

      {/* Preview Modal */}
      {showPreviewModal && selectedDocument && previewUrl && (
        <div className="fixed inset-0 bg-gradient-to-br from-slate-900/95 to-slate-800/95 backdrop-blur-sm flex items-center justify-center z-50 p-4 md:p-8 animate-in fade-in duration-200">
          <div className="bg-white rounded-3xl shadow-2xl max-w-5xl w-full max-h-[92vh] overflow-hidden border border-white/10 flex flex-col">
            {/* Header */}
            <div className="flex items-center justify-between p-5 md:p-6 border-b border-gray-100 bg-gradient-to-r from-white to-gray-50">
              <div className="flex items-center gap-3 flex-1 min-w-0">
                <div className="flex-shrink-0 p-2.5 bg-gradient-to-br from-primary-accent to-primary-accent/80 rounded-xl shadow-lg">
                  <FileText size={22} className="text-white" />
                </div>
                <div className="flex-1 min-w-0">
                  <h3 className="text-lg md:text-xl font-bold text-gray-900 truncate leading-tight">
                    {selectedDocument.originalFileName}
                  </h3>
                  <div className="flex items-center gap-3 mt-1">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary-accent/10 text-primary-accent border border-primary-accent/20">
                      {selectedDocument.documentType}
                    </span>
                    <span className="text-xs text-gray-500">
                      {documentService.formatFileSize(selectedDocument.fileSize)}
                    </span>
                  </div>
                </div>
              </div>
              <button
                onClick={closePreviewModal}
                className="flex-shrink-0 p-2.5 hover:bg-gray-100 rounded-xl transition-all duration-200 hover:scale-105 active:scale-95 group"
              >
                <X size={20} className="text-gray-500 group-hover:text-gray-700 transition-colors" />
              </button>
            </div>

            {/* Preview Content */}
            <div className="flex-1 overflow-hidden bg-gradient-to-br from-gray-50 to-gray-100/50">
              <div className="h-full p-4 md:p-6 flex items-center justify-center overflow-auto">
                <div className="relative w-full h-full flex items-center justify-center bg-white rounded-2xl shadow-inner border border-gray-200/50">
                  {selectedDocument.contentType.startsWith('image/') && (
                    <img
                      src={previewUrl}
                      alt={selectedDocument.originalFileName}
                      className="max-w-full max-h-[calc(92vh-220px)] object-contain rounded-lg"
                    />
                  )}
                  {selectedDocument.contentType === 'application/pdf' && (
                    <iframe
                      src={previewUrl}
                      title={selectedDocument.originalFileName}
                      className="w-full h-[calc(92vh-220px)] rounded-lg"
                    />
                  )}
                  {selectedDocument.contentType.startsWith('video/') && (
                    <video
                      src={previewUrl}
                      controls
                      className="max-w-full max-h-[calc(92vh-220px)] rounded-lg shadow-lg"
                    />
                  )}
                  {selectedDocument.contentType.startsWith('audio/') && (
                    <div className="w-full max-w-md p-8">
                      <div className="flex items-center justify-center mb-6">
                        <div className="p-6 bg-gradient-to-br from-primary-accent to-primary-accent/70 rounded-full shadow-2xl animate-pulse">
                          <span className="text-5xl">🎵</span>
                        </div>
                      </div>
                      <audio src={previewUrl} controls className="w-full" />
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="flex items-center justify-between p-4 md:p-5 border-t border-gray-100 bg-white/80 backdrop-blur-sm">
              <div className="flex items-center gap-4 text-sm">
                <div className="flex items-center gap-2 text-gray-600">
                  <FileText size={16} className="text-primary-accent" />
                  <span className="font-medium">{selectedDocument.contentType}</span>
                </div>
                <span className="text-gray-300">|</span>
                <span className="text-gray-500">
                  {new Date(selectedDocument.createdAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric'
                  })}
                </span>
              </div>
              <div className="flex gap-3">
                <button
                  onClick={() => handleDownload(selectedDocument)}
                  className="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-primary-accent to-primary-accent/90 text-white font-medium rounded-xl hover:from-primary-accent hover:to-primary-accent transition-all duration-200 hover:scale-105 active:scale-95 shadow-lg shadow-primary-accent/25"
                >
                  <Download size={18} />
                  <span className="hidden sm:inline">Download</span>
                </button>
                <button
                  onClick={closePreviewModal}
                  className="flex items-center gap-2 px-5 py-2.5 bg-gray-100 text-gray-700 font-medium rounded-xl hover:bg-gray-200 transition-all duration-200 hover:scale-105 active:scale-95"
                >
                  <span className="hidden sm:inline">Close</span>
                  <X size={18} className="sm:hidden" />
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </Card>
  )
}