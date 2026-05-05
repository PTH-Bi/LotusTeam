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
  
  // Default URL - có thể đổi thành http nếu https không hoạt động
  return "https://localhost:7010/api", "http://localhost:5000/api"
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

// REQUEST INTERCEPTOR
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

// RESPONSE INTERCEPTOR
axiosClient.interceptors.response.use(
  (response) => {
    console.log(`📥 API Response: ${response.config.url}`, response.status)
    return response.data
  },
  (error) => {
    console.error("❌ API Error Details:", {
      url: error.config?.url,
      method: error.config?.method,
      baseURL: error.config?.baseURL,
      message: error.message,
      code: error.code,
      status: error.response?.status
    })
    
    // Không kết nối được server
    if (error.code === "ERR_NETWORK" || error.message?.includes("Network Error")) {
      const networkError = new Error(
        `Không thể kết nối đến server tại ${error.config?.baseURL || defaultUrl}. ` +
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
    
    // Unauthorized - không reject ngay, để component xử lý
    if (error.response?.status === 401) {
      console.warn("⚠️ Unauthorized request - token có thể đã hết hạn")
      // Không tự động logout, để authStore xử lý
    }
    
    // Build a more detailed error message including status and server response
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
    err.response = error.response
    err.request = error.request
    err.original = error

    return Promise.reject(err)
  }
)

// Helper function để kiểm tra kết nối
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

// Helper để thay đổi base URL (cho phép cấu hình động)
export const setBaseUrl = (url) => {
  axiosClient.defaults.baseURL = url
  if (typeof window !== 'undefined') {
    localStorage.setItem('api_base_url', url)
  }
  console.log('🔄 API Base URL changed to:', url)
}

export default axiosClient