<template>
  <div class="training-page">
    <Sidebar activeItem="Đào tạo" />

    <main class="training-main">
      <!-- Header với title và nút thêm mới -->
      <header class="training-header">
        <div class="header-left">
          <h1 class="training-title">Quản lý Đào tạo</h1>
          <p class="page-subtitle">Tổ chức và quản lý các khóa học nội bộ</p>
        </div>
        <div class="header-right">
          <BaseButton variant="primary" @click="openCreateModal">
            <template #icon-left>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 5v14M5 12h14" />
              </svg>
            </template>
            Thêm khóa mới
          </BaseButton>
        </div>
      </header>

      <!-- Success Message -->
      <div v-if="successMessage" class="success-message">
        <p>{{ successMessage }}</p>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message">
        <p>{{ error }}</p>
        <BaseButton @click="loadCourses" variant="primary">Thử lại</BaseButton>
      </div>

      <div class="training-content">
        <!-- Stats Cards -->
        <div class="stats-row">
          <div v-for="stat in stats" :key="stat.label" class="stat-card">
            <div class="stat-info">
              <div class="stat-label">{{ stat.label }}</div>
              <div class="stat-value">{{ stat.value }}</div>
            </div>
          </div>
        </div>

        <!-- Filter Tabs -->
        <div class="filter-section">
          <div class="tabs">
            <button 
              v-for="tab in tabs" 
              :key="tab.value"
              :class="['tab-btn', { active: currentTab === tab.value }]"
              @click="currentTab = tab.value"
            >
              {{ tab.label }}
            </button>
          </div>

          <div class="search-box">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
            </svg>
            <input 
              v-model="searchQuery" 
              type="text"
              placeholder="Tìm tên khóa học, người dạy..."
            />
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="loading-state">
          <div class="spinner"></div>
          <p>Đang tải dữ liệu...</p>
        </div>

        <!-- Course List -->
        <div v-else class="course-list">
          <div class="table-card">
            <table class="base-table">
              <thead>
                <tr>
                  <th>TÊN KHÓA HỌC</th>
                  <th>GIẢNG VIÊN</th>
                  <th>PHÒNG BAN</th>
                  <th>HÌNH THỨC</th>
                  <th>THỜI GIAN</th>
                  <th>TRẠNG THÁI</th>
                  <th>HÀNH ĐỘNG</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="filteredCourses.length === 0">
                  <td colspan="7" class="text-center">Không có dữ liệu</td>
                </tr>
                <tr v-for="course in paginatedCourses" :key="course.trainingId || course.id">
                  <td>
                    <div class="course-cell">
                      <strong>{{ course.trainingName || course.name || 'Chưa có tên' }}</strong>
                      <span class="course-id">ID: {{ course.trainingId || course.id || 'N/A' }}</span>
                    </div>
                  </td>
                  <td>{{ course.trainer || course.instructor || '---' }}</td>
                  <td>{{ course.department || '---' }}</td>
                  <td><span class="format-tag" :class="(course.format || (course.location ? 'Offline' : 'Online')).toLowerCase()">{{ course.format || (course.location ? 'Offline' : 'Online') }}</span></td>
                  <td>{{ formatPeriod(course.startDate, course.endDate) }}</td>
                  <td><span :class="['status-badge', getStatusClass(course.status)]">{{ getStatusText(course.status) }}</span></td>
                  <td>
                    <div class="table-actions">
                      <button class="base-button" @click="editCourse(course)">Sửa</button>
                      <button class="base-button" @click="confirmDelete(course)">Xóa</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>

            <div class="table-footer">
              <div class="pagination-info">Hiển thị {{ filteredCourses.length === 0 ? 0 : (currentPage - 1) * pageSize + 1 }} - {{ Math.min(currentPage * pageSize, filteredCourses.length) }} trong {{ filteredCourses.length }} khóa học</div>
              <div class="pagination">
                <button :disabled="currentPage === 1" @click="changePage(currentPage - 1)">‹</button>
                <button v-for="n in totalPages" :key="n" :class="{ active: currentPage === n }" @click="changePage(n)">{{ n }}</button>
                <button :disabled="currentPage === totalPages" @click="changePage(currentPage + 1)">›</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <TrainingModal
      v-model="showModal"
      :item="editingItem"
      @save="handleSave"
      @close="showModal = false"
    />
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '../services/api'
import Sidebar from './Sidebar.vue'
import TrainingModal from './TrainingModal.vue'
import BaseButton from './base/BaseButton.vue'
import '@/CSS/TrainingAdmin.css'

const courses = ref([])
const loading = ref(false)
const error = ref('')
const successMessage = ref('')
const searchQuery = ref('')
const currentTab = ref('all')
const currentPage = ref(1)
const pageSize = ref(10)
const showModal = ref(false)
const editingItem = ref(null)

const tabs = [
  { label: 'Tất cả', value: 'all' },
  { label: 'Đang mở', value: 'open' },
  { label: 'Sắp tới', value: 'upcoming' },
  { label: 'Kết thúc', value: 'ended' }
]

const stats = computed(() => {
  const total = courses.value.length
  const open = courses.value.filter(c => c.status === 'open').length
  const upcoming = courses.value.filter(c => c.status === 'upcoming').length
  const students = courses.value.reduce((s, c) => s + (c.enrolledCount || 0), 0)
  return [
    { label: 'TỔNG KHÓA HỌC', value: total },
    { label: 'ĐANG MỞ ĐĂNG KÝ', value: open },
    { label: 'KHÓA HỌC SẮP TỚI', value: upcoming },
    { label: 'HỌC VIÊN ACTIVE', value: students }
  ]
})

