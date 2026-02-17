import { requestWithAuthRetry } from './httpClient'

const API_URL = '/api/friends'

class FriendService {
  async sendFriendRequest(addresseeId, token) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${API_URL}/request`,
      data: { addresseeId },
    }, token)
    return response.data
  }

  async acceptFriendRequest(friendshipId, token) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${API_URL}/${friendshipId}/accept`,
    }, token)
    return response.data
  }

  async declineFriendRequest(friendshipId, token) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${API_URL}/${friendshipId}/decline`,
    }, token)
    return response.data
  }

  async getFriends(token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: API_URL,
    }, token)
    return response.data
  }

  async getPendingRequests(token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/requests/pending`,
    }, token)
    return response.data
  }

  async getSentRequests(token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/requests/sent`,
    }, token)
    return response.data
  }

  async getFriendshipStatus(userId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/status/${userId}`,
    }, token)
    return response.data
  }

  async removeFriend(friendshipId, token) {
    const response = await requestWithAuthRetry({
      method: 'delete',
      url: `${API_URL}/${friendshipId}`,
    }, token)
    return response.data
  }

  async getFriendIds(token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/ids`,
    }, token)
    return response.data
  }
}

export const friendService = new FriendService()
