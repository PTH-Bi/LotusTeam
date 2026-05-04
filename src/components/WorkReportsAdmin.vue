<template>
  <div class="reports-page">
    <Sidebar activeItem="Báo cáo admin" />
    <main class="reports-main">
      <header class="reports-header glass">
        <h1 class="reports-title">Quản lý báo cáo công việc</h1>
      </header>
      <div class="reports-content">
        <BaseCard class="report-card">
          <!-- Filter bar -->
                <div class="filter-bar">
                  <div class="filter-group">
                    <label for="employee">Nhân viên:</label>
                    <select id="employee" class="filter-select" v-model="filters.employeeId">
                      <option value="">Tất cả</option>
                      <option v-for="emp in employeesList" :key="emp.id" :value="emp.id">{{ emp.name }}</option>
                    </select>
                  </div>
                  <div class="filter-group">
                    <label for="date-from">Từ ngày:</label>
                    <input type="date" id="date-from" class="filter-input" v-model="filters.dateFrom">
                  </div>
                  <div class="filter-group">
                    <label for="date-to">Đến ngày:</label>
                    <input type="date" id="date-to" class="filter-input" v-model="filters.dateTo">
                  </div>
                  <div class="filter-group">
                    <label for="status">Trạng thái:</label>
                    <select id="status" class="filter-select" v-model="filters.status">
                      <option value="">Tất cả</option>
                      <option value="Hoàn thành">Hoàn thành</option>
                      <option value="Đang thực hiện">Đang thực hiện</option>
                      <option value="Chưa bắt đầu">Chưa bắt đầu</option>
                    </select>
                  </div>
                  <button class="btn-filter" @click="loadReports" :disabled="loadingReports">Lọc</button>
                  <button class="btn-reset" @click="resetFilters">Reset</button>
                </div>

          <!-- Table -->
          <div class="table-responsive">
            <div v-if="loadingReports">Đang tải báo cáo...</div>
            <div v-if="reportsError" class="error">{{ reportsError }}</div>
            <table class="report-table">
              <thead>
                <tr>
                  <th>STT</th>
                  <th>Nhân viên</th>
                  <th>Công việc</th>
                  <th>Mô tả</th>
                  <th>Thời gian thực hiện</th>
                  <th>Kết quả</th>
                  <th>File nộp</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in reportData" :key="index">
                  <td>{{ index + 1 }}</td>
                  <td>{{ item.employeeName || item.employee || item.employeeFullName || '' }}</td>
                  <td>{{ item.task || item.title || item.work || '' }}</td>
                  <td class="description-cell">{{ item.description || item.note || '' }}</td>
                  <td>{{ item.date || item.reportDate || item.createdAt || '' }}</td>
                  <td>
                    <span :class="['status-badge', statusClass(item.status || item.reportStatus)]">
                      {{ item.status || item.reportStatus || '' }}
                    </span>
                  </td>
                  <td>{{ item.submittedFileName || item.fileName || '-' }}</td>
                  <td>
                    <button
                      class="btn-download"
                      :disabled="!(item.submittedFileName || item.fileName)"
                      @click="downloadSubmission(item)"
                    >
                      Tải file
                    </button>
                    <div class="review-actions">
                      <select
                        class="status-select"
                        v-model="item.status"
                        @change="setReviewStatus(item, item.status)"
                      >
                        <option value="Hoàn thành">Hoàn thành</option>
                        <option value="Đang thực hiện">Đang thực hiện</option>
                        <option value="Từ chối">Từ chối</option>
                      </select>
                      <button class="btn-apply" @click="setReviewStatus(item, item.status)">Cập nhật</button>
                    </div>
                    <button class="btn-detail" @click="openDetail(item)">Chi tiết</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Footer with pagination and summary -->
          <div class="report-footer">
            <div class="pagination">
              <button class="pagination-btn" disabled>‹</button>
              <button class="pagination-btn active">1</button>
              <button class="pagination-btn">2</button>
              <button class="pagination-btn">3</button>
              <span class="pagination-dots">...</span>
              <button class="pagination-btn">5</button>
              <button class="pagination-btn">›</button>
            </div>
            <div class="report-summary">
              <span>Tổng số: <strong>{{ reportData.length }} công việc</strong></span>
            </div>
          </div>
        </BaseCard>
      </div>
    </main>

    <!-- Modal chi tiết công việc -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-container">
        <div class="modal-header">
          <h3>Chi tiết công việc</h3>
          <button class="modal-close" @click="closeModal">×</button>
        </div>
        <div class="modal-body" v-if="selectedItem">
          <div class="detail-row">
            <span class="detail-label">Công việc:</span>
            <span class="detail-value">{{ selectedItem.task }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Mô tả chi tiết:</span>
            <span class="detail-value">{{ selectedItem.detailDescription || '...' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Thời gian bắt đầu:</span>
            <span class="detail-value">{{ selectedItem.startTime }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Thời gian kết thúc:</span>
            <span class="detail-value">{{ selectedItem.endTime }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Tình trạng thời gian:</span>
            <span class="detail-value" :class="deadlineClass(selectedItem.deadlineStatus)">
              {{ selectedItem.deadlineStatus }}
            </span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Người thực hiện:</span>
            <span class="detail-value">{{ selectedItem.employee }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Người hỗ trợ:</span>
            <span class="detail-value">{{ selectedItem.supporters || 'Không' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">File nộp:</span>
            <span class="detail-value">{{ selectedItem.submittedFileName || selectedItem.fileName || 'Không có' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Duyệt hoàn thành:</span>
            <span class="detail-value">
              <span :class="approvedClass(selectedItem.approved)">
                {{ selectedItem.approved ? 'Đã duyệt' : 'Chưa duyệt' }}
              </span>
            </span>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-close" @click="closeModal">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import '@/CSS/WorkReportsAdmin.css'

import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'
import { useEmployeeStore } from '@/stores/employeeStore'

const STORAGE_KEY = 'work_reports_submissions'

// Report data loaded from API
const reportData = ref([])
const loadingReports = ref(false)
const reportsError = ref(null)

// Filters
const filters = ref({
  employeeId: '',
  dateFrom: '',
  dateTo: '',
  status: ''
})

const employeeStore = useEmployeeStore()
const employeesList = computed(() => employeeStore.employees)

const loadSubmissionsFromStorage = () => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return []
  try {
    return JSON.parse(raw) || []
  } catch (error) {
    console.error('Failed to parse stored submissions:', error)
    return []
  }
}

const mergeSubmissions = (baseReports) => {
  const submissions = loadSubmissionsFromStorage()
  if (!Array.isArray(submissions) || submissions.length === 0) return baseReports

  const mapped = submissions.map((s) => ({
    id: s.id,
    employeeName: 'Nhân viên',
    task: `Nộp dự án: ${s.projectName || s.fileName}`,
    description: `File: ${s.fileName}`,
    date: s.submittedAt || '',
    status: s.status || 'Đã nộp',
    detailDescription: `File nộp: ${s.fileName} (${s.fileType || 'unknown'})`, 
    startTime: s.submittedAt || '',
    endTime: s.submittedAt || '',
    deadlineStatus: 'Đã nộp',
    employee: 'Nhân viên',
    supporters: '',
    approved: false,
    submittedFileName: s.fileName,
    fileUrl: s.fileUrl,
    fileDataUrl: s.fileDataUrl
  }))
  return [...mapped, ...baseReports]
}

const saveSubmissionsToStorage = (submissions) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(submissions || []))
}

const setReviewStatus = (item, newStatus) => {
  if (!item) return
  item.status = newStatus

  const submissions = loadSubmissionsFromStorage()
  const idx = submissions.findIndex((s) => s.id === item.id)
  if (idx >= 0) {
    submissions[idx].status = newStatus
    saveSubmissionsToStorage(submissions)
  }
}

const downloadSubmission = (item) => {
  if (!item) return

  if (item.fileDataUrl) {
    const a = document.createElement('a')
    a.href = item.fileDataUrl
    a.download = item.fileName || item.submittedFileName || 'download'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    return
  }

  if (item.fileUrl) {
    window.open(item.fileUrl, '_blank')
    return
  }

  alert('Không thể tải file: không tìm thấy dữ liệu tải xuống.')
}

async function loadReports() {
  loadingReports.value = true
  reportsError.value = null
  try {
    const params = {}
    if (filters.value.employeeId) params.employeeId = filters.value.employeeId
    if (filters.value.dateFrom) params.dateFrom = new Date(filters.value.dateFrom).toISOString()
    if (filters.value.dateTo) params.dateTo = new Date(filters.value.dateTo).toISOString()
    if (filters.value.status) params.status = filters.value.status

    const resp = await api.reports.employees(params)
    // Accept several response shapes
    if (Array.isArray(resp)) {
      reportData.value = mergeSubmissions(resp)
    } else if (resp && Array.isArray(resp.data)) {
      reportData.value = mergeSubmissions(resp.data)
    } else if (resp && resp.success && Array.isArray(resp.data)) {
      reportData.value = mergeSubmissions(resp.data)
    } else {
      // try nested data
      reportData.value = mergeSubmissions(resp?.data?.data ?? [])
    }
  } catch (err) {
    reportsError.value = err.message || String(err)
    console.error('Failed to load reports:', err)
    reportData.value = mergeSubmissions([])
  } finally {
    loadingReports.value = false
  }
}

function resetFilters() {
  filters.value = { employeeId: '', dateFrom: '', dateTo: '', status: '' }
  loadReports()
}

onMounted(async () => {
  // load employees for filter dropdown and then load reports
  await employeeStore.loadPositions().catch(() => {})
  await employeeStore.loadDepartments().catch(() => {})
  await employeeStore.fetchEmployees().catch(() => {})
  await loadReports()
})

const statusClass = (status) => {
  if (status === 'Hoàn thành') return 'status-success'
  if (status === 'Đang thực hiện') return 'status-warning'
  if (status === 'Từ chối') return 'status-rejected'
  if (status === 'Đã nộp') return 'status-info'
  return 'status-pending'
}

const deadlineClass = (deadline) => {
  if (deadline === 'Đúng hạn') return 'deadline-ontime'
  if (deadline === 'Sớm') return 'deadline-early'
  if (deadline === 'Trễ') return 'deadline-late'
  return ''
}

const approvedClass = (approved) => {
  return approved ? 'approved-yes' : 'approved-no'
}

// Modal
const showModal = ref(false)
const selectedItem = ref(null)

const openDetail = (item) => {
  selectedItem.value = item
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  selectedItem.value = null
}
</script>