import { defineStore } from 'pinia'
import { authService } from '@/services/authService'
import { normalizeRoleCode, guessRoleCode, getRoleGroupNames } from '@/utils/roleUtils'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    isAuthenticated: false,
    loading: false,
    error: null,
    token: localStorage.getItem('token') || null,
    refreshToken: localStorage.getItem('refreshToken') || null,
    refreshInterval: null
  }),

  getters: {
    // Role checks
    isAdmin: (state) => {
      const role = normalizeRoleCode(state.user?.role)
      return ['SUPER_ADMIN', 'ADMIN'].includes(role)
    },
    isDirector: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    isAccounting: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('finance'),
    isHR: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('hr'),
    isTechnical: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('tech'),
    isMarketing: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('marketing'),
    
    // Permission helpers
    canViewAll: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    canViewPayroll: (state) => [
      'SUPER_ADMIN',
      'ADMIN',
      'DIRECTOR',
      'MANAGER',
      'ACCOUNTANT',
      'FINANCE_MANAGER'
    ].includes(normalizeRoleCode(state.user?.role)),
    canViewRecruitment: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('hr') || getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    canViewEmployees: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('hr') || getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    canViewTraining: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('hr') || getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    canManageAttendance: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('director') || getRoleGroupNames(state.user?.role, state.user?.department).includes('hr') || getRoleGroupNames(state.user?.role, state.user?.department).includes('finance'),
    canViewFeedback: (state) => !getRoleGroupNames(state.user?.role, state.user?.department).includes('finance'),
    canViewCV: (state) => getRoleGroupNames(state.user?.role, state.user?.department).includes('hr') || getRoleGroupNames(state.user?.role, state.user?.department).includes('marketing') || getRoleGroupNames(state.user?.role, state.user?.department).includes('director'),
    canViewLeave: (state) => true, // Mọi người đều xem được nghỉ phép
    canManageQR: (state) => true, // Mọi người đều xem được QR
  },

  actions: {
    async login(username, password) {
      this.loading = true
      this.error = null

      try {
        const result = await authService.login(username, password)

        if (result.success) {
          const userData = result.data

          // Set role dựa trên username và data từ server
          if (!userData.role) {
            userData.role = guessRoleCode({
              role: userData.role,
              department: userData.department,
              position: userData.position,
              username: userData.username
            })
          }
          userData.role = normalizeRoleCode(userData.role)
          
          // Nếu server không trả về department hoặc position, giữ nguyên giá trị hiện có.
          if (!userData.department) {
            userData.department = userData.department || ''
          }
          if (!userData.position) {
            userData.position = userData.position || ''
          }

          this.user = userData
          this.isAuthenticated = true
          this.token = userData.token
          this.refreshToken = userData.refreshToken

          // Lưu vào localStorage
          localStorage.setItem('token', userData.token)
          localStorage.setItem('refreshToken', userData.refreshToken)
          localStorage.setItem('user', JSON.stringify(userData))

          // Bắt đầu tự động refresh token
          this.startTokenRefresh()

          console.log('✅ Login success - User:', userData.username, 'Role:', userData.role, 'Department:', userData.department, 'Position:', userData.position)
          return true
        } else {
          this.error = result.message || 'Đăng nhập thất bại'
          console.error('❌ Login failed:', this.error)
          return false
        }
      } catch (error) {
        this.error = 'Đã xảy ra lỗi khi đăng nhập'
        console.error('❌ Login error:', error)
        return false
      } finally {
        this.loading = false
      }
    },

    async refreshAuthToken() {
      if (!this.refreshToken) {
        console.log('⚠️ No refresh token available')
        return false
      }

      if (!this.refreshToken) {
        console.log('⚠️ No refresh token available')
        return false
      }

      try {
        console.log('🔄 Attempting to refresh token...')
        const result = await authService.refreshToken(this.refreshToken)

        if (result.success) {
          // Cập nhật token mới
          this.token = result.data.token
          localStorage.setItem('token', result.data.token)

          // Cập nhật refresh token mới nếu có
          if (result.data.refreshToken) {
            this.refreshToken = result.data.refreshToken
            localStorage.setItem('refreshToken', result.data.refreshToken)
          }

          console.log('✅ Token refreshed successfully')
          return true
        } else {
          console.log('❌ Refresh token failed:', result.message)
          await this.logout()
          return false
        }
      } catch (error) {
        console.error('❌ Refresh token error:', error)
        await this.logout()
        return false
      }
    },

    startTokenRefresh() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval)
      }

      // Refresh token mỗi 50 phút (tokens thường hết hạn sau 60 phút)
      this.refreshInterval = setInterval(async () => {
        if (this.isAuthenticated && this.refreshToken) {
          console.log('🔄 Auto refreshing token...')
          await this.refreshAuthToken()
        }
      }, 50 * 60 * 1000) // 50 minutes

      console.log('⏰ Token refresh scheduler started')
    },

    stopTokenRefresh() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval)
        this.refreshInterval = null
        console.log('⏹️ Token refresh scheduler stopped')
      }
    },

    async logout() {
      try {
        if (this.token) {
          await authService.logout()
        }
      } catch (error) {
        console.error('Logout error:', error)
      } finally {
        // Dừng refresh token
        this.stopTokenRefresh()
        
        // Xóa state
        this.user = null
        this.isAuthenticated = false
        this.error = null
        this.token = null
        this.refreshToken = null

        // Xóa localStorage
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        localStorage.removeItem('user')
        
        console.log('👋 Logged out successfully')
      }
    },

    checkAuth() {
      const token = localStorage.getItem('token')
      const refreshToken = localStorage.getItem('refreshToken')
      const userStr = localStorage.getItem('user')

      console.log('🔍 CheckAuth - Token:', token ? 'Có token' : 'Không có token')
      console.log('🔍 CheckAuth - RefreshToken:', refreshToken ? 'Có refresh token' : 'Không có refresh token')
      console.log('🔍 CheckAuth - UserStr:', userStr ? 'Có user' : 'Không có user')

      if (!token || !userStr) {
        console.log('⚠️ CheckAuth - Thiếu token hoặc user, logout')
        this.logout()
        return false
      }

      try {
        const userData = JSON.parse(userStr)
        
        // Set role dựa trên username nếu chưa có
        if (!userData.role) {
          userData.role = guessRoleCode({
            role: userData.role,
            department: userData.department,
            position: userData.position,
            username: userData.username
          })
        }
        userData.role = normalizeRoleCode(userData.role)
        
        this.user = userData
        this.isAuthenticated = true
        this.token = token
        this.refreshToken = refreshToken

        // Khởi động lại refresh token scheduler
        this.startTokenRefresh()
        
        console.log('✅ CheckAuth - Thành công:', userData.username, 'Role:', userData.role, 'Department:', userData.department)
        return true
      } catch (error) {
        console.error('❌ CheckAuth - Lỗi parse user:', error)
        this.logout()
        return false
      }
    },

    // Helper method để lấy token cho API calls
    getAuthHeader() {
      if (this.token) {
        return { Authorization: `Bearer ${this.token}` }
      }
      return {}
    }
  }
})