<template>
  <div class="employees-page">
    <Sidebar activeItem="Danh sách nhân viên" />

    <main class="employees-main">
      <header class="employees-header">
        <div class="header-left">
          <h1 class="employee-title">Danh sách nhân viên</h1>
        </div>
        <div class="header-right">
          <BaseInput
            v-model="searchQuery"
            placeholder="Tìm theo tên, mã hoặc email..."
            class="search-input"
          >
            <template #icon>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8" />
                <path d="M21 21l-4.35-4.35" />
              </svg>
            </template>
          </BaseInput>
          <BaseButton variant="primary" @click="openAddModal">
            <template #icon-left>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 5v14M5 12h14" />
              </svg>
            </template>
            Thêm nhân viên
          </BaseButton>
        </div>
      </header>

      <div class="employees-content">
        <!-- Filters Area -->
        <div class="filters-section">
          <div class="filter-tabs">
            <button 
              class="filter-tab" 
              :class="{ active: filters.department === '' }"
              @click="filters.department = ''"
            >
              All Departments
            </button>
            <button 
              v-for="dept in departments" 
              :key="dept"
              class="filter-tab"
              :class="{ active: filters.department === dept }"
              @click="filters.department = dept"
            >
              {{ dept }}
            </button>
          </div>
          <div class="filter-actions">
            <BaseButton variant="secondary" @click="loadEmployees">
              <template #icon-left>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M23 4v6h-6M1 20v-6h6M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" />
                </svg>
              </template>
              Làm mới
            </BaseButton>
            <BaseButton variant="secondary" @click="exportToExcel">
              Xuất Excel
            </BaseButton>
          </div>
        </div>

        <!-- Results Info -->
        <div class="results-info">
          Showing {{ startIndex }}-{{ endIndex }} of {{ totalEmployees }} employees
        </div>

        <!-- Loading / Error -->
        <div v-if="employeeStore.loading" class="employees-feedback">
          <p class="loading">Đang tải danh sách nhân viên...</p>
        </div>
        <div v-if="employeeStore.error" class="employees-feedback error">
          <p>Lỗi khi tải dữ liệu: {{ employeeStore.error }}</p>
        </div>

        <!-- Table Area -->
        <BaseTable :columns="tableColumns" :data="paginatedEmployees" class="employees-table">
          <template #employeeCode="{ row }">
            <span class="employee-id">{{ row.employeeCode }}</span>
          </template>

          <template #name="{ row }">
            <div class="employee-profile">
              <div class="employee-avatar" @click="showQRModal(row)" :title="'Click để xem QR của ' + row.name">
                {{ row.name?.charAt(0) }}{{ row.name?.split(' ').pop()?.charAt(0) }}
              </div>
              <div class="employee-meta">
                <span class="name-text">{{ row.name }}</span>
                <span class="email-text">{{ row.email }}</span>
              </div>
            </div>
          </template>

          <template #position="{ row }">
            <span class="position-text">{{ row.position }}</span>
          </template>

          <template #department="{ row }">
            <span class="department-text">{{ row.department?.toUpperCase() }}</span>
          </template>

          <template #status="{ row }">
            <span :class="['status-badge', getStatusClass(row.status)]">
              {{ row.status?.toUpperCase() }}
            </span>
          </template>

          <template #actions="{ row }">
            <div class="table-actions">
              <button class="action-btn" @click="viewDetails(row)" title="Xem chi tiết">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="3" />
                  <path d="M22 12c-2.667 4.667-6 7-10 7s-7.333-2.333-10-7c2.667-4.667 6-7 10-7s7.333 2.333 10 7z" />
                </svg>
              </button>
              <button 
                class="action-btn" 
                @click="editEmployee(row)"
                title="Sửa"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
                </svg>
              </button>
              <button 
                class="action-btn" 
                @click="confirmDelete(row)"
                title="Xóa"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2M10 11v6M14 11v6" />
                </svg>
              </button>
            </div>
          </template>
        </BaseTable>

        <!-- Pagination -->
        <div class="pagination-section">
          <div class="pagination-btns">
            <BaseButton variant="secondary" :disabled="currentPage === 1" @click="currentPage--">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M15 18l-6-6 6-6" />
              </svg>
              Previous
            </BaseButton>
            
            <button 
              v-for="page in displayedPages" 
              :key="page"
              class="page-btn"
              :class="{ active: currentPage === page }"
              @click="currentPage = page"
            >
              {{ page }}
            </button>
            
            <BaseButton variant="secondary" :disabled="currentPage === totalPages" @click="currentPage++">
              Next
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 18l6-6-6-6" />
              </svg>
            </BaseButton>
          </div>
        </div>
      </div>
    </main>

    <!-- Modal Thêm/Sửa nhân viên -->
    <EmployeeModal
      v-model="showModal"
      :employee="editingEmployee"
      :departments="departments"
      :positions="positions"
      :loading="submitting"
      @save="handleSaveEmployee"
    />

    <!-- Modal xem chi tiết -->
    <EmployeeDetailModal
      v-model="showDetailModal"
      :employee="viewingEmployee"
      @edit="handleEditFromDetail"
    />

    <!-- MODAL QR CHIA 2 CỘT -->
    <div v-if="showQRModalFlag" class="qr-modal-overlay" @click.self="closeQRModal">
      <div class="qr-modal-content">
        <div class="qr-modal-header">
          <h3 class="qr-modal-title">MÃ QR NHÂN VIÊN</h3>
          <button class="qr-close-btn" @click="closeQRModal">×</button>
        </div>
        
        <div class="qr-modal-body">
          <div class="qr-two-columns">
            <!-- CỘT TRÁI: THÔNG TIN -->
            <div class="qr-info-column">
              <div class="qr-info-header">
                <div class="qr-info-avatar">{{ qrEmployee?.name?.charAt(0) }}{{ qrEmployee?.name?.split(' ').pop()?.charAt(0) }}</div>
                <div class="qr-info-title">
                  <h4>{{ qrEmployee?.name }}</h4>
                  <p>{{ qrEmployee?.employeeCode }}</p>
                </div>
              </div>
              
              <div class="qr-info-list">
                <div class="qr-info-item">
                  <span class="qr-info-label">ID nhân viên</span>
                  <span class="qr-info-value">{{ qrEmployee?.employeeCode }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Tên nhân viên</span>
                  <span class="qr-info-value">{{ qrEmployee?.name }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Phòng Ban</span>
                  <span class="qr-info-value">{{ qrEmployee?.department }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Chức vụ</span>
                  <span class="qr-info-value">{{ qrEmployee?.position }}</span>
                </div>
              </div>
            </div>
            
            <!-- CỘT PHẢI: QR CODE -->
            <div class="qr-code-column">
              <div class="qr-code-container">
                <canvas ref="qrCanvas"></canvas>
              </div>
              <p class="qr-code-note">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                  <line x1="9" y1="9" x2="15" y2="15"></line>
                  <line x1="15" y1="9" x2="9" y2="15"></line>
                </svg>
                Quét mã để xem thông tin
              </p>
            </div>
          </div>
        </div>
        
        <div class="qr-modal-footer">
          <BaseButton variant="secondary" @click="closeQRModal">Đóng</BaseButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useEmployeeStore } from '@/stores/employeeStore'
import QRCode from 'qrcode'
import Sidebar from './Sidebar.vue'
import EmployeeModal from './EmployeeModal.vue'
import EmployeeDetailModal from './EmployeeDetailModal.vue'
import BaseCard from './base/BaseCard.vue'
import BaseButton from './base/BaseButton.vue'
import BaseTable from './base/BaseTable.vue'
import BaseInput from './base/BaseInput.vue'
import '@/CSS/Employees.css'

const employeeStore = useEmployeeStore()

const searchQuery = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const showModal = ref(false)
const editingEmployee = ref(null)
const submitting = ref(false)

// State cho modal chi tiết
const showDetailModal = ref(false)
const viewingEmployee = ref(null)

// State cho modal QR
const showQRModalFlag = ref(false)
const qrEmployee = ref(null)
const qrCanvas = ref(null)

const filters = ref({
  department: '',
  position: '',
  status: ''
})

// Hiển thị số trang
const displayedPages = computed(() => {
  const total = totalPages.value
  const current = currentPage.value
  const delta = 2
  const range = []
  const rangeWithDots = []
  let l

  for (let i = 1; i <= total; i++) {
    if (i === 1 || i === total || (i >= current - delta && i <= current + delta)) {
      range.push(i)
    }
  }

  range.forEach((i) => {
    if (l) {
      if (i - l === 2) {
        rangeWithDots.push(l + 1)
      } else if (i - l !== 1) {
        rangeWithDots.push('...')
      }
    }
    rangeWithDots.push(i)
    l = i
  })

  return rangeWithDots
})

const tableColumns = [
  { key: 'employeeCode', label: 'EMPLOYEE ID', width: '120px' },
  { key: 'name', label: 'EMPLOYEE NAME' },
  { key: 'position', label: 'POSITION' },
  { key: 'department', label: 'DEPARTMENT' },
  { key: 'status', label: 'STATUS', width: '100px' },
  { key: 'actions', label: 'ACTIONS', width: '120px' }
]

// Derive filter options from loaded employees
const departments = computed(() => {
  return (employeeStore.departmentsCache || []).map(d => d.name).filter(Boolean).sort()
})

const positions = computed(() => {
  return (employeeStore.positionsCache || []).map(p => p.name).filter(Boolean).sort()
})

const statusOptions = computed(() => {
  const set = new Set(employeeStore.employees.map(e => e.status).filter(Boolean))
  return Array.from(set).sort()
})

// No local/mock employees allowed: remove local flags

// Lọc và tìm kiếm
const filteredEmployees = computed(() => {
  let list = employeeStore.employees || []
  
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(e => 
      e.name?.toLowerCase().includes(q) || 
      e.employeeCode?.toLowerCase().includes(q) || 
      e.email?.toLowerCase().includes(q)
    )
  }
  
  if (filters.value.department) list = list.filter(e => e.department === filters.value.department)
  if (filters.value.position) list = list.filter(e => e.position === filters.value.position)
  if (filters.value.status) list = list.filter(e => e.status === filters.value.status)
  
  return list
})

const totalEmployees = computed(() => filteredEmployees.value.length)
const totalPages = computed(() => Math.ceil(totalEmployees.value / pageSize.value))
const startIndex = computed(() => (currentPage.value - 1) * pageSize.value + 1)
const endIndex = computed(() => Math.min(currentPage.value * pageSize.value, totalEmployees.value))

const paginatedEmployees = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredEmployees.value.slice(start, start + pageSize.value)
})

