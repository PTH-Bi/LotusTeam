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
        <button v-else class="btn-success btn-large" disabled>
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
              <td :class="{ late: isLate(item.checkInTime) }">
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

    <!-- ==================== CAMERA MODAL ==================== -->
    <div class="modal" v-if="showCamera" @click.self="closeCamera">
      <div class="modal-content camera-modal">
        <div class="modal-header">
          <h3>{{ cameraMode === 'checkin' ? 'Check-in bằng khuôn mặt' : 'Check-out bằng khuôn mặt' }}</h3>
          <button class="close-btn" @click="closeCamera">&times;</button>
        </div>
        <div class="modal-body">
          <!-- Model loading -->
          <div class="model-loading" v-if="modelLoading">
            <div class="loading-spinner"></div>
            <p>Đang tải model nhận diện...</p>
          </div>

          <div class="camera-container" v-show="!modelLoading">
            <video ref="video" autoplay playsinline muted class="camera-video"></video>
            <canvas ref="overlayCanvas" class="overlay-canvas"></canvas>
            <canvas ref="canvas" style="display: none"></canvas>

            <!-- Camera initializing overlay -->
            <div class="camera-overlay" v-if="!cameraReady">
              <div class="loading-spinner"></div>
              <p>Đang khởi tạo camera...</p>
            </div>

            <!-- Face detection status badge -->
            <div v-if="cameraReady && !modelLoading" class="face-status-badge" :class="faceStatusClass">
              {{ faceStatusText }}
            </div>
          </div>

          <div class="camera-instructions" v-if="!modelLoading">
            <p>👉 Hướng mặt vào camera — TỰ ĐỘNG nhận diện và chụp</p>
            <p>👉 Đảm bảo đủ ánh sáng, mặt rõ ràng, không bị che khuất</p>
            <p>👉 Chỉ 1 khuôn mặt trong khung hình</p>
          </div>

          <div class="preview-area" v-if="capturedImage">
            <h4>Ảnh đã chụp:</h4>
            <img :src="capturedImage" class="preview-image" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeCamera">Hủy</button>
          <span v-if="isProcessing" class="processing-text">
            <div class="loading-spinner-sm"></div> Đang xử lý...
          </span>
        </div>
      </div>
    </div>

    <!-- ==================== REGISTER MODAL ==================== -->
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
            <div class="camera-container small" style="position: relative">
              <video ref="registerVideo" autoplay playsinline muted class="camera-video"></video>
              <canvas ref="registerOverlayCanvas" class="overlay-canvas"></canvas>
              <canvas ref="registerCanvas" style="display: none"></canvas>
              <div class="face-status-badge" :class="registerFaceStatusClass" v-if="registerCameraReady">
                {{ registerFaceStatusText }}
              </div>
            </div>
            <p class="auto-hint">💡 TỰ ĐỘNG chụp khi phát hiện khuôn mặt hợp lệ</p>
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
              <li>✓ CHỈ MỘT người trong khung hình</li>
            </ul>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeRegisterModal">Hủy</button>
          <button
            class="btn-primary"
            @click="submitRegister"
            :disabled="!registerCapturedImage || isProcessing"
          >
            {{ isProcessing ? 'Đang đăng ký...' : 'Đăng ký' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== RESULT MODAL ==================== -->
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
import * as faceapi from 'face-api.js'
import api from '../services/api'

// ─── Constants ───────────────────────────────────────────────
const MODELS_PATH = '/models'
const DETECT_INTERVAL_MS = 150        // Check mỗi 150ms cho mượt
const MIN_FACE_WIDTH = 120            // Khuôn mặt phải đủ lớn (tối thiểu 120px)
const MIN_FACE_HEIGHT = 120
const MAX_FACE_WIDTH = 500            // Tránh trường hợp quá gần camera
const MAX_FACE_HEIGHT = 500
const COMPRESSED_WIDTH = 480          // Resize về 480px (giảm ~70% dung lượng)
const COMPRESSED_QUALITY = 0.7        // JPEG quality 70%

export default {
  name: 'FaceAttendance',
  data() {
    return {
      // ── User ──
      currentUser: null,
      currentEmployeeId: null,
      currentEmployeeName: '',
      // ── Today status ──
      todayStatus: {
        hasCheckedIn: false, hasCheckedOut: false,
        checkInTime: null, checkOutTime: null,
        workingHours: null, status: '', statusClass: '',
        checkInStatus: '', checkInStatusText: '', message: ''
      },
      // ── History ──
      historyList: [],
      historyFilters: { fromDate: '', toDate: '' },
      // ── Modal flags ──
      showCamera: false,
      showRegisterModal: false,
      showResultModal: false,
      // ── Camera state ──
      cameraMode: 'checkin',
      cameraReady: false,
      registerCameraReady: false,
      isProcessing: false,
      modelLoading: false,
      modelsLoaded: false,
      // ── Detection state ──
      detectAnimationId: null,
      registerDetectAnimationId: null,
      // ── Captured images ──
      capturedImage: null,
      capturedImageBase64: null,
      registerCapturedImage: null,
      registerCapturedImageBase64: null,
      // ── Register ──
      employees: [],
      registerForm: { employeeId: null },
      // ── Result modal ──
      resultModal: { type: 'success', title: '', message: '', confidence: null }
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
    },
    faceStatusClass() {
      if (this.faceDetected) return 'status-detected'
      return 'status-waiting'
    },
    faceStatusText() {
      if (this.faceDetected) {
        if (this.faceTooSmall) return '⚠️ Hãy đến gần camera hơn'
        if (this.faceTooLarge) return '⚠️ Hãy lùi ra xa hơn'
        if (this.multipleFaces) return '⚠️ Chỉ một người trong khung hình'
        return '✅ Khuôn mặt hợp lệ - Đang xử lý...'
      }
      return '👤 Hướng mặt vào camera'
    },
    registerFaceStatusClass() {
      if (this.registerFaceDetected && !this.registerFaceTooSmall && !this.registerMultipleFaces) return 'status-detected'
      if (this.registerFaceDetected && this.registerFaceTooSmall) return 'status-warning'
      return 'status-waiting'
    },
    registerFaceStatusText() {
      if (this.registerFaceDetected) {
        if (this.registerFaceTooSmall) return '⚠️ Hãy đến gần camera hơn'
        if (this.registerMultipleFaces) return '⚠️ Chỉ một người trong khung hình'
        return '✅ Khuôn mặt hợp lệ - Đang chụp...'
      }
      return '👤 Hướng mặt vào camera'
    }
  },

  mounted() {
    this.loadUserInfo()
    this.fetchTodayStatus()
    this.fetchHistory()
    if (this.canRegisterAll) this.fetchEmployees()
  },

  beforeUnmount() {
    this.cleanup()
  },

  methods: {
    // ════════════════════════════════════════
    //  KHỞI TẠO
    // ════════════════════════════════════════
    loadUserInfo() {
      const userStr = localStorage.getItem('user')
      if (!userStr) return
      try {
        this.currentUser = JSON.parse(userStr)
        this.currentEmployeeId = this.currentUser?.employeeId || this.currentUser?.id
        this.currentEmployeeName = this.currentUser?.fullName || this.currentUser?.username || ''
        this.registerForm.employeeId = this.currentEmployeeId
      } catch (e) { console.error('Error parsing user:', e) }
    },

    // ════════════════════════════════════════
    //  LOAD FACE-API MODELS
    // ════════════════════════════════════════
    async loadModels() {
      if (this.modelsLoaded) return true
      this.modelLoading = true
      try {
        console.log('Loading face detection models...')
        await faceapi.nets.tinyFaceDetector.loadFromUri(MODELS_PATH)
        this.modelsLoaded = true
        console.log('Models loaded successfully')
        return true
      } catch (err) {
        console.error('Failed to load face-api models:', err)
        this.modelsLoaded = false
        alert('Không thể tải model nhận diện. Vui lòng kiểm tra thư mục /models')
        return false
      } finally {
        this.modelLoading = false
      }
    },

    // ════════════════════════════════════════
    //  MỞ CAMERA (CHECKIN/CHECKOUT)
    // ════════════════════════════════════════
    async openCamera(mode) {
      this.cameraMode = mode
      this.capturedImage = null
      this.capturedImageBase64 = null
      this.faceDetected = false
      this.faceTooSmall = false
      this.faceTooLarge = false
      this.multipleFaces = false
      this.showCamera = true

      await this.$nextTick()
      
      // Load models và init camera song song
      const modelsPromise = this.loadModels()
      const cameraPromise = this.initCamera()
      
      await Promise.all([modelsPromise, cameraPromise])
      
      if (this.modelsLoaded && this.cameraReady) {
        this.startDetection()
      }
    },

    async initCamera() {
      this.cameraReady = false
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          video: { 
            facingMode: 'user', 
            width: { ideal: 1280 }, 
            height: { ideal: 720 } 
          }
        })
        const videoEl = this.$refs.video
        if (!videoEl) return
        
        videoEl.srcObject = stream
        await new Promise((resolve) => {
          videoEl.onloadedmetadata = () => {
            videoEl.play()
            const overlay = this.$refs.overlayCanvas
            if (overlay) {
              overlay.width = videoEl.videoWidth
              overlay.height = videoEl.videoHeight
            }
            this.cameraReady = true
            resolve()
          }
        })
      } catch (error) {
        console.error('Camera error:', error)
        alert('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.')
        this.closeCamera()
      }
    },

    // ════════════════════════════════════════
    //  AUTO DETECTION LOOP (requestAnimationFrame)
    // ════════════════════════════════════════
    startDetection() {
      this.stopDetection()
      const detectLoop = async () => {
        if (!this.showCamera || !this.cameraReady || this.isProcessing) {
          this.detectAnimationId = requestAnimationFrame(detectLoop)
          return
        }
        
        await this.detectAndProcess()
        this.detectAnimationId = requestAnimationFrame(detectLoop)
      }
      this.detectAnimationId = requestAnimationFrame(detectLoop)
    },

    stopDetection() {
      if (this.detectAnimationId) {
        cancelAnimationFrame(this.detectAnimationId)
        this.detectAnimationId = null
      }
    },

    async detectAndProcess() {
      const videoEl = this.$refs.video
      if (!videoEl || videoEl.readyState !== 4) return

      try {
        // Detect all faces trong khung hình
        const detections = await faceapi.detectAllFaces(
          videoEl,
          new faceapi.TinyFaceDetectorOptions({ inputSize: 224, scoreThreshold: 0.5 })
        )

        // Vẽ bounding boxes
        this.drawFaceBoxes(detections, this.$refs.overlayCanvas, videoEl)

        // KIỂM TRA ĐIỀU KIỆN:
        
        // 1. Không có mặt
        if (detections.length === 0) {
          this.faceDetected = false
          return
        }

        // 2. Nhiều mặt → reject
        if (detections.length > 1) {
          this.faceDetected = true
          this.multipleFaces = true
          this.faceTooSmall = false
          this.faceTooLarge = false
          return
        }

        // 3. Một mặt → kiểm tra kích thước
        this.multipleFaces = false
        const face = detections[0]
        const box = face.box
        
        // Kiểm tra kích thước khuôn mặt
        const isValidSize = box.width >= MIN_FACE_WIDTH && 
                           box.height >= MIN_FACE_HEIGHT &&
                           box.width <= MAX_FACE_WIDTH &&
                           box.height <= MAX_FACE_HEIGHT
        
        this.faceTooSmall = box.width < MIN_FACE_WIDTH || box.height < MIN_FACE_HEIGHT
        this.faceTooLarge = box.width > MAX_FACE_WIDTH || box.height > MAX_FACE_HEIGHT
        
        if (!isValidSize) {
          this.faceDetected = true
          return
        }

        // TẤT CẢ ĐIỀU KIỆN ĐỀU HỢP LỆ → CHỤP VÀ XỬ LÝ
        this.faceDetected = true
        this.faceTooSmall = false
        this.faceTooLarge = false
        
        // Dừng detection và chụp ảnh
        this.stopDetection()
        this.capturePhoto()
        await this.processAttendance()
        
      } catch (err) {
        console.error('Detection error:', err)
      }
    },

    // ════════════════════════════════════════
    //  REGISTER DETECTION
    // ════════════════════════════════════════
    async openRegisterModal() {
      this.registerCapturedImage = null
      this.registerCapturedImageBase64 = null
      this.registerFaceDetected = false
      this.registerFaceTooSmall = false
      this.registerMultipleFaces = false
      this.showRegisterModal = true
      
      await this.$nextTick()
      
      const modelsPromise = this.loadModels()
      const cameraPromise = this.initRegisterCamera()
      
      await Promise.all([modelsPromise, cameraPromise])
      
      if (this.modelsLoaded && this.registerCameraReady) {
        this.startRegisterDetection()
      }
    },

    async initRegisterCamera() {
      this.registerCameraReady = false
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: 'user', width: { ideal: 1280 }, height: { ideal: 720 } }
        })
        const videoEl = this.$refs.registerVideo
        if (!videoEl) return
        
        videoEl.srcObject = stream
        await new Promise((resolve) => {
          videoEl.onloadedmetadata = () => {
            videoEl.play()
            const overlay = this.$refs.registerOverlayCanvas
            if (overlay) {
              overlay.width = videoEl.videoWidth
              overlay.height = videoEl.videoHeight
            }
            this.registerCameraReady = true
            resolve()
          }
        })
      } catch (error) {
        console.error('Register camera error:', error)
        this.registerCameraReady = false
      }
    },

    startRegisterDetection() {
      if (this.registerDetectAnimationId) {
        cancelAnimationFrame(this.registerDetectAnimationId)
      }
      
      const detectLoop = async () => {
        if (!this.showRegisterModal || !this.registerCameraReady || this.registerCapturedImage) {
          this.registerDetectAnimationId = requestAnimationFrame(detectLoop)
          return
        }
        
        await this.detectAndRegister()
        this.registerDetectAnimationId = requestAnimationFrame(detectLoop)
      }
      this.registerDetectAnimationId = requestAnimationFrame(detectLoop)
    },

    async detectAndRegister() {
      const videoEl = this.$refs.registerVideo
      if (!videoEl || videoEl.readyState !== 4) return

      try {
        const detections = await faceapi.detectAllFaces(
          videoEl,
          new faceapi.TinyFaceDetectorOptions({ inputSize: 224, scoreThreshold: 0.5 })
        )

        this.drawFaceBoxes(detections, this.$refs.registerOverlayCanvas, videoEl)

        // Kiểm tra điều kiện cho đăng ký
        if (detections.length === 0) {
          this.registerFaceDetected = false
          return
        }

        if (detections.length > 1) {
          this.registerFaceDetected = true
          this.registerMultipleFaces = true
          this.registerFaceTooSmall = false
          return
        }

        this.registerMultipleFaces = false
        const face = detections[0]
        const box = face.box
        
        const isValidSize = box.width >= MIN_FACE_WIDTH && box.height >= MIN_FACE_HEIGHT
        
        this.registerFaceTooSmall = box.width < MIN_FACE_WIDTH || box.height < MIN_FACE_HEIGHT
        
        if (!isValidSize) {
          this.registerFaceDetected = true
          return
        }

        // Hợp lệ → chụp ảnh đăng ký
        this.registerFaceDetected = true
        this.registerFaceTooSmall = false
        this.captureRegisterPhoto()
        
        // Dừng detection sau khi đã chụp
        if (this.registerDetectAnimationId) {
          cancelAnimationFrame(this.registerDetectAnimationId)
          this.registerDetectAnimationId = null
        }
        
      } catch (err) {
        console.error('Register detection error:', err)
      }
    },

    // ════════════════════════════════════════
    //  VẼ BOUNDING BOX
    // ════════════════════════════════════════
    drawFaceBoxes(detections, canvas, video) {
      if (!canvas) return
      const ctx = canvas.getContext('2d')
      ctx.clearRect(0, 0, canvas.width, canvas.height)
      if (!detections || detections.length === 0) return

      const scaleX = canvas.width / video.videoWidth
      const scaleY = canvas.height / video.videoHeight

      detections.forEach(detection => {
        const box = detection.box
        const isMulti = detections.length > 1
        const isTooSmall = box.width < MIN_FACE_WIDTH || box.height < MIN_FACE_HEIGHT
        const isTooLarge = box.width > MAX_FACE_WIDTH || box.height > MAX_FACE_HEIGHT
        
        // Chọn màu sắc dựa trên điều kiện
        let strokeColor = '#10b981' // mặc định xanh (tốt)
        if (isMulti) strokeColor = '#ef4444' // đỏ (nhiều mặt)
        else if (isTooSmall) strokeColor = '#f59e0b' // cam (quá nhỏ)
        else if (isTooLarge) strokeColor = '#f59e0b' // cam (quá lớn)
        
        ctx.strokeStyle = strokeColor
        ctx.lineWidth = 2
        ctx.strokeRect(
          box.x * scaleX,
          box.y * scaleY,
          box.width * scaleX,
          box.height * scaleY
        )

        // Label
        ctx.fillStyle = strokeColor
        ctx.font = '12px sans-serif'
        let label = `${Math.round(detection.score * 100)}%`
        if (isMulti) label = '⚠️ NHIỀU MẶT'
        else if (isTooSmall) label = '📏 ĐẾN GẦN HƠN'
        else if (isTooLarge) label = '📏 LÙI RA XA'
        
        ctx.fillText(
          label,
          box.x * scaleX,
          (box.y * scaleY) - 4
        )
      })
    },

    // ════════════════════════════════════════
    //  CHỤP & NÉN ẢNH (GIẢM 70% DUNG LƯỢNG)
    // ════════════════════════════════════════
    capturePhoto() {
      const video = this.$refs.video
      const canvas = this.$refs.canvas
      if (!video || !canvas) return

      // Resize về COMPRESSED_WIDTH để giảm dung lượng (~70%)
      const scale = COMPRESSED_WIDTH / video.videoWidth
      canvas.width = COMPRESSED_WIDTH
      canvas.height = Math.round(video.videoHeight * scale)

      const ctx = canvas.getContext('2d')
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height)
      
      // Quality 0.7 + resize → giảm ~70% dung lượng
      this.capturedImageBase64 = canvas.toDataURL('image/jpeg', COMPRESSED_QUALITY)
      this.capturedImage = this.capturedImageBase64
      
      console.log('Photo captured, size reduced')
    },

    captureRegisterPhoto() {
      const video = this.$refs.registerVideo
      const canvas = this.$refs.registerCanvas
      if (!video || !canvas) return

      const scale = COMPRESSED_WIDTH / video.videoWidth
      canvas.width = COMPRESSED_WIDTH
      canvas.height = Math.round(video.videoHeight * scale)

      const ctx = canvas.getContext('2d')
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height)
      this.registerCapturedImageBase64 = canvas.toDataURL('image/jpeg', COMPRESSED_QUALITY)
      this.registerCapturedImage = this.registerCapturedImageBase64
    },

    // ════════════════════════════════════════
    //  GỬI API
    // ════════════════════════════════════════
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

        if (response.data?.success) {
          const data = response.data.data
          this.resultModal = {
            type: 'success',
            title: this.cameraMode === 'checkin' ? 'Check-in thành công!' : 'Check-out thành công!',
            message: response.data.message || 'Chấm công thành công',
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
          // Mở lại camera để thử lại
          setTimeout(() => {
            if (this.showCamera && !this.isProcessing) {
              this.startDetection()
            }
          }, 2000)
        }
        this.showResultModal = true
      } catch (error) {
        console.error('Attendance failed:', error)
        this.resultModal = {
          type: 'error',
          title: 'Lỗi kết nối',
          message: error.response?.data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.',
          confidence: null
        }
        this.showResultModal = true
        // Mở lại camera để thử lại
        setTimeout(() => {
          if (this.showCamera && !this.isProcessing) {
            this.startDetection()
          }
        }, 2000)
      } finally {
        this.isProcessing = false
      }
    },

    async submitRegister() {
      if (!this.registerCapturedImageBase64) {
        alert('Vui lòng chụp ảnh khuôn mặt')
        return
      }
      if (!this.registerForm.employeeId) {
        alert('Vui lòng chọn nhân viên')
        return
      }
      
      this.isProcessing = true
      try {
        const response = await api.faceAttendance.registerFace(
          this.registerForm.employeeId,
          this.registerCapturedImageBase64
        )
        if (response.data?.success) {
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
            message: response.data?.message || 'Không thể đăng ký khuôn mặt.',
            confidence: null
          }
        }
        this.showResultModal = true
      } catch (error) {
        this.resultModal = {
          type: 'error',
          title: 'Lỗi',
          message: error.response?.data?.message || 'Có lỗi xảy ra.',
          confidence: null
        }
        this.showResultModal = true
      } finally {
        this.isProcessing = false
      }
    },

    // ════════════════════════════════════════
    //  ĐÓNG / DỌN DẸP
    // ════════════════════════════════════════
    stopAllCameras() {
      ['video', 'registerVideo'].forEach(ref => {
        const el = this.$refs[ref]
        if (el?.srcObject) {
          el.srcObject.getTracks().forEach(t => t.stop())
          el.srcObject = null
        }
      })
    },

    cleanup() {
      this.stopDetection()
      if (this.registerDetectAnimationId) {
        cancelAnimationFrame(this.registerDetectAnimationId)
        this.registerDetectAnimationId = null
      }
      this.stopAllCameras()
    },

    closeCamera() {
      this.cleanup()
      this.showCamera = false
      this.cameraReady = false
      this.faceDetected = false
    },

    closeRegisterModal() {
      if (this.registerDetectAnimationId) {
        cancelAnimationFrame(this.registerDetectAnimationId)
        this.registerDetectAnimationId = null
      }
      this.stopAllCameras()
      this.showRegisterModal = false
      this.registerCameraReady = false
    },

    closeResultModal() { 
      this.showResultModal = false 
    },

    // ════════════════════════════════════════
    //  FETCH DATA
    // ════════════════════════════════════════
    async fetchTodayStatus() {
      if (!this.currentEmployeeId) return
      try {
        const res = await api.faceAttendance.getToday(this.currentEmployeeId)
        if (res.data?.success) {
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
      } catch (error) { console.error('Failed to fetch today status:', error) }
    },

    async fetchHistory() {
      if (!this.currentEmployeeId) return
      try {
        const params = {}
        if (this.historyFilters.fromDate) params.fromDate = this.historyFilters.fromDate
        if (this.historyFilters.toDate) params.toDate = this.historyFilters.toDate
        const res = await api.faceAttendance.getHistory(this.currentEmployeeId, params)
        if (res.data?.success) this.historyList = res.data.data || []
      } catch (error) { console.error('Failed to fetch history:', error) }
    },

    async fetchEmployees() {
      try {
        const res = await api.employees.list({ status: 'Active' })
        this.employees = res.data.items || res.data || []
      } catch (error) { console.error('Failed to fetch employees:', error) }
    },

    resetFilters() {
      this.historyFilters.fromDate = ''
      this.historyFilters.toDate = ''
      this.fetchHistory()
    },

    // ════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════
    getStatusClass(status) {
      return { on_time: 'card-success', late: 'card-warning', early_leave: 'card-warning' }[status] || 'card-info'
    },
    getStatusMessage(data) {
      if (!data.hasCheckedIn) return 'Bạn chưa check-in hôm nay'
      if (!data.hasCheckedOut) return 'Bạn đã check-in, hãy nhớ check-out trước khi kết thúc ngày làm việc'
      return 'Đã hoàn thành chấm công hôm nay'
    },
    getHistoryStatusClass(status) {
      return { on_time: 'success', late: 'warning', early_leave: 'warning', absent: 'danger' }[status] || 'secondary'
    },
    getHistoryStatusText(status) {
      return { on_time: 'Đúng giờ', late: 'Đi muộn', early_leave: 'Về sớm', absent: 'Vắng mặt' }[status] || status
    },
    isLate(checkInTime) {
      if (!checkInTime) return false
      const t = new Date(checkInTime)
      return t.getHours() > 8 || (t.getHours() === 8 && t.getMinutes() > 30)
    },
    formatDate(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    },
    formatTime(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
    }
  }
}
</script>

