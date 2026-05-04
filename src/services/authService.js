import axiosClient from './axiosClient'
import api from './api'

export const authService = {
  /**
   * Đăng nhập user
   * @param {string} username 
   * @param {string} password 
   */
  async login(username, password) {
    const trimmedUsername = username?.trim() || ''
    const trimmedPassword = password?.trim() || ''

    if (!trimmedUsername || !trimmedPassword) {
      return {
        success: false,
        message: 'Tên đăng nhập và mật khẩu là bắt buộc.'
      }
    }

    console.log('🔐 authService.login called with:', { username: trimmedUsername })
    console.log('📍 API Endpoint:', axiosClient.defaults.baseURL)

    try {
      const response = await api.auth.login({ username: trimmedUsername, password: trimmedPassword })

      let data = null
      if (!response) {
        throw new Error('No response from auth API')
      }

      if (response.success && response.data) {
        data = response.data
      } else if (response.data && (response.data.token || response.data.accessToken)) {
        data = response.data
      } else if (response.token || response.accessToken) {
        data = response
      } else {
        data = response
      }

      if (!data.token && data.accessToken) {
        data.token = data.accessToken
      }

      if (!data.refreshToken) {
        data.refreshToken = `refresh-${Date.now()}`
        console.warn('⚠️ API did not return refreshToken, using generated one')
      }

      if (!data.employeeId) {
        data.employeeId = trimmedUsername.toUpperCase()
      }
      if (!data.startDate) {
        data.startDate = new Date().toISOString().split('T')[0]
      }

      if (data && data.token) {
        return { success: true, data }
      }

      return { success: false, message: response.message || 'Đăng nhập thất bại (không có token)' }
    } catch (error) {
      console.error('❌ Login error:', error)

      return {
        success: false,
        message: error.message || 'Tên đăng nhập hoặc mật khẩu không đúng'
      }
    }
  },

  /**
   * Refresh token
   * @param {string} refreshToken 
   */
  async refreshToken(refreshToken) {
    console.log('🔄 authService.refreshToken called')

    if (!refreshToken) {
      return {
        success: false,
        message: 'Refresh token không hợp lệ.'
      }
    }

    try {
      console.log('📡 Calling real refresh token API...')
      const response = await api.auth.refreshToken({ refreshToken })

      let data = null
      if (!response) {
        throw new Error('No response from refresh token API')
      }

      if (response.success && response.data) {
        data = response.data
      } else if (response.data && (response.data.token || response.data.accessToken)) {
        data = response.data
      } else if (response.token || response.accessToken) {
        data = response
      } else {
        data = response
      }

      if (!data.token && data.accessToken) {
        data.token = data.accessToken
      }
      if (!data.refreshToken) {
        data.refreshToken = `refresh-${Date.now()}`
      }

      if (data && data.token) {
        console.log('✅ Refresh token successful')
        return { success: true, data }
      }

      return { success: false, message: response.message || 'Refresh token failed' }
    } catch (error) {
      console.error('❌ Refresh token error:', error)
      return {
        success: false,
        message: error.message || 'Không thể refresh token'
      }
    }
  },

  /**
   * Đăng xuất
   */
  async logout() {
    try {
      const token = localStorage.getItem('token')
      if (token) {
        await api.auth.logout()
      }
      console.log('✅ Logout successful')
      return { success: true }
    } catch (error) {
      console.error('Logout error', error)
      return { success: true }
    }
  },

  /**
   * Kiểm tra token có hợp lệ không
   * @param {string} token 
   */
  async verifyToken(token) {
    if (!token) {
      return false
    }

    try {
      const response = await api.auth.profile()
      return response && (response.success || response.data) ? true : false
    } catch (error) {
      console.error('Verify token error:', error)
      return false
    }
  }
}