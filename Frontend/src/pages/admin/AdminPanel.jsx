import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { adminService } from '../../services/adminService'
import {
  Users,
  UserCheck,
  UserX,
  Activity,
  Search,
  Plus,
  Edit,
  Trash2,
  Shield,
  ShieldOff,
  Key,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'

export const AdminPanel = () => {
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isAdmin = currentUser?.roles?.includes('Admin')

  const [users, setUsers] = useState([])
  const [statistics, setStatistics] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)
  const [selectedUser, setSelectedUser] = useState(null)
  const [showPasswordModal, setShowPasswordModal] = useState(false)
  const [showRoleModal, setShowRoleModal] = useState(false)
  const [editFormData, setEditFormData] = useState({
    email: '',
    userName: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    isActive: true,
  })

  const [formData, setFormData] = useState({
    email: '',
    password: '',
    userName: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    role: 'User',
    isActive: true,
  })

  const [newPassword, setNewPassword] = useState('')
  const [selectedRole, setSelectedRole] = useState('')

  const ITEMS_PER_PAGE = 10

  useEffect(() => {
    if (!isAdmin) return
    loadData()
  }, [isAdmin])

  const loadData = async () => {
    try {
      setLoading(true)
      setLoadError('')
      const [usersData, statsData] = await Promise.all([
        adminService.getAllUsers(token),
        adminService.getUserStatistics(token),
      ])

      if (!Array.isArray(usersData)) {
        throw new Error('Invalid users response format from admin API')
      }

      setUsers(usersData)
      setStatistics(statsData && typeof statsData === 'object' ? statsData : null)
    } catch (error) {
      console.error('Failed to load admin data:', error)
      setUsers([])
      setStatistics(null)
      setLoadError(error?.response?.data?.message || error?.message || 'Failed to load admin data')
    } finally {
      setLoading(false)
    }
  }

  const safeUsers = Array.isArray(users) ? users : []

  const filteredUsers = safeUsers.filter(
    (u) =>
      u.email?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      u.userName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      u.firstName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      u.lastName?.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const totalPages = Math.ceil(filteredUsers.length / ITEMS_PER_PAGE)
  const paginatedUsers = filteredUsers.slice(
    (currentPage - 1) * ITEMS_PER_PAGE,
    currentPage * ITEMS_PER_PAGE
  )

  const handleCreateUser = async (e) => {
    e.preventDefault()
    try {
      await adminService.createUser(formData, token)
      setShowCreateModal(false)
      setFormData({
        email: '',
        password: '',
        userName: '',
        firstName: '',
        lastName: '',
        phoneNumber: '',
        role: 'User',
        isActive: true,
      })
      loadData()
    } catch (error) {
      console.error('Failed to create user:', error)
      alert('Failed to create user. Please try again.')
    }
  }

  const handleToggleActive = async (userId) => {
    try {
      await adminService.toggleUserActiveStatus(userId, token)
      loadData()
    } catch (error) {
      console.error('Failed to toggle user status:', error)
      alert('Failed to update user status. Please try again.')
    }
  }

  const handleOpenEditModal = (selected) => {
    setSelectedUser(selected)
    setEditFormData({
      email: selected.email || '',
      userName: selected.userName || '',
      firstName: selected.firstName || '',
      lastName: selected.lastName || '',
      phoneNumber: selected.phoneNumber || '',
      isActive: selected.isActive,
    })
    setShowEditModal(true)
  }

  const handleEditUser = async (e) => {
    e.preventDefault()
    if (!selectedUser) return

    try {
      await adminService.updateUser(
        selectedUser.id,
        {
          id: selectedUser.id,
          email: editFormData.email,
          userName: editFormData.userName,
          firstName: editFormData.firstName,
          lastName: editFormData.lastName,
          avatarUrl: selectedUser.avatarUrl,
          phoneNumber: editFormData.phoneNumber,
          isActive: editFormData.isActive,
          isOnline: selectedUser.isOnline,
          createdAt: selectedUser.createdAt,
          lastLoginAt: selectedUser.lastLoginAt,
          roles: selectedUser.roles,
        },
        token
      )

      setShowEditModal(false)
      setSelectedUser(null)
      loadData()
    } catch (error) {
      console.error('Failed to update user:', error)
      alert('Failed to update user. Please try again.')
    }
  }

  const handleDeleteUser = async (userId) => {
    if (currentUser?.id === userId) {
      alert('You cannot delete your own admin account.')
      return
    }

    if (!confirm('Are you sure you want to permanently delete this user?')) return

    try {
      await adminService.deleteUser(userId, token)
      loadData()
    } catch (error) {
      console.error('Failed to delete user:', error)
      alert('Failed to delete user. Please try again.')
    }
  }

  const handleAssignRole = async () => {
    if (!selectedUser || !selectedRole) return
    try {
      await adminService.assignRole(selectedUser.id, selectedRole, token)
      setShowRoleModal(false)
      setSelectedRole('')
      loadData()
    } catch (error) {
      console.error('Failed to assign role:', error)
      alert('Failed to assign role. Please try again.')
    }
  }

  const handleRemoveRole = async (roleName) => {
    if (!selectedUser || selectedUser.roles.length <= 1) {
      alert('User must have at least one role.')
      return
    }
    if (!confirm(`Are you sure you want to remove the ${roleName} role?`)) return
    try {
      await adminService.removeRole(selectedUser.id, roleName, token)
      loadData()
    } catch (error) {
      console.error('Failed to remove role:', error)
      alert('Failed to remove role. Please try again.')
    }
  }

  const handleResetPassword = async () => {
    if (!selectedUser || !newPassword) return
    try {
      await adminService.resetUserPassword(selectedUser.id, newPassword, token)
      setShowPasswordModal(false)
      setNewPassword('')
      alert('Password reset successfully!')
      loadData()
    } catch (error) {
      console.error('Failed to reset password:', error)
      alert('Failed to reset password. Please try again.')
    }
  }

  if (!isAdmin) {
    return (
      <MainContent>
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <ShieldOff size={64} className="mx-auto text-red-500 mb-4" />
            <h2 className="text-2xl font-semibold text-text-primary mb-2">Access Denied</h2>
            <p className="text-text-secondary">You don't have permission to access the admin panel.</p>
          </div>
        </div>
      </MainContent>
    )
  }

  return (
    <MainContent>
      <SectionHeader
        title="Admin Panel"
        subtitle="Manage users and system settings"
        action={
          <Button onClick={() => setShowCreateModal(true)} variant="primary">
            <Plus size={18} className="mr-2" />
            Create User
          </Button>
        }
      />

      {/* Statistics Cards */}
      {loadError && (
        <div className="mb-6 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-secondary">Total Users</p>
                  <p className="text-2xl font-bold text-text-primary mt-1">{statistics.totalUsers}</p>
                </div>
                <div className="bg-blue-100 p-3 rounded-full">
                  <Users size={24} className="text-blue-600" />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-secondary">Active Users</p>
                  <p className="text-2xl font-bold text-text-primary mt-1">{statistics.activeUsers}</p>
                </div>
                <div className="bg-green-100 p-3 rounded-full">
                  <UserCheck size={24} className="text-green-600" />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-secondary">Online Now</p>
                  <p className="text-2xl font-bold text-text-primary mt-1">{statistics.onlineUsers}</p>
                </div>
                <div className="bg-green-100 p-3 rounded-full">
                  <Activity size={24} className="text-green-600" />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-text-secondary">Inactive Users</p>
                  <p className="text-2xl font-bold text-text-primary mt-1">{statistics.inactiveUsers}</p>
                </div>
                <div className="bg-red-100 p-3 rounded-full">
                  <UserX size={24} className="text-red-600" />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Users by Role */}
      {statistics && statistics.usersByRole && (
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Users by Role</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
              {Object.entries(statistics.usersByRole).map(([role, count]) => (
                <div key={role} className="text-center p-3 bg-gray-50 rounded-lg">
                  <p className="text-lg font-bold text-text-primary">{count}</p>
                  <p className="text-sm text-text-secondary">{role}</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Users Table */}
      <Card>
        <CardHeader>
          <CardTitle>All Users</CardTitle>
          <div className="mt-4 flex gap-2">
            <div className="relative flex-1">
              <Search size={18} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder="Search users..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <Button onClick={loadData} variant="outline">
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="text-center py-8">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-gray-200">
                      <th className="text-left py-3 px-4 font-medium text-text-secondary">User</th>
                      <th className="text-left py-3 px-4 font-medium text-text-secondary">Email</th>
                      <th className="text-left py-3 px-4 font-medium text-text-secondary">Roles</th>
                      <th className="text-left py-3 px-4 font-medium text-text-secondary">Status</th>
                      <th className="text-left py-3 px-4 font-medium text-text-secondary">Created</th>
                      <th className="text-right py-3 px-4 font-medium text-text-secondary">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {paginatedUsers.map((user) => (
                      <tr key={user.id} className="border-b border-gray-100 hover:bg-gray-50">
                        <td className="py-3 px-4">
                          <div>
                            <p className="font-medium text-text-primary">
                              {user.firstName} {user.lastName}
                            </p>
                            <p className="text-sm text-text-secondary">@{user.userName}</p>
                          </div>
                        </td>
                        <td className="py-3 px-4 text-text-secondary">{user.email}</td>
                        <td className="py-3 px-4">
                          <div className="flex flex-wrap gap-1">
                            {user.roles.map((role) => (
                              <span
                                key={role}
                                className={`px-2 py-1 rounded-full text-xs font-medium ${
                                  role === 'Admin'
                                    ? 'bg-purple-100 text-purple-700'
                                    : role === 'Doctor' || role === 'Professional'
                                    ? 'bg-blue-100 text-blue-700'
                                    : 'bg-gray-100 text-gray-700'
                                }`}
                              >
                                {role}
                              </span>
                            ))}
                          </div>
                        </td>
                        <td className="py-3 px-4">
                          <div className="flex items-center gap-2">
                            <span
                              className={`w-2 h-2 rounded-full ${
                                user.isActive ? 'bg-green-500' : 'bg-red-500'
                              }`}
                            />
                            <span
                              className={`text-sm font-medium ${
                                user.isActive ? 'text-green-600' : 'text-red-600'
                              }`}
                            >
                              {user.isActive ? 'Active' : 'Inactive'}
                            </span>
                            {user.isOnline && (
                              <span className="text-xs text-blue-600 ml-2">(Online)</span>
                            )}
                          </div>
                        </td>
                        <td className="py-3 px-4 text-sm text-text-secondary">
                          {new Date(user.createdAt).toLocaleDateString()}
                        </td>
                        <td className="py-3 px-4">
                          <div className="flex justify-end gap-2">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleOpenEditModal(user)}
                            >
                              <Edit size={16} />
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setSelectedUser(user)
                                setShowRoleModal(true)
                              }}
                            >
                              <Shield size={16} />
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setSelectedUser(user)
                                setShowPasswordModal(true)
                              }}
                            >
                              <Key size={16} />
                            </Button>
                            <Button
                              size="sm"
                              variant={user.isActive ? 'danger' : 'success'}
                              onClick={() => handleToggleActive(user.id)}
                            >
                              {user.isActive ? <UserX size={16} /> : <UserCheck size={16} />}
                            </Button>
                            <Button
                              size="sm"
                              variant="danger"
                              onClick={() => handleDeleteUser(user.id)}
                            >
                              <Trash2 size={16} />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between mt-6">
                  <p className="text-sm text-text-secondary">
                    Showing {(currentPage - 1) * ITEMS_PER_PAGE + 1} to{' '}
                    {Math.min(currentPage * ITEMS_PER_PAGE, filteredUsers.length)} of{' '}
                    {filteredUsers.length} users
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                    >
                      <ChevronLeft size={16} />
                    </Button>
                    <span className="px-3 py-2 text-sm text-text-secondary">
                      Page {currentPage} of {totalPages}
                    </span>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      disabled={currentPage === totalPages}
                    >
                      <ChevronRight size={16} />
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Create User Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
            <h3 className="text-xl font-semibold mb-4">Create New User</h3>
            <form onSubmit={handleCreateUser} className="space-y-4">
              <Input
                label="Email"
                type="email"
                required
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              />
              <Input
                label="Password"
                type="password"
                required
                value={formData.password}
                onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              />
              <Input
                label="Username"
                required
                value={formData.userName}
                onChange={(e) => setFormData({ ...formData, userName: e.target.value })}
              />
              <div className="grid grid-cols-2 gap-4">
                <Input
                  label="First Name"
                  value={formData.firstName}
                  onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                />
                <Input
                  label="Last Name"
                  value={formData.lastName}
                  onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                />
              </div>
              <Input
                label="Phone Number"
                value={formData.phoneNumber}
                onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
              />
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-2">Role</label>
                <select
                  required
                  value={formData.role}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="User">User</option>
                  <option value="Professional">Professional</option>
                  <option value="Doctor">Doctor</option>
                  <option value="Patient">Patient</option>
                  <option value="Jurist">Jurist</option>
                  <option value="Management">Management</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="rounded border-gray-300"
                />
                <label htmlFor="isActive" className="text-sm text-text-secondary">
                  Active User
                </label>
              </div>
              <div className="flex gap-2 pt-4">
                <Button type="submit" variant="primary">
                  Create User
                </Button>
                <Button type="button" variant="outline" onClick={() => setShowCreateModal(false)}>
                  Cancel
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit User Modal */}
      {showEditModal && selectedUser && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
            <h3 className="text-xl font-semibold mb-4">Edit User</h3>
            <form onSubmit={handleEditUser} className="space-y-4">
              <Input
                label="Email"
                type="email"
                required
                value={editFormData.email}
                onChange={(e) => setEditFormData({ ...editFormData, email: e.target.value })}
              />
              <Input
                label="Username"
                required
                value={editFormData.userName}
                onChange={(e) => setEditFormData({ ...editFormData, userName: e.target.value })}
              />
              <div className="grid grid-cols-2 gap-4">
                <Input
                  label="First Name"
                  value={editFormData.firstName}
                  onChange={(e) => setEditFormData({ ...editFormData, firstName: e.target.value })}
                />
                <Input
                  label="Last Name"
                  value={editFormData.lastName}
                  onChange={(e) => setEditFormData({ ...editFormData, lastName: e.target.value })}
                />
              </div>
              <Input
                label="Phone Number"
                value={editFormData.phoneNumber}
                onChange={(e) => setEditFormData({ ...editFormData, phoneNumber: e.target.value })}
              />
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="editIsActive"
                  checked={editFormData.isActive}
                  onChange={(e) => setEditFormData({ ...editFormData, isActive: e.target.checked })}
                  className="rounded border-gray-300"
                />
                <label htmlFor="editIsActive" className="text-sm text-text-secondary">
                  Active User
                </label>
              </div>
              <div className="flex gap-2 pt-4">
                <Button type="submit" variant="primary">
                  Save Changes
                </Button>
                <Button type="button" variant="outline" onClick={() => setShowEditModal(false)}>
                  Cancel
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Role Management Modal */}
      {showRoleModal && selectedUser && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-xl font-semibold mb-4">
              Manage Roles - {selectedUser.userName}
            </h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-2">
                  Assign New Role
                </label>
                <select
                  value={selectedRole}
                  onChange={(e) => setSelectedRole(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select a role...</option>
                  <option value="User">User</option>
                  <option value="Professional">Professional</option>
                  <option value="Doctor">Doctor</option>
                  <option value="Patient">Patient</option>
                  <option value="Jurist">Jurist</option>
                  <option value="Management">Management</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-2">
                  Current Roles
                </label>
                <div className="flex flex-wrap gap-2">
                  {selectedUser.roles.map((role) => (
                    <div key={role} className="flex items-center gap-2 bg-gray-100 px-3 py-2 rounded-lg">
                      <span className="font-medium">{role}</span>
                      {role !== 'User' && (
                        <button
                          onClick={() => handleRemoveRole(role)}
                          className="text-red-500 hover:text-red-700"
                        >
                          <Trash2 size={16} />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex gap-2 pt-4">
                <Button onClick={handleAssignRole} variant="primary" disabled={!selectedRole}>
                  Assign Role
                </Button>
                <Button variant="outline" onClick={() => setShowRoleModal(false)}>
                  Close
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Reset Password Modal */}
      {showPasswordModal && selectedUser && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-xl font-semibold mb-4">
              Reset Password - {selectedUser.userName}
            </h3>
            <div className="space-y-4">
              <Input
                label="New Password"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                minLength={6}
              />
              <div className="flex gap-2 pt-4">
                <Button
                  onClick={handleResetPassword}
                  variant="primary"
                  disabled={newPassword.length < 6}
                >
                  Reset Password
                </Button>
                <Button variant="outline" onClick={() => setShowPasswordModal(false)}>
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </MainContent>
  )
}