<style scoped>
/* ── Base ── */
.face-attendance { padding: 20px; background: #f5f7fa; min-height: 100vh; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
.page-title { font-size: 24px; font-weight: 600; color: #1a1a2e; margin: 0; }

/* ── Buttons ── */
.btn-outline { background: transparent; border: 1px solid #4361ee; color: #4361ee; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 14px; transition: all 0.3s; }
.btn-outline:hover { background: #4361ee; color: white; }
.btn-outline:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-sm { padding: 4px 10px; font-size: 12px; }
.btn-primary { background: #4361ee; color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 500; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-warning { background: #f59e0b; color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; }
.btn-success { background: #10b981; color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: default; }
.btn-large { padding: 14px 28px; font-size: 16px; }
.btn-secondary { background: #e9ecef; color: #495057; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }

/* ── Status card ── */
.status-card { background: white; border-radius: 20px; padding: 24px; display: flex; justify-content: space-between; align-items: center; margin-bottom: 32px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }
.status-card.card-success { border-left: 4px solid #10b981; }
.status-card.card-warning { border-left: 4px solid #f59e0b; }
.status-card.card-info { border-left: 4px solid #4361ee; }
.status-icon { width: 80px; height: 80px; background: #f0f4ff; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 40px; }
.status-info h3 { margin: 0 0 12px 0; font-size: 18px; }
.status-details { display: flex; gap: 24px; margin-bottom: 8px; flex-wrap: wrap; }
.detail-item { display: flex; align-items: center; gap: 8px; }
.detail-item .label { color: #6c757d; font-size: 13px; }
.detail-item .value { font-weight: 500; color: #333; }
.badge { padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 500; }
.badge.late { background: #fee2e2; color: #ef4444; }
.badge.on-time { background: #d1fae5; color: #10b981; }
.status-message { margin: 8px 0 0 0; color: #6c757d; font-size: 13px; }

/* ── History ── */
.history-section { background: white; border-radius: 16px; padding: 20px; }
.section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-wrap: wrap; gap: 16px; }
.section-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.history-filters { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.filter-input { padding: 8px 12px; border: 1px solid #ddd; border-radius: 8px; }
.table-container { overflow-x: auto; }
.data-table { width: 100%; border-collapse: collapse; }
.data-table th, .data-table td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #eee; }
.data-table th { background: #f8f9fa; font-weight: 600; color: #495057; }
.late { color: #ef4444; }
.late-tag { background: #fee2e2; color: #ef4444; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 8px; }
.confidence-bar { width: 80px; height: 20px; background: #e9ecef; border-radius: 10px; overflow: hidden; position: relative; }
.confidence-fill { height: 100%; background: #10b981; border-radius: 10px; transition: width 0.3s; }
.confidence-bar span { position: absolute; left: 50%; top: 50%; transform: translate(-50%,-50%); font-size: 10px; font-weight: 500; color: #333; }
.status-badge { padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 500; }
.status-badge.success { background: #d1fae5; color: #10b981; }
.status-badge.warning { background: #fed7aa; color: #f59e0b; }
.status-badge.danger { background: #fee2e2; color: #ef4444; }
.empty-state { text-align: center; padding: 40px; color: #6c757d; }

/* ── Modal ── */
.modal { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.6); display: flex; justify-content: center; align-items: center; z-index: 1000; }
.modal-content { background: white; border-radius: 20px; width: 500px; max-width: 95%; max-height: 95vh; overflow-y: auto; }
.camera-modal { width: 600px; }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: 16px 20px; border-bottom: 1px solid #eee; }
.modal-header.success { background: #d1fae5; color: #10b981; }
.modal-header.error { background: #fee2e2; color: #ef4444; }
.modal-header.warning { background: #fed7aa; color: #f59e0b; }
.modal-body { padding: 20px; }
.modal-footer { padding: 16px 20px; border-top: 1px solid #eee; display: flex; justify-content: flex-end; align-items: center; gap: 10px; }
.close-btn { background: none; border: none; font-size: 24px; cursor: pointer; color: #999; }

/* ── Camera ── */
.camera-container { position: relative; background: #000; border-radius: 12px; overflow: hidden; margin-bottom: 16px; }
.camera-video { width: 100%; max-height: 400px; object-fit: cover; display: block; }
.camera-container.small .camera-video { max-height: 280px; }
.overlay-canvas {
  position: absolute; top: 0; left: 0;
  width: 100%; height: 100%;
  pointer-events: none;
}
.camera-overlay { position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); display: flex; flex-direction: column; justify-content: center; align-items: center; color: white; }

/* ── Face status badge ── */
.face-status-badge {
  position: absolute; bottom: 10px; left: 50%; transform: translateX(-50%);
  padding: 6px 16px; border-radius: 20px; font-size: 13px; font-weight: 500;
  white-space: nowrap; backdrop-filter: blur(4px);
}
.status-waiting { background: rgba(0,0,0,0.6); color: #ccc; }
.status-detected { background: rgba(16,185,129,0.85); color: white; }
.status-warning { background: rgba(245,158,11,0.85); color: white; }

/* ── Model loading ── */
.model-loading { display: flex; flex-direction: column; align-items: center; padding: 40px; gap: 12px; color: #6c757d; }

/* ── Spinners ── */
.loading-spinner { width: 40px; height: 40px; border: 3px solid rgba(67,97,238,0.2); border-top-color: #4361ee; border-radius: 50%; animation: spin 1s linear infinite; }
.loading-spinner-sm { width: 16px; height: 16px; border: 2px solid rgba(67,97,238,0.2); border-top-color: #4361ee; border-radius: 50%; animation: spin 1s linear infinite; display: inline-block; }
@keyframes spin { to { transform: rotate(360deg); } }

.processing-text { display: flex; align-items: center; gap: 8px; color: #6c757d; font-size: 14px; }
.auto-hint { font-size: 12px; color: #6c757d; margin: 4px 0 8px; text-align: center; }

/* ── Instructions ── */
.camera-instructions { background: #f8f9fa; padding: 12px; border-radius: 8px; margin-bottom: 16px; }
.camera-instructions p { margin: 4px 0; font-size: 13px; color: #6c757d; }
.preview-area { margin-top: 16px; text-align: center; }
.preview-image { max-width: 200px; border-radius: 12px; margin-top: 8px; }
.camera-section { text-align: center; margin-bottom: 16px; }
.instruction-list { margin: 8px 0 0 20px; color: #6c757d; font-size: 13px; }
.form-group { margin-bottom: 16px; }
.form-group label { display: block; margin-bottom: 6px; font-weight: 500; font-size: 14px; }
.form-control { width: 100%; padding: 8px 12px; border: 1px solid #ddd; border-radius: 8px; font-size: 14px; box-sizing: border-box; }

/* ── Result modal ── */
.result-modal { text-align: center; }
.result-icon { font-size: 64px; margin-bottom: 16px; }
.result-message { font-size: 16px; color: #333; margin-bottom: 20px; }
.confidence-circle { display: flex; justify-content: center; margin-bottom: 16px; }
.circular-chart { width: 100px; height: 100px; }
.circle-bg { fill: none; stroke: #eee; stroke-width: 3.8; }
.circle { fill: none; stroke: #10b981; stroke-width: 3.8; stroke-linecap: round; animation: progress 1s ease-out forwards; }
@keyframes progress { 0% { stroke-dasharray: 0, 100; } }
.percentage { fill: #333; font-size: 4px; text-anchor: middle; dominant-baseline: middle; }
.confidence-text { font-weight: 600; margin-bottom: 8px; }
.confidence-note { font-size: 13px; color: #6c757d; }

/* ── Responsive ── */
@media (max-width: 768px) {
  .face-attendance { padding: 10px; }
  .page-header { flex-direction: column; align-items: flex-start; gap: 12px; }
  .status-card { flex-direction: column; text-align: center; padding: 16px; gap: 16px; }
  .status-icon { width: 60px; height: 60px; font-size: 30px; }
  .status-details { flex-direction: column; gap: 8px; align-items: center; }
  .action-buttons { width: 100%; }
  .btn-large { width: 100%; }
  .section-header { flex-direction: column; align-items: flex-start; }
  .history-filters { flex-direction: column; width: 100%; }
  .filter-input { width: 100%; box-sizing: border-box; }
  .modal-content { width: 100%; margin: 0; border-radius: 16px 16px 0 0; max-height: 95vh; position: fixed; bottom: 0; left: 0; }
  .camera-video { max-height: 280px; }
}
@media (max-width: 480px) {
  .camera-video { max-height: 240px; }
  .data-table th, .data-table td { padding: 8px 10px; font-size: 13px; }
}
</style>