async function loadEmployees() {
  await employeeStore.fetchEmployees()
}

function openAddModal() {
  editingEmployee.value = null
  showModal.value = true
}

function editEmployee(emp) {
  editingEmployee.value = { ...emp }
  showModal.value = true
}

// Mở modal chi tiết
function viewDetails(emp) {
  viewingEmployee.value = emp
  showDetailModal.value = true
}

// Mở modal QR
async function showQRModal(emp) {
  qrEmployee.value = emp
  showQRModalFlag.value = true
  
  await nextTick()
  generateQR()
}

// Tạo QR code
async function generateQR() {
  if (qrCanvas.value && qrEmployee.value) {
    const data = `ID: ${qrEmployee.value.employeeCode}
Tên: ${qrEmployee.value.name}
Phòng: ${qrEmployee.value.department}
Chức vụ: ${qrEmployee.value.position}`
    
    QRCode.toCanvas(qrCanvas.value, data, { 
      width: 220,
      margin: 2,
      color: {
        dark: '#1e293b',
        light: '#ffffff'
      }
    })
  }
}

// Đóng modal QR
function closeQRModal() {
  showQRModalFlag.value = false
  qrEmployee.value = null
}

// Xử lý khi từ modal chi tiết nhấn nút Sửa
function handleEditFromDetail(emp) {
  showDetailModal.value = false
  setTimeout(() => {
    editingEmployee.value = { ...emp }
    showModal.value = true
  }, 300)
}

