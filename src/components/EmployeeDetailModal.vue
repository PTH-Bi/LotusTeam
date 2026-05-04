<template>
  <div v-if="modelValue" class="detail-modal-overlay" @click.self="close">
    <BaseCard class="detail-modal" :glass="true">
      <template #header>
        <div class="modal-header-content">
          <h3 class="modal-title">Thông tin nhân viên</h3>
          <BaseButton variant="ghost" iconOnly @click="close">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </BaseButton>
        </div>
      </template>

      <div class="detail-content">
        <div v-if="loading" class="modal-loading">Đang tải thông tin nhân viên...</div>
        <div v-else-if="error" class="modal-error">Lỗi khi tải dữ liệu: {{ error }}</div>
        <div v-else>
        <!-- Thông tin chia 2 cột ngang -->
        <div class="info-grid">
          <!-- Cột trái: Thông tin cá nhân -->
          <div class="info-column">
            <h4 class="section-title">Thông tin cá nhân</h4>
            <div class="info-row"><strong>Họ và tên:</strong> {{ employee?.name }}</div>
            <div class="info-row"><strong>Email:</strong> {{ employee?.email }}</div>
            <div class="info-row"><strong>Số điện thoại:</strong> {{ employee?.phone || 'Chưa cập nhật' }}</div>
            <div class="info-row"><strong>Ngày sinh:</strong> {{ formatDate(employee?.birthDate) || 'Chưa cập nhật' }}</div>
          </div>

          <!-- Cột phải: Thông tin công việc -->
          <div class="info-column">
            <h4 class="section-title">Thông tin công việc</h4>
            <div class="info-row"><strong>Mã nhân viên:</strong> {{ employee?.employeeCode }}</div>
            <div class="info-row"><strong>Phòng ban:</strong> {{ employee?.department }}</div>
            <div class="info-row"><strong>Chức vụ:</strong> {{ employee?.position }}</div>
            <div class="info-row"><strong>Ngày bắt đầu:</strong> {{ formatDate(employee?.startDate) || 'Chưa cập nhật' }}</div>
            <div class="info-row"><strong>Trạng thái:</strong> {{ employee?.status }}</div>
          </div>
        </div>

        <!-- 3 button: QR, Sửa, Thoát (nằm ngang) -->
        <div class="action-buttons">
          <BaseButton variant="primary" @click="toggleQR">
            {{ showQR ? 'Ẩn QR' : 'Xem QR' }}
          </BaseButton>
          <BaseButton variant="secondary" @click="editEmployee">
            Sửa
          </BaseButton>
          <BaseButton variant="secondary" @click="close">
            Thoát
          </BaseButton>
        </div>

        <!-- Khu vực hiển thị QR và thông tin (chỉ hiện khi showQR = true) -->
        <div v-if="showQR" class="qr-section">
          <!-- Avatar clickable để hiện QR -->
          <div class="qr-avatar-trigger" @click="toggleQRFromAvatar">
            <div class="employee-avatar-large">{{ employee?.name?.charAt(0) }}</div>
            <span class="avatar-hint">Click vào avatar để xem QR</span>
          </div>
          
          <!-- Chia 2 cột: thông tin QR bên trái, QR code bên phải -->
          <div class="qr-two-columns">
            <!-- Cột trái: thông tin có trong QR -->
            <div class="qr-info-column">
              <h4 class="qr-section-title">Thông tin QR</h4>
              <div class="qr-info-list">
                <div class="qr-info-item">
                  <span class="qr-info-label">Mã NV</span>
                  <span class="qr-info-value">{{ employee?.employeeCode }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Họ tên</span>
                  <span class="qr-info-value">{{ employee?.name }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Phòng ban</span>
                  <span class="qr-info-value">{{ employee?.department }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Chức vụ</span>
                  <span class="qr-info-value">{{ employee?.position }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Email</span>
                  <span class="qr-info-value">{{ employee?.email }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Trạng thái</span>
                  <span class="qr-info-value">{{ employee?.status }}</span>
                </div>
              </div>
              <div class="qr-scan-hint">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                  <line x1="9" y1="9" x2="15" y2="15"></line>
                  <line x1="15" y1="9" x2="9" y2="15"></line>
                </svg>
                <span>Quét mã để xem thông tin</span>
              </div>
            </div>
            
            <!-- Cột phải: QR code -->
            <div class="qr-code-column">
              <div class="qr-display">
                <canvas ref="qrCanvas"></canvas>
                <p class="qr-note">QR chứa thông tin liên hệ</p>
              </div>
            </div>
          </div>
        </div>
        </div>
      </div>
    </BaseCard>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, computed } from 'vue'
import QRCode from 'qrcode'
import BaseCard from './base/BaseCard.vue'
import BaseButton from './base/BaseButton.vue'
import api from '../services/api'

const props = defineProps({
  modelValue: Boolean,
  employee: Object
})

const emit = defineEmits(['update:modelValue', 'edit'])

const showQR = ref(false)
const qrCanvas = ref(null)
const loading = ref(false)
const error = ref(null)
const employeeData = ref(null)

const employee = computed(() => {
  return employeeData.value || props.employee
})

async function loadEmployeeDetail() {
  if (!props.employee) return

  try {
    loading.value = true
    error.value = null
    const detail = await api.employees.get(props.employee.id)
    // Normalize/map fields from backend to modal format
    if (!detail) {
      throw new Error('Không nhận được dữ liệu')
    }

    const d = detail.data ?? detail // handle wrapper { data: {...} }
    employeeData.value = {
      id: d.employeeId ?? d.id ?? props.employee.id,
      employeeCode: d.employeeCode ?? d.code ?? props.employee.employeeCode ?? '',
      name: d.fullName ?? d.name ?? d.displayName ?? props.employee.name ?? '',
      email: d.email ?? props.employee.email ?? '',
      phone: d.phone ?? d.mobile ?? props.employee.phone ?? '',
      department: d.departmentName ?? d.department ?? props.employee.department ?? '',
      position: d.positionName ?? d.position ?? props.employee.position ?? '',
      status: (d.status !== undefined) ? getStatusText(d.status) : (d.statusText ?? props.employee.status ?? ''),
      startDate: d.hireDate ?? d.startDate ?? props.employee.startDate ?? '',
      baseSalary: d.baseSalary ?? props.employee.baseSalary ?? 0,
      avatar: d.avatarUrl ?? d.avatar ?? props.employee.avatar ?? '',
      gender: (d.genderId === 1 || d.gender === 'male') ? 'male' : (d.genderId === 2 || d.gender === 'female') ? 'female' : null,
      birthDate: d.dateOfBirth ?? d.birthDate ?? props.employee.birthDate ?? '',
      address: d.address ?? props.employee.address ?? ''
    }
    // If position name missing but backend returned a positionId, try fetching it
    try {
      if ((!employeeData.value.position || employeeData.value.position === '') && (d.positionId || d.position_id)) {
        const posId = d.positionId ?? d.position_id
        const posResp = await api.positions.get(posId)
        const pos = posResp?.data ?? posResp
        employeeData.value.position = pos?.name ?? pos?.positionName ?? pos?.title ?? employeeData.value.position
      }
    } catch (posErr) {
      console.warn('Failed to fetch position name for id', d.positionId ?? d.position_id, posErr)
    }
  } catch (err) {
    console.error('Failed to load employee detail:', err)
    employeeData.value = null
    error.value = err.message || String(err)
  } finally {
    loading.value = false
  }
}

// Reuse mapping helpers from store logic
function getStatusText(statusValue) {
  switch (statusValue) {
    case 0: return 'Đã nghỉ việc'
    case 1: return 'Đang làm việc'
    case 2: return 'Nghỉ phép'
    case 3: return 'Tạm ngừng'
    default: return statusValue || ''
  }
}

// Format ngày tháng theo kiểu Việt Nam
function formatDate(dateString) {
  if (!dateString) return null
  const date = new Date(dateString)
  return date.toLocaleDateString('vi-VN')
}

// Ẩn QR khi chuyển nhân viên khác hoặc đóng modal
watch(() => props.employee, () => {
  showQR.value = false
  // if modal is open, refresh detail
  if (props.modelValue) loadEmployeeDetail()
})

watch(() => props.modelValue, (val) => {
  if (!val) {
    showQR.value = false
    employeeData.value = null
  } else {
    // modal opened -> load detail
    loadEmployeeDetail()
  }
})

async function generateQR() {
  await nextTick()
  if (qrCanvas.value && employee.value) {
    // Dữ liệu QR đầy đủ thông tin hơn
    const data = `ID: ${employee.value.employeeCode}
Tên: ${employee.value.name}
Phòng: ${employee.value.department}
Chức vụ: ${employee.value.position}
Email: ${employee.value.email}
Trạng thái: ${employee.value.status}`
    
    QRCode.toCanvas(qrCanvas.value, data, { 
      width: 200,
      margin: 2,
      color: {
        dark: '#1e293b',
        light: '#ffffff'
      }
    })
  }
}

// Bật/tắt QR
function toggleQR() {
  showQR.value = !showQR.value
  if (showQR.value) {
    nextTick(() => generateQR())
  }
}

// Toggle QR từ avatar
function toggleQRFromAvatar() {
  showQR.value = true
  nextTick(() => generateQR())
}

function editEmployee() {
  emit('edit', employee.value || props.employee)
}

function close() {
  emit('update:modelValue', false)
}
</script>

<style scoped>
.detail-modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(4px);
  z-index: 1000;
  padding: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.detail-modal {
  width: 100%;
  max-width: 700px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.modal-title {
  font-size: 18px;
  font-weight: 700;
}

.detail-content {
  padding: 1.5rem;
}

/* Grid 2 cột cho thông tin */
.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-bottom: 1.5rem;
}

.info-column {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  margin: 0 0 0.75rem 0;
  color: var(--primary, #1e293b);
  border-bottom: 1px solid var(--glass-border, #e2e8f0);
  padding-bottom: 0.5rem;
}

.info-row {
  font-size: 14px;
  line-height: 1.5;
  display: flex;
  flex-wrap: wrap;
}

.info-row strong {
  min-width: 110px;
  font-weight: 600;
  color: var(--foreground, #1e293b);
}

.action-buttons {
  display: flex;
  justify-content: center;
  gap: 1rem;
  margin: 1rem 0;
}

/* QR Section */
.qr-section {
  margin-top: 1.5rem;
  border-top: 2px dashed var(--glass-border, #e2e8f0);
  padding-top: 1.5rem;
}

.qr-avatar-trigger {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 0.75rem;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.qr-avatar-trigger:hover {
  background: rgba(0, 0, 0, 0.05);
  transform: translateY(-2px);
}

.employee-avatar-large {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  background: var(--primary, #1e293b);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 20px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.avatar-hint {
  font-size: 13px;
  color: var(--primary, #1e293b);
  font-style: italic;
  opacity: 0.7;
}

.qr-two-columns {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-top: 1rem;
}

.qr-info-column {
  padding: 1rem;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 8px;
  border: 1px solid var(--glass-border, #e2e8f0);
}

.qr-section-title {
  font-size: 14px;
  font-weight: 600;
  margin: 0 0 1rem 0;
  color: var(--primary, #1e293b);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.qr-info-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.qr-info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0;
  border-bottom: 1px dashed rgba(0, 0, 0, 0.1);
}

.qr-info-label {
  font-size: 12px;
  font-weight: 500;
  color: #64748b;
  text-transform: uppercase;
}

.qr-info-value {
  font-size: 14px;
  font-weight: 600;
  color: #1e293b;
  text-align: right;
}

.qr-scan-hint {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 1.5rem;
  padding: 0.5rem;
  background: rgba(0, 0, 0, 0.03);
  border-radius: 4px;
  font-size: 12px;
  color: #64748b;
}

.qr-code-column {
  display: flex;
  justify-content: center;
  align-items: center;
}

.qr-display {
  text-align: center;
  padding: 1rem;
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

canvas {
  border: 1px solid var(--border, #d1d5db);
  padding: 0.5rem;
  background: white;
  max-width: 100%;
  margin: 0 auto;
  border-radius: 4px;
}

.qr-note {
  font-size: 11px;
  color: #64748b;
  margin-top: 0.5rem;
  font-style: italic;
}

/* Responsive: khi màn hình nhỏ thì chuyển về 1 cột */
@media (max-width: 600px) {
  .info-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .qr-two-columns {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .qr-info-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
  
  .qr-info-value {
    text-align: left;
  }
}
</style>