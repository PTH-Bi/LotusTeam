import axios from "axios"

// URL backend với fallback và kiểm tra
const getBaseUrl = () => {
  // Ưu tiên từ environment variable
  const envUrl = import.meta.env.VITE_API_URL
  if (envUrl) return envUrl
  
  // Kiểm tra nếu đang chạy trong browser
  if (typeof window !== 'undefined') {
    // Thử lấy từ localStorage (có thể set sau khi cấu hình)
    const savedUrl = localStorage.getItem('api_base_url')
    if (savedUrl) return savedUrl
  }
  
  // Default URL - thử https trước, nếu lỗi thì chuyển sang http
  return "http://localhost:5000/api"
}

const defaultUrl = getBaseUrl()

console.log('🚀 API Base URL:', defaultUrl)

const axiosClient = axios.create({
  baseURL: defaultUrl,
  headers: {
    "Content-Type": "application/json",
    "Accept": "application/json",
  },
  timeout: 30000,
  withCredentials: false,
})

// ===== BIẾN QUẢN LÝ REFRESH TOKEN =====
let isRefreshing = false
let failedQueue = []

const processQueue = (error, token = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

// ===== REQUEST INTERCEPTOR =====
axiosClient.interceptors.request.use(
  (config) => {
    // Log request cho debug
    console.log(`📤 API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`)
    
    const token = localStorage.getItem("token")
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    
    return config
  },
  (error) => {
    console.error("❌ Request error:", error)
    return Promise.reject(error)
  }
)

// ===== RESPONSE INTERCEPTOR =====
axiosClient.interceptors.response.use(
  (response) => {
    console.log(`📥 API Response: ${response.config.url}`, response.status)
    return response.data
  },
  async (error) => {
    const originalRequest = error.config

    console.error("❌ API Error Details:", {
      url: originalRequest?.url,
      method: originalRequest?.method,
      baseURL: originalRequest?.baseURL,
      message: error.message,
      code: error.code,
      status: error.response?.status
    })
    
    // ===== XỬ LÝ LỖI KẾT NỐI =====
    // Không kết nối được server
    if (error.code === "ERR_NETWORK" || error.message?.includes("Network Error")) {
      const networkError = new Error(
        `Không thể kết nối đến server tại ${originalRequest?.baseURL || defaultUrl}. ` +
        `Vui lòng kiểm tra backend đã chạy chưa.`
      )
      networkError.code = "NETWORK_ERROR"
      return Promise.reject(networkError)
    }
    
    // Connection refused
    if (error.message?.includes("ERR_CONNECTION_REFUSED")) {
      const connError = new Error(
        `Không thể kết nối đến server. Vui lòng kiểm tra backend đã chạy tại ${defaultUrl}`
      )
      connError.code = "CONNECTION_REFUSED"
      return Promise.reject(connError)
    }
    
    // ===== XỬ LÝ 401 - REFRESH TOKEN =====
    if (error.response?.status === 401 && !originalRequest._retry) {
      console.warn("⚠️ Token hết hạn → đang refresh...")
      
      // Nếu đang refresh, đợi và thử lại
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return axiosClient(originalRequest)
        }).catch(err => {
          return Promise.reject(err)
        })
      }
      
      originalRequest._retry = true
      isRefreshing = true
      
      try {
        const refreshToken = localStorage.getItem("refreshToken")
        
        if (!refreshToken) {
          throw new Error("Không có refresh token")
        }
        
        // Gọi API refresh token
        const response = await axios.post(`${defaultUrl}/auth/refresh`, {
          refreshToken: refreshToken
        })
        
        // Lấy token mới (cấu trúc response có thể khác tùy backend)
        const newToken = response.data?.data?.token || response.data?.token
        const newRefreshToken = response.data?.data?.refreshToken || response.data?.refreshToken
        
        if (!newToken) {
          throw new Error("Không nhận được token mới từ server")
        }
        
        // Cập nhật storage
        localStorage.setItem("token", newToken)
        if (newRefreshToken) {
          localStorage.setItem("refreshToken", newRefreshToken)
        }
        
        console.log("✅ Refresh token thành công")
        
        // Xử lý các request đang chờ
        processQueue(null, newToken)
        
        // Thử lại request cũ với token mới
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return axiosClient(originalRequest)
        
      } catch (refreshError) {
        console.error("❌ Refresh token failed:", refreshError)
        
        // Xử lý các request đang chờ
        processQueue(refreshError, null)
        
        // Xóa token cũ
        localStorage.removeItem("token")
        localStorage.removeItem("refreshToken")
        
        // Chuyển hướng về login nếu đang ở browser
        if (typeof window !== 'undefined') {
          // Kiểm tra không phải trang login để tránh loop
          if (!window.location.pathname.includes('/login')) {
            window.location.href = "/login"
          }
        }
        
        // Tạo lỗi chi tiết
        const authError = new Error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.")
        authError.code = "AUTH_EXPIRED"
        return Promise.reject(authError)
        
      } finally {
        isRefreshing = false
      }
    }
    
    // ===== XỬ LÝ CÁC LỖI KHÁC (400, 403, 500, ...) =====
    // Build error message chi tiết
    let detailedMessage = ''
    const status = error.response?.status
    if (status) {
      detailedMessage += `HTTP ${status}`
      if (error.response?.statusText) detailedMessage += ` ${error.response.statusText}`
      detailedMessage += ': '
    }
    
    const respData = error.response?.data
    if (respData) {
      if (typeof respData === 'string') {
        detailedMessage += respData
      } else if (respData.message) {
        detailedMessage += respData.message
      } else if (respData.title) {
        detailedMessage += respData.title
      } else if (respData.errors) {
        try {
          const errors = Object.values(respData.errors).flat()
          detailedMessage += errors.join(', ')
        } catch (e) {
          detailedMessage += JSON.stringify(respData)
        }
      } else {
        detailedMessage += JSON.stringify(respData)
      }
    } else {
      detailedMessage += error.message || 'Có lỗi xảy ra'
    }
    
    const err = new Error(detailedMessage)
    err.code = error.code
    err.status = error.response?.status
    err.response = error.response
    err.request = error.request
    err.original = error
    
    return Promise.reject(err)
  }
)

