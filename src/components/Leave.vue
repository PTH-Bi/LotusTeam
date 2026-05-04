<template>
  <div class="dashboard">
    <Sidebar activeItem="Nghỉ phép" />
    <div class="main-content">
      <div class="leave-container">
        <!-- Header với tiêu đề -->
        <div class="page-header">
          <div>
            <h1 class="leave-title">Danh sách đơn nghỉ phép</h1>
            <p class="page-subtitle">Quản lý và phê duyệt các yêu cầu nghỉ phép của nhân viên</p>
          </div>
        </div>

        <!-- Stats Cards -->
        <div class="stats-cards">
          <div class="stat-item">
            <span class="stat-label">TỔNG YÊU CẦU</span>
            <span class="stat-value large">{{ totalRequests }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">CHƯA DUYỆT</span>
            <span class="stat-value warning">{{ pendingCount }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">ĐÃ DUYỆT</span>
            <span class="stat-value success">{{ approvedCount }}</span>
          </div>
        </div>

        <!-- Filter Section theo thiết kế image -->
        <div class="content-layout">
          <div class="requests-content">
            <!-- Filter Controls -->
            <div class="filter-bar">
              <select class="filter-select" v-model="filters.department">
                <option value="">Tất cả phòng ban</option>
                <option value="cong-nghe">Công nghệ</option>
                <option value="ki-thuat">Kĩ thuật</option>
                <option value="ke-toan">Kế toán</option>
              </select>
              
              <select class="filter-select" v-model="filters.status">
                <option value="">Tất cả trạng thái</option>
                <option value="pending">Chưa duyệt</option>
                <option value="approved">Đã duyệt</option>
              </select>
              
              <button class="btn-filter" @click="loadLeaves" :disabled="loading">
                Lọc
              </button>
            </div>

            <!-- Table -->
            <div class="table-container">
              <table class="leave-table">
                <thead>
                  <tr>
                    <th>PHÒNG BAN</th>
                    <th>LÝ DO NGHỈ</th>
                    <th>THỜI GIAN</th>
                    <th>TRẠNG THÁI</th>
                    <th>THAO TÁC</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-if="loading">
                    <td colspan="5" class="text-center">Đang tải dữ liệu...</td>
                  </tr>
                  <tr v-else-if="pagedRequests.length === 0">
                    <td colspan="5" class="text-center">Không có dữ liệu</td>
                  </tr>
                  <tr v-for="request in pagedRequests" :key="request.id">
                    <td>{{ request.department }}</td>
                    <td>{{ request.reason }}</td>
                    <td>{{ formatDate(request.time) }}</td>
                    <td>
                      <span :class="['status-badge', request.status]">
                        {{ getStatusText(request.status) }}
                      </span>
                    </td>
                    <td>
                      <button 
                        class="btn-view" 
                        @click="viewRequest(request)"
                        title="Xem chi tiết"
                      >
                        Xem
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Pagination -->
            <div class="pagination-section">
              <span class="pagination-info">
                Hiển thị {{ getDisplayRange() }} của {{ total }} yêu cầu
              </span>
              
              <div class="pagination-controls">
                <button 
                  class="page-nav" 
                  @click="prevPage" 
                  :disabled="page <= 1"
                >
                  Trước
                </button>
                
                <button 
                  v-for="p in paginationPages" 
                  :key="p"
                  :class="['page-btn', { active: p === page }]"
                  @click="goPage(p)"
                >
                  {{ p }}
                </button>
                
                <button 
                  v-if="totalPages > 5 && page < totalPages - 2"
                  class="page-dots"
                >
                  ...
                </button>
                
                <button 
                  v-if="totalPages > 5 && page < totalPages - 2"
                  class="page-btn"
                  @click="goPage(totalPages)"
                >
                  {{ totalPages }}
                </button>
                
                <button 
                  class="page-nav" 
                  @click="nextPage" 
                  :disabled="page >= totalPages"
                >
                  Sau
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Detail modal (giữ nguyên) -->
        <div v-if="showDetail" class="modal-overlay" @click.self="closeDetail">
          <div class="modal-content">
            <h3>Chi tiết yêu cầu</h3>
            <div class="detail-row"><strong>Mã NV:</strong> {{ selectedRequest.employeeId }}</div>
            <div class="detail-row"><strong>Họ và tên:</strong> {{ selectedRequest.employeeName }}</div>
            <div class="detail-row"><strong>Phòng ban:</strong> {{ selectedRequest.department }}</div>
            <div class="detail-row"><strong>Lý do:</strong> {{ selectedRequest.reason }}</div>
            <div class="detail-row"><strong>Thời gian:</strong> {{ selectedRequest.time }}</div>
            <div class="detail-row"><strong>Trạng thái:</strong> {{ getStatusText(selectedRequest.status) }}</div>
            <div class="detail-row"><strong>Loại nghỉ:</strong> {{ selectedRequest.leaveType || '-' }}</div>
            <div class="detail-row"><strong>Người duyệt:</strong> {{ selectedRequest.approver || '-' }}</div>

            <div class="modal-actions">
              <button class="btn-approve" @click="approveRequest(selectedRequest.id)">Duyệt</button>
              <button class="btn-reject" @click="rejectRequest(selectedRequest.id)">Từ chối</button>
              <button class="btn-close" @click="closeDetail">Đóng</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/authStore'
import '@/assets/Leave.css'

const authStore = useAuthStore()
const leaveRequests = ref([])
const loading = ref(false)

const filters = ref({ search: '', department: '', status: '' })
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

// Computed properties
const totalPages = computed(() => Math.ceil(total.value / pageSize.value))

const paginationPages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, page.value - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  
  if (end - start + 1 < maxVisible) {
    start = Math.max(1, end - maxVisible + 1)
  }
  
  for (let i = start; i <= end; i++) {
    pages.push(i)
  }
  return pages
})

