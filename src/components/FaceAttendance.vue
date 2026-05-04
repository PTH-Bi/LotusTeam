<template>
  <div class="face-attendance">
    <div class="page-header">
      <h1 class="page-title">Chấm công khuôn mặt</h1>
      <div class="header-actions">
        <button class="btn-outline" @click="openRegisterModal" v-if="canRegister">
          <i>📷</i> Đăng ký khuôn mặt
        </button>
      </div>
    </div>

    <!-- Today's Status Card -->
    <div class="status-card" :class="todayStatus.statusClass">
      <div class="status-icon">
        <span v-if="!todayStatus.hasCheckedIn">👤</span>
        <span v-else-if="!todayStatus.hasCheckedOut">✅</span>
        <span v-else>🏁</span>
      </div>
      <div class="status-info">
        <h3>Hôm nay - {{ formatDate(new Date()) }}</h3>
        <div class="status-details">
          <div class="detail-item" v-if="todayStatus.checkInTime">
            <span class="label">Check-in:</span>
            <span class="value">{{ formatTime(todayStatus.checkInTime) }}</span>
            <span :class="['badge', todayStatus.checkInStatus]">{{ todayStatus.checkInStatusText }}</span>
          </div>
          <div class="detail-item" v-if="todayStatus.checkOutTime">
            <span class="label">Check-out:</span>
            <span class="value">{{ formatTime(todayStatus.checkOutTime) }}</span>
          </div>
          <div class="detail-item" v-if="todayStatus.workingHours">
            <span class="label">Số giờ làm:</span>
            <span class="value">{{ todayStatus.workingHours }} giờ</span>
          </div>
        </div>
        <p class="status-message">{{ todayStatus.message }}</p>
      </div>
      <div class="action-buttons">
        <button 
          v-if="!todayStatus.hasCheckedIn"
          class="btn-primary btn-large"
          @click="openCamera('checkin')"
          :disabled="isProcessing"
        >
          <i>📸</i> Check-in
        </button>
        <button 
          v-else-if="!todayStatus.hasCheckedOut"
          class="btn-warning btn-large"
          @click="openCamera('checkout')"
          :disabled="isProcessing"
        >
          <i>📸</i> Check-out
        </button>
        <button 
          v-else
          class="btn-success btn-large"
          disabled
        >
          <i>✅</i> Đã hoàn thành
        </button>
      </div>
    </div>

    <!-- History Section -->
    <div class="history-section">
      <div class="section-header">
        <h2>Lịch sử chấm công</h2>
        <div class="history-filters">
          <input type="date" v-model="historyFilters.fromDate" class="filter-input" />
          <span>→</span>
          <input type="date" v-model="historyFilters.toDate" class="filter-input" />
          <button class="btn-secondary" @click="fetchHistory">Lọc</button>
          <button class="btn-outline" @click="resetFilters">Reset</button>
        </div>
      </div>

      <div class="table-container" v-if="historyList.length > 0">
        <table class="data-table">
          <thead>
            <tr>
              <th>Ngày</th>
              <th>Check-in</th>
              <th>Check-out</th>
              <th>Số giờ</th>
              <th>Độ tin cậy</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in historyList" :key="item.id">
              <td>{{ formatDate(item.date) }}</td>
              <td :class="{ 'late': isLate(item.checkInTime) }">
                {{ formatTime(item.checkInTime) }}
                <span v-if="isLate(item.checkInTime)" class="late-tag">Muộn</span>
              </td>
              <td>{{ formatTime(item.checkOutTime) }}</td>
              <td>{{ item.workingHours?.toFixed(1) || '—' }} giờ</td>
              <td>
                <div class="confidence-bar">
                  <div class="confidence-fill" :style="{ width: item.checkInConfidence + '%' }"></div>
                  <span>{{ item.checkInConfidence?.toFixed(0) || 0 }}%</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', getHistoryStatusClass(item.status)]">
                  {{ getHistoryStatusText(item.status) }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else class="empty-state">
        <p>Chưa có lịch sử chấm công</p>
      </div>
    </div>

    <!-- Camera Modal -->
    <div class="modal" v-if="showCamera" @click.self="closeCamera">
      <div class="modal-content camera-modal">
        <div class="modal-header">
          <h3>{{ cameraMode === 'checkin' ? 'Check-in bằng khuôn mặt' : 'Check-out bằng khuôn mặt' }}</h3>
          <button class="close-btn" @click="closeCamera">&times;</button>
        </div>
        <div class="modal-body">
          <div class="camera-container">
            <video ref="video" autoplay playsinline class="camera-video"></video>
            <canvas ref="canvas" style="display: none;"></canvas>
            <div class="camera-overlay" v-if="!cameraReady">
              <div class="loading-spinner"></div>
              <p>Đang khởi tạo camera...</p>
            </div>
          </div>
          <div class="camera-instructions">
            <p>👉 Hướng mặt vào camera, giữ yên trong giây lát</p>
            <p>👉 Đảm bảo đủ ánh sáng và mặt không bị che khuất</p>
          </div>
          <div class="preview-area" v-if="capturedImage">
            <h4>Ảnh đã chụp:</h4>
            <img :src="capturedImage" class="preview-image" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeCamera">Hủy</button>
          <button class="btn-primary" @click="captureAndProcess" :disabled="!cameraReady || isProcessing">
            {{ isProcessing ? 'Đang xử lý...' : 'Chụp ảnh & Xác nhận' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Register Face Modal -->
    <div class="modal" v-if="showRegisterModal" @click.self="closeRegisterModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>Đăng ký khuôn mặt</h3>
          <button class="close-btn" @click="closeRegisterModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Chọn nhân viên</label>
            <select v-model="registerForm.employeeId" class="form-control" v-if="canRegisterAll">
              <option value="">-- Chọn nhân viên --</option>
              <option v-for="emp in employees" :key="emp.id" :value="emp.id">
                {{ emp.fullName }} - {{ emp.employeeCode }}
              </option>
            </select>
            <input v-else type="text" :value="currentEmployeeName" class="form-control" disabled />
          </div>

          <div class="camera-section">
            <div class="camera-container small">
              <video ref="registerVideo" autoplay playsinline class="camera-video"></video>
              <canvas ref="registerCanvas" style="display: none;"></canvas>
            </div>
            <button type="button" class="btn-outline" @click="captureRegisterPhoto" :disabled="!registerCameraReady">
              📸 Chụp ảnh
            </button>
          </div>

          <div class="preview-area" v-if="registerCapturedImage">
            <h4>Ảnh đăng ký:</h4>
            <img :src="registerCapturedImage" class="preview-image" />
          </div>

          <div class="form-group">
            <label>Hướng dẫn:</label>
            <ul class="instruction-list">
              <li>✓ Mặt nhìn thẳng vào camera</li>
              <li>✓ Không đeo kính râm, khẩu trang</li>
              <li>✓ Ánh sáng đầy đủ, không bị chói</li>
              <li>✓ Giữ yên trong lúc chụp</li>
            </ul>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeRegisterModal">Hủy</button>
          <button class="btn-primary" @click="submitRegister" :disabled="!registerCapturedImage || isProcessing">
            {{ isProcessing ? 'Đang đăng ký...' : 'Đăng ký' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Result Modal -->
    <div class="modal" v-if="showResultModal" @click.self="closeResultModal">
      <div class="modal-content result-modal">
        <div class="modal-header" :class="resultModal.type">
          <h3>{{ resultModal.title }}</h3>
          <button class="close-btn" @click="closeResultModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="result-icon">
            <span v-if="resultModal.type === 'success'">✅</span>
            <span v-else-if="resultModal.type === 'warning'">⚠️</span>
            <span v-else>❌</span>
          </div>
          <p class="result-message">{{ resultModal.message }}</p>
          <div class="result-details" v-if="resultModal.confidence">
            <div class="confidence-circle">
              <svg viewBox="0 0 36 36" class="circular-chart">
                <path class="circle-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <path class="circle" :stroke-dasharray="`${resultModal.confidence}, 100`" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                <text x="18" y="20.35" class="percentage">{{ resultModal.confidence }}%</text>
              </svg>
            </div>
            <p class="confidence-text">Độ tin cậy: {{ resultModal.confidence }}%</p>
            <p class="confidence-note">
              {{ resultModal.confidence >= 70 ? '✓ Nhận diện thành công' : '✗ Độ tin cậy thấp, vui lòng thử lại' }}
            </p>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-primary" @click="closeResultModal">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import api from '../services/api'

export default {
  name: 'FaceAttendance',
  data() {
    return {
      todayStatus: {
        hasCheckedIn: false,
        hasCheckedOut: false,
        checkInTime: null,
        checkOutTime: null,
        workingHours: null,
        status: '',
        statusClass: '',
        checkInStatus: '',
        checkInStatusText: '',
        message: ''
      },
      historyList: [],
      historyFilters: {
        fromDate: '',
        toDate: ''
      },
      showCamera: false,
      showRegisterModal: false,
      showResultModal: false,
      cameraMode: 'checkin',
      cameraReady: false,
      registerCameraReady: false,
      isProcessing: false,
      capturedImage: null,
      capturedImageBase64: null,
      registerCapturedImage: null,
      registerCapturedImageBase64: null,
      employees: [],
      registerForm: {
        employeeId: null
      },
      resultModal: {
        type: 'success',
        title: '',
        message: '',
        confidence: null
      },
      currentUser: null,
      currentEmployeeId: null,
      currentEmployeeName: ''
    }
  },
  computed: {
    canRegister() {
      const role = this.currentUser?.role || ''
      return ['SUPER_ADMIN', 'ADMIN', 'HR_MANAGER', 'HR_STAFF'].includes(role)
    },
    canRegisterAll() {
      const role = this.currentUser?.role || ''
      return ['SUPER_ADMIN', 'ADMIN', 'HR_MANAGER', 'HR_STAFF'].includes(role)
    }
  },
  mounted() {
    this.loadUserInfo()
    this.fetchTodayStatus()
    this.fetchHistory()
    if (this.canRegisterAll) {
      this.fetchEmployees()
    }
  },
  beforeUnmount() {
    this.stopCamera()
  },
  methods: {
    loadUserInfo() {
      const userStr = localStorage.getItem('user')
      if (userStr) {
        try {
          this.currentUser = JSON.parse(userStr)
          this.currentEmployeeId = this.currentUser?.employeeId || this.currentUser?.id
          this.currentEmployeeName = this.currentUser?.fullName || this.currentUser?.username || ''
          this.registerForm.employeeId = this.currentEmployeeId
        } catch (e) {
          console.error('Error parsing user:', e)
        }
      }
    },
    async fetchTodayStatus() {
      if (!this.currentEmployeeId) return
      try {
        const res = await api.faceAttendance.getToday(this.currentEmployeeId)
        if (res.data && res.data.success) {
          const data = res.data.data
          this.todayStatus = {
            hasCheckedIn: data.hasCheckedIn || false,
            hasCheckedOut: data.hasCheckedOut || false,
            checkInTime: data.checkInTime,
            checkOutTime: data.checkOutTime,
            workingHours: data.workingHours,
            status: data.status,
            statusClass: this.getStatusClass(data.status),
            checkInStatus: data.checkInStatus,
            checkInStatusText: data.checkInStatus === 'late' ? 'Đi muộn' : 'Đúng giờ',
            message: this.getStatusMessage(data)
          }
        }
      } catch (error) {
        console.error('Failed to fetch today status:', error)
      }
    },
    async fetchHistory() {
      if (!this.currentEmployeeId) return
      try {
        const params = {}
        if (this.historyFilters.fromDate) params.fromDate = this.historyFilters.fromDate
        if (this.historyFilters.toDate) params.toDate = this.historyFilters.toDate
        const res = await api.faceAttendance.getHistory(this.currentEmployeeId, params)
        if (res.data && res.data.success) {
          this.historyList = res.data.data || []
        }
      } catch (error) {
        console.error('Failed to fetch history:', error)
      }
    },
    async fetchEmployees() {
      try {
        const res = await api.employees.list({ status: 'Active' })
        this.employees = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch employees:', error)
      }
    },
    openCamera(mode) {
      this.cameraMode = mode
      this.capturedImage = null
      this.capturedImageBase64 = null
      this.showCamera = true
      this.initCamera()
    },
    async initCamera() {
  this.cameraReady = false
  try {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: 'user',      // camera trước trên điện thoại
        width: { ideal: 1280 },
        height: { ideal: 720 }
      }
    })
    const videoEl = this.$refs.video
    if (videoEl) {
      videoEl.srcObject = stream
      videoEl.onloadedmetadata = () => {
        videoEl.play()           // cần thiết trên iOS Safari
        this.cameraReady = true
      }
    }
  } catch (error) {
    console.error('Camera error:', error)
    // Thử lại không có constraint nếu thiết bị không hỗ trợ
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: true })
      const videoEl = this.$refs.video
      if (videoEl) {
        videoEl.srcObject = stream
        videoEl.onloadedmetadata = () => {
          videoEl.play()
          this.cameraReady = true
        }
      }
    } catch (fallbackError) {
      this.$toast?.error('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.')
      this.closeCamera()
    }
  }
},

// Tương tự cho initRegisterCamera():
async initRegisterCamera() {
  this.registerCameraReady = false
  try {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: 'user', width: { ideal: 1280 }, height: { ideal: 720 } }
    })
    const videoEl = this.$refs.registerVideo
    if (videoEl) {
      videoEl.srcObject = stream
      videoEl.onloadedmetadata = () => {
        videoEl.play()
        this.registerCameraReady = true
      }
    }
  } catch (error) {
    this.$toast?.error('Không thể truy cập camera')
  }
},
    async captureAndProcess() {
      this.capturePhoto()
      // capturePhoto() là sync nên capturedImageBase64 đã có ngay
      // Nhưng cần nextTick để Vue update preview image trước
      await this.$nextTick()
      if (!this.capturedImageBase64) {
        this.$toast?.error('Không thể chụp ảnh. Camera chưa sẵn sàng.')
        return
      }
      await this.processAttendance()
    },
    capturePhoto() {
      const video = this.$refs.video
      const canvas = this.$refs.canvas
      if (video && canvas) {
        const context = canvas.getContext('2d')
        canvas.width = video.videoWidth
        canvas.height = video.videoHeight
        context.drawImage(video, 0, 0, canvas.width, canvas.height)
        this.capturedImageBase64 = canvas.toDataURL('image/jpeg', 0.8)
        this.capturedImage = this.capturedImageBase64
      }
    },
    captureRegisterPhoto() {
      const video = this.$refs.registerVideo
      const canvas = this.$refs.registerCanvas
      if (video && canvas) {
        const context = canvas.getContext('2d')
        canvas.width = video.videoWidth
        canvas.height = video.videoHeight
        context.drawImage(video, 0, 0, canvas.width, canvas.height)
        this.registerCapturedImageBase64 = canvas.toDataURL('image/jpeg', 0.8)
        this.registerCapturedImage = this.registerCapturedImageBase64
      }
    },
    async processAttendance() {
      if (!this.capturedImageBase64) return
      
      this.isProcessing = true
      const employeeId = this.currentEmployeeId
      
      try {
        let response
        if (this.cameraMode === 'checkin') {
          response = await api.faceAttendance.checkIn(employeeId, this.capturedImageBase64)
        } else {
          response = await api.faceAttendance.checkOut(employeeId, this.capturedImageBase64)
        }
        
        if (response.data && response.data.success) {
          const data = response.data.data
          this.resultModal = {
            type: 'success',
            title: this.cameraMode === 'checkin' ? 'Check-in thành công!' : 'Check-out thành công!',
            message: response.data.message || `Chấm công ${this.cameraMode === 'checkin' ? 'check-in' : 'check-out'} thành công`,
            confidence: data?.confidence ? Math.round(data.confidence) : null
          }
          this.closeCamera()
          this.fetchTodayStatus()
          this.fetchHistory()
        } else {
          this.resultModal = {
            type: 'error',
            title: 'Chấm công thất bại',
            message: response.data?.message || 'Không thể nhận diện khuôn mặt. Vui lòng thử lại.',
            confidence: response.data?.data?.confidence ? Math.round(response.data.data.confidence) : null
          }
        }
        this.showResultModal = true
      } catch (error) {
        console.error('Attendance failed:', error)
        this.resultModal = {
          type: 'error',
          title: 'Lỗi',
          message: error.response?.data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.',
          confidence: null
        }
        this.showResultModal = true
      } finally {
        this.isProcessing = false
      }
    },
    async submitRegister() {
      if (!this.registerCapturedImageBase64) {
        this.$toast?.warning('Vui lòng chụp ảnh khuôn mặt')
        return
      }
      if (!this.registerForm.employeeId) {
        this.$toast?.warning('Vui lòng chọn nhân viên')
        return
      }
      
      this.isProcessing = true
      try {
        const response = await api.faceAttendance.registerFace(
          this.registerForm.employeeId,
          this.registerCapturedImageBase64
        )
        
        if (response.data && response.data.success) {
          this.resultModal = {
            type: 'success',
            title: 'Đăng ký thành công!',
            message: 'Khuôn mặt đã được đăng ký thành công.',
            confidence: null
          }
          this.closeRegisterModal()
        } else {
          this.resultModal = {
            type: 'error',
            title: 'Đăng ký thất bại',
            message: response.data?.message || 'Không thể đăng ký khuôn mặt. Vui lòng thử lại.',
            confidence: null
          }
        }
        this.showResultModal = true
      } catch (error) {
        console.error('Register failed:', error)
        this.resultModal = {
          type: 'error',
          title: 'Lỗi',
          message: error.response?.data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.',
          confidence: null
        }
        this.showResultModal = true
      } finally {
        this.isProcessing = false
      }
    },
    openRegisterModal() {
      this.registerCapturedImage = null
      this.registerCapturedImageBase64 = null
      this.showRegisterModal = true
      this.$nextTick(() => {
        this.initRegisterCamera()
      })
    },
    stopCamera() {
  // Stop attendance camera
  const videoEl = this.$refs.video
  if (videoEl?.srcObject) {
    videoEl.srcObject.getTracks().forEach(t => t.stop())
    videoEl.srcObject = null
  }
  // Stop register camera
  const registerVideoEl = this.$refs.registerVideo
  if (registerVideoEl?.srcObject) {
    registerVideoEl.srcObject.getTracks().forEach(t => t.stop())
    registerVideoEl.srcObject = null
  }
},
    closeCamera() {
      this.stopCamera()
      this.showCamera = false
      this.cameraReady = false
    },
    closeRegisterModal() {
      this.stopCamera()
      this.showRegisterModal = false
      this.registerCameraReady = false
    },
    closeResultModal() {
      this.showResultModal = false
    },
    resetFilters() {
      this.historyFilters.fromDate = ''
      this.historyFilters.toDate = ''
      this.fetchHistory()
    },
    getStatusClass(status) {
      const classes = {
        'on_time': 'card-success',
        'late': 'card-warning',
        'early_leave': 'card-warning'
      }
      return classes[status] || 'card-info'
    },
    getStatusMessage(data) {
      if (!data.hasCheckedIn) return 'Bạn chưa check-in hôm nay'
      if (data.hasCheckedIn && !data.hasCheckedOut) return 'Bạn đã check-in, hãy nhớ check-out trước khi kết thúc ngày làm việc'
      if (data.hasCheckedOut) return 'Đã hoàn thành chấm công hôm nay'
      return ''
    },
    getHistoryStatusClass(status) {
      const classes = {
        'on_time': 'success',
        'late': 'warning',
        'early_leave': 'warning',
        'absent': 'danger'
      }
      return classes[status] || 'secondary'
    },
    getHistoryStatusText(status) {
      const texts = {
        'on_time': 'Đúng giờ',
        'late': 'Đi muộn',
        'early_leave': 'Về sớm',
        'absent': 'Vắng mặt'
      }
      return texts[status] || status
    },
    isLate(checkInTime) {
      if (!checkInTime) return false
      const time = new Date(checkInTime)
      const hours = time.getHours()
      const minutes = time.getMinutes()
      return hours > 8 || (hours === 8 && minutes > 30)
    },
    formatDate(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    },
    formatTime(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`
    }
  }
}
</script>

<style scoped>
.face-attendance {
  padding: 20px;
  background: #f5f7fa;
  min-height: 100vh;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}
.page-title {
  font-size: 24px;
  font-weight: 600;
  color: #1a1a2e;
  margin: 0;
}
.btn-outline {
  background: transparent;
  border: 1px solid #4361ee;
  color: #4361ee;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.3s;
}
.btn-outline:hover {
  background: #4361ee;
  color: white;
}
.btn-primary {
  background: #4361ee;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
}
.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.btn-warning {
  background: #f59e0b;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
}
.btn-success {
  background: #10b981;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: default;
}
.btn-large {
  padding: 14px 28px;
  font-size: 16px;
}
.btn-secondary {
  background: #e9ecef;
  color: #495057;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
}
.status-card {
  background: white;
  border-radius: 20px;
  padding: 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 32px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  transition: all 0.3s;
}
.status-card.card-success {
  border-left: 4px solid #10b981;
}
.status-card.card-warning {
  border-left: 4px solid #f59e0b;
}
.status-card.card-info {
  border-left: 4px solid #4361ee;
}
.status-icon {
  width: 80px;
  height: 80px;
  background: #f0f4ff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 40px;
}
.status-info h3 {
  margin: 0 0 12px 0;
  font-size: 18px;
}
.status-details {
  display: flex;
  gap: 24px;
  margin-bottom: 8px;
}
.detail-item {
  display: flex;
  align-items: center;
  gap: 8px;
}
.detail-item .label {
  color: #6c757d;
  font-size: 13px;
}
.detail-item .value {
  font-weight: 500;
  color: #333;
}
.badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 500;
}
.badge.late {
  background: #fee2e2;
  color: #ef4444;
}
.badge.on-time {
  background: #d1fae5;
  color: #10b981;
}
.status-message {
  margin: 8px 0 0 0;
  color: #6c757d;
  font-size: 13px;
}
.history-section {
  background: white;
  border-radius: 16px;
  padding: 20px;
}
.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  flex-wrap: wrap;
  gap: 16px;
}
.section-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}
.history-filters {
  display: flex;
  align-items: center;
  gap: 10px;
}
.filter-input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
}
.table-container {
  overflow-x: auto;
}
.data-table {
  width: 100%;
  border-collapse: collapse;
}
.data-table th,
.data-table td {
  padding: 12px 15px;
  text-align: left;
  border-bottom: 1px solid #eee;
}
.data-table th {
  background: #f8f9fa;
  font-weight: 600;
  color: #495057;
}
.late {
  color: #ef4444;
}
.late-tag {
  background: #fee2e2;
  color: #ef4444;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 10px;
  margin-left: 8px;
}
.confidence-bar {
  width: 80px;
  height: 20px;
  background: #e9ecef;
  border-radius: 10px;
  overflow: hidden;
  position: relative;
}
.confidence-fill {
  height: 100%;
  background: #10b981;
  border-radius: 10px;
  transition: width 0.3s;
}
.confidence-bar span {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  font-size: 10px;
  font-weight: 500;
  color: #333;
}
.status-badge {
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 500;
}
.status-badge.success {
  background: #d1fae5;
  color: #10b981;
}
.status-badge.warning {
  background: #fed7aa;
  color: #f59e0b;
}
.status-badge.danger {
  background: #fee2e2;
  color: #ef4444;
}
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0,0,0,0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}
.modal-content {
  background: white;
  border-radius: 20px;
  width: 500px;
  max-width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}
.camera-modal {
  width: 600px;
}
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #eee;
}
.modal-header.success {
  background: #d1fae5;
  color: #10b981;
}
.modal-header.error {
  background: #fee2e2;
  color: #ef4444;
}
.modal-header.warning {
  background: #fed7aa;
  color: #f59e0b;
}
.modal-body {
  padding: 20px;
}
.modal-footer {
  padding: 16px 20px;
  border-top: 1px solid #eee;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.camera-container {
  position: relative;
  background: #000;
  border-radius: 12px;
  overflow: hidden;
  margin-bottom: 16px;
}
.camera-video {
  width: 100%;
  max-height: 400px;
  object-fit: cover;
}
.camera-container.small .camera-video {
  max-height: 300px;
}
.camera-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0,0,0,0.7);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  color: white;
}
.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid rgba(255,255,255,0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
.camera-instructions {
  background: #f8f9fa;
  padding: 12px;
  border-radius: 8px;
  margin-bottom: 16px;
}
.camera-instructions p {
  margin: 4px 0;
  font-size: 13px;
  color: #6c757d;
}
.preview-area {
  margin-top: 16px;
  text-align: center;
}
.preview-image {
  max-width: 200px;
  border-radius: 12px;
  margin-top: 8px;
}
.camera-section {
  text-align: center;
  margin-bottom: 16px;
}
.instruction-list {
  margin: 8px 0 0 20px;
  color: #6c757d;
  font-size: 13px;
}
.result-modal {
  text-align: center;
}
.result-icon {
  font-size: 64px;
  margin-bottom: 16px;
}
.result-message {
  font-size: 16px;
  color: #333;
  margin-bottom: 20px;
}
.confidence-circle {
  display: flex;
  justify-content: center;
  margin-bottom: 16px;
}
.circular-chart {
  width: 100px;
  height: 100px;
}
.circle-bg {
  fill: none;
  stroke: #eee;
  stroke-width: 3.8;
}
.circle {
  fill: none;
  stroke: #10b981;
  stroke-width: 3.8;
  stroke-linecap: round;
  animation: progress 1s ease-out forwards;
}
@keyframes progress {
  0% { stroke-dasharray: 0, 100; }
}
.percentage {
  fill: #333;
  font-size: 4px;
  text-anchor: middle;
  dominant-baseline: middle;
}
.confidence-text {
  font-weight: 600;
  margin-bottom: 8px;
}
.confidence-note {
  font-size: 13px;
  color: #6c757d;
}
.empty-state {
  text-align: center;
  padding: 40px;
  color: #6c757d;
}
.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
}
</style>