const filteredCourses = computed(() => {
  let list = courses.value

  if (currentTab.value !== 'all') {
    list = list.filter(c => c.status === currentTab.value)
  }

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(c => 
      (c.trainingName || c.name || '').toLowerCase().includes(q) || 
      (c.trainer || c.instructor || '').toLowerCase().includes(q)
    )
  }

  return list
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredCourses.value.length / pageSize.value)))

const paginatedCourses = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  return filteredCourses.value.slice(start, end)
})

function changePage(page) {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
}

function openCreateModal() {
  editingItem.value = null
  showModal.value = true
}

function editCourse(item) {
  editingItem.value = { ...item }
  showModal.value = true
}

async function handleSave(data) {
  loading.value = true
  error.value = ''
  successMessage.value = ''
  
  try {
    const courseData = {
      trainingName: data.name,
      description: data.description || '',
      startDate: data.date,
      endDate: data.date,
      trainer: data.instructor,
      location: data.location || 'Phòng họp',
      format: data.format || 'Online',
      department: data.department || 'Công nghệ',
      status: data.status || 'open'
    }

    let response
    if (editingItem.value && (editingItem.value.trainingId || editingItem.value.id)) {
      const id = editingItem.value.trainingId || editingItem.value.id
      response = await api.training.update(id, courseData)
      successMessage.value = 'Cập nhật khóa học thành công!'
    } else {
      response = await api.training.create(courseData)
      successMessage.value = 'Thêm khóa học mới thành công!'
    }

    showModal.value = false
    await loadCourses()
    
    setTimeout(() => {
      successMessage.value = ''
    }, 3000)
    
  } catch (ex) {
    console.error('Save error:', ex)
    error.value = ex?.message || String(ex)
  } finally {
    loading.value = false
  }
}

async function confirmDelete(item) {
  const courseName = item.trainingName || item.name || 'khóa học'
  if (!confirm(`Bạn có chắc muốn xóa khóa học "${courseName}"?`)) return
  
  loading.value = true
  error.value = ''
  
  try {
    const id = item.trainingId || item.id
    await api.training.delete(id)
    successMessage.value = 'Xóa khóa học thành công!'
    await loadCourses()
    
    setTimeout(() => {
      successMessage.value = ''
    }, 3000)
  } catch (ex) {
    console.error('Delete error:', ex)
    error.value = ex?.message || String(ex)
  } finally {
    loading.value = false
  }
}

function getStatusText(status) {
  const map = { 
    open: 'Đang mở', 
    upcoming: 'Sắp tới', 
    ended: 'Kết thúc',
    completed: 'Đã hoàn thành'
  }
  return map[status] || status || 'Chưa xác định'
}

function formatPeriod(start, end) {
  if (!start && !end) return '---'
  const startFmt = start ? new Date(start).toLocaleDateString('vi-VN') : '---'
  const endFmt = end ? new Date(end).toLocaleDateString('vi-VN') : startFmt
  return `${startFmt} - ${endFmt}`
}

function getStatusClass(status) {
  const map = {
    open: 'open',
    upcoming: 'upcoming',
    ended: 'ended',
    completed: 'completed'
  }
  return map[status] || 'default'
}

watch([searchQuery, currentTab], () => {
  currentPage.value = 1
})

function determineStatus(startDate) {
  if (!startDate) return 'open'
  try {
    const today = new Date()
    const courseDate = new Date(startDate)
    const diffDays = (courseDate - today) / (1000 * 60 * 60 * 24)
    
    if (diffDays < -7) return 'ended'
    if (diffDays < 0) return 'ended'
    if (diffDays <= 7) return 'upcoming'
    return 'open'
  } catch (e) {
    return 'open'
  }
}

async function loadCourses() {
  loading.value = true
  error.value = ''
  
  try {
    const response = await api.training.getAll()
    
    let coursesData = []
    
    if (Array.isArray(response)) {
      coursesData = response
    } else if (response?.data && Array.isArray(response.data)) {
      coursesData = response.data
    } else if (response?.items && Array.isArray(response.items)) {
      coursesData = response.items
    } else if (response?.value && Array.isArray(response.value)) {
      coursesData = response.value
    } else if (response && typeof response === 'object') {
      const possibleArrays = Object.values(response).filter(val => Array.isArray(val))
      if (possibleArrays.length > 0) {
        coursesData = possibleArrays[0]
      } else {
        coursesData = []
      }
    }
    
    if (!Array.isArray(coursesData)) {
      coursesData = []
    }
    
    courses.value = coursesData.map(course => ({
      ...course,
      trainingId: course.trainingId || course.id,
      trainingName: course.trainingName || course.name || course.title,
      trainer: course.trainer || course.instructor,
      status: course.status || determineStatus(course.startDate)
    }))
    
    error.value = ''
    
  } catch (e) {
    console.error('Load courses error:', e)
    
    if (e.message?.includes('Network Error')) {
      error.value = 'Không thể kết nối đến server. Vui lòng kiểm tra backend.'
    } else if (e.message?.includes('404')) {
      error.value = 'API không tồn tại. Vui lòng kiểm tra cấu hình API.'
    } else {
      error.value = e?.message || 'Không thể tải danh sách khóa học'
    }
    
    courses.value = []
    
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadCourses()
})
</script>