// ===== HELPER FUNCTIONS =====

// Kiểm tra kết nối
export const testConnection = async () => {
  try {
    // Thử gọi endpoint đơn giản
    await axiosClient.get('/ping', { timeout: 5000 })
    return { success: true, message: 'Kết nối thành công' }
  } catch (error) {
    return { 
      success: false, 
      message: error.message,
      code: error.code
    }
  }
}

// Thay đổi base URL động
export const setBaseUrl = (url) => {
  axiosClient.defaults.baseURL = url
  if (typeof window !== 'undefined') {
    localStorage.setItem('api_base_url', url)
  }
  console.log('🔄 API Base URL changed to:', url)
}

// Lấy token hiện tại
export const getToken = () => {
  return localStorage.getItem('token')
}

// Lấy refresh token hiện tại
export const getRefreshToken = () => {
  return localStorage.getItem('refreshToken')
}

// Set tokens
export const setTokens = (token, refreshToken) => {
  if (token) localStorage.setItem('token', token)
  if (refreshToken) localStorage.setItem('refreshToken', refreshToken)
}

// Clear tokens (logout)
export const clearTokens = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}

// Kiểm tra token còn hạn không
export const isTokenValid = () => {
  const token = localStorage.getItem('token')
  if (!token) return false
  
  try {
    // Decode JWT token để kiểm tra expiration
    const payload = JSON.parse(atob(token.split('.')[1]))
    const exp = payload.exp
    if (!exp) return true // Nếu không có exp, coi như valid
    
    const now = Math.floor(Date.now() / 1000)
    return exp > now
  } catch (error) {
    console.error('Error checking token validity:', error)
    return false
  }
}

// Force refresh token (có thể gọi manual khi cần)
export const forceRefreshToken = async () => {
  const refreshToken = localStorage.getItem('refreshToken')
  if (!refreshToken) {
    throw new Error('No refresh token available')
  }
  
  try {
    const response = await axios.post(`${defaultUrl}/auth/refresh`, {
      refreshToken: refreshToken
    })
    
    const newToken = response.data?.data?.token || response.data?.token
    const newRefreshToken = response.data?.data?.refreshToken || response.data?.refreshToken
    
    if (newToken) {
      localStorage.setItem('token', newToken)
      if (newRefreshToken) {
        localStorage.setItem('refreshToken', newRefreshToken)
      }
      return newToken
    }
    
    throw new Error('Invalid refresh response')
  } catch (error) {
    clearTokens()
    throw error
  }
}

export default axiosClient