const pagedRequests = computed(() => {
  return leaveRequests.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value)
})

const totalRequests = computed(() => total.value || leaveRequests.value.length)
const pendingCount = computed(() => leaveRequests.value.filter(r => r.status === 'pending').length)
const approvedCount = computed(() => leaveRequests.value.filter(r => r.status === 'approved').length)

const selectedRequest = ref(null)
const showDetail = ref(false)

// Methods
function formatDate(date) {
  if (!date) return ''
  return new Date(date).toLocaleDateString('vi-VN')
}

function getStatusText(status) {
  const statusMap = {
    'pending': 'Chờ duyệt',
    'approved': 'Đã duyệt',
    'rejected': 'Từ chối'
  }
  return statusMap[status] || status
}

function getDisplayRange() {
  if (total.value === 0) return '0 - 0'
  const start = (page.value - 1) * pageSize.value + 1
  const end = Math.min(page.value * pageSize.value, total.value)
  return `${start} - ${end}`
}

const LEAVE_STORAGE_KEY = 'employee_leave_requests'

function getStoredLeaves() {
  try {
    return JSON.parse(localStorage.getItem(LEAVE_STORAGE_KEY) || '[]')
  } catch (e) {
    return []
  }
}

function setStoredLeaves(leaves) {
  localStorage.setItem(LEAVE_STORAGE_KEY, JSON.stringify(leaves || []))
}

async function loadLeaves() {
  loading.value = true
  try {
    let requests = []

    if (api.leave?.list) {
      try {
        const response = await api.leave.list()
        const list = Array.isArray(response)
          ? response
          : (response?.data ?? response?.items ?? [])

        requests = list.map(item => ({
          id: item.id ?? item.requestId ?? item.leaveId,
          employeeId: item.employeeId ?? item.employeeCode ?? '',
          employeeName: item.employeeName ?? item.fullName ?? '',
          department: item.department ?? item.departmentName ?? '',
          reason: item.reason ?? item.title ?? '',
          time: item.fromDate && item.toDate ? `${item.fromDate} - ${item.toDate}` : item.time || '',
          status: (item.status ?? item.state ?? 'pending')
        }))
      } catch (err) {
        console.warn('API leave list unavailable, dùng dữ liệu local:', err)
      }
    }

    const localItems = getStoredLeaves().map(item => ({
      id: item.id,
      employeeId: item.sender || item.employeeId || '',
      employeeName: item.sender || '',
      department: item.department || 'Không rõ',
      reason: item.reason || '',
      time: item.fromDate && item.toDate ? `${item.fromDate} - ${item.toDate}` : item.time || '',
      status: item.status || 'pending'
    }))

    const allRequests = [...requests, ...localItems]
    const uniq = []
    const ids = new Set()
    for (const req of allRequests) {
      if (!ids.has(req.id)) {
        ids.add(req.id)
        uniq.push(req)
      }
    }

    leaveRequests.value = uniq
    total.value = uniq.length
  } catch (err) {
    console.error('Error loading leaves:', err)
  } finally {
    loading.value = false
  }
}

async function approveRequest(id) {
  if (!confirm('Xác nhận duyệt yêu cầu nghỉ phép này?')) return
  try {
    loading.value = true
    if (api.leave?.approve) {
      await api.leave.approve(id)
    }

    const leaves = getStoredLeaves()
    const mapped = leaves.map(item => {
      if (item.id === id) {
        return { ...item, status: 'approved' }
      }
      return item
    })
    setStoredLeaves(mapped)

    await loadLeaves()
  } catch (err) {
    alert('Duyệt thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
  }
}

async function rejectRequest(id) {
  if (!confirm('Bạn có chắc chắn muốn từ chối yêu cầu này?')) return
  try {
    loading.value = true
    if (api.leave?.reject) {
      await api.leave.reject(id)
    }

    const leaves = getStoredLeaves()
    const mapped = leaves.map(item => {
      if (item.id === id) {
        return { ...item, status: 'rejected' }
      }
      return item
    })
    setStoredLeaves(mapped)

    await loadLeaves()
  } catch (err) {
    alert('Từ chối thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
  }
}

function viewRequest(request) {
  selectedRequest.value = request
  showDetail.value = true
}

function closeDetail() {
  showDetail.value = false
  selectedRequest.value = null
}

function exportReport() {
  console.log('Export report')
}

function onPageSizeChange() {
  page.value = 1
}

function prevPage() {
  if (page.value > 1) page.value--
}

function nextPage() {
  if (page.value < totalPages.value) page.value++
}

function goPage(p) {
  page.value = p
}

onMounted(() => {
  loadLeaves()
})
</script>