async function handleSaveEmployee(employeeData) {
  submitting.value = true
  try {
    if (editingEmployee.value) {
      await employeeStore.updateEmployee(editingEmployee.value.id, employeeData)
    } else {
      await employeeStore.addEmployee(employeeData)
    }
    await loadEmployees()
    showModal.value = false
    alert('Lưu thành công!')
  } catch (error) {
    let detail = error?.message || 'Lỗi không xác định'
    if (error?.response) {
      try {
        const resp = error.response
        const data = resp.data
        detail += '\n\nServer response:\n' + (typeof data === 'string' ? data : JSON.stringify(data, null, 2))
      } catch (e) {
        detail += '\n\nKhông thể đọc chi tiết phản hồi từ server'
      }
    }
    alert(detail)
  } finally {
    submitting.value = false
  }
}

async function confirmDelete(emp) {
  if (confirm(`Bạn có chắc muốn xóa nhân viên ${emp.name}?`)) {
    await employeeStore.removeEmployee(emp.id)
    await loadEmployees()
  }
}

function getStatusClass(status) {
  switch (status?.toLowerCase()) {
    case 'đang làm việc':
    case 'active':
      return 'active';
    case 'tạm dừng':
    case 'inactive':
      return 'inactive';
    case 'nghỉ phép':
    case 'on leave':
      return 'warning';
    case 'nghỉ việc':
    case 'remote':
      return 'danger';
    default: return '';
  }
}

function exportToExcel() {
  alert('Đang xuất dữ liệu Excel...')
}

onMounted(async () => {
  await Promise.all([
    employeeStore.loadDepartments().catch(() => {}),
    employeeStore.loadPositions().catch(() => {})
  ])
  await loadEmployees()

  // No pending sync; rely on API
})
</script>