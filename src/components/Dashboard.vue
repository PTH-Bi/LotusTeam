<template>
  <div class="dashboard-layout">
    <Sidebar activeItem="Tổng quan" />

    <main class="dashboard-main">
      <header class="dashboard-header glass">
        <div class="header-left">
          <h1 class="dashboard-title">Tổng quan hệ thống</h1>
          <p class="page-subtitle">Chào mừng trở lại, {{ currentUser?.fullName || 'Admin' }}</p>
        </div>
        <div class="header-right">
          <BaseInput
            v-model="searchQuery"
            placeholder="Tìm kiếm..."
            class="header-search"
          >
            <template #icon>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8" />
                <path d="M21 21l-4.35-4.35" />
              </svg>
            </template>
          </BaseInput>
          <BaseButton variant="secondary" iconOnly>
            <template #icon-left>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" />
              </svg>
            </template>
          </BaseButton>
        </div>
      </header>

      <div class="dashboard-content" v-if="!loading">
        <!-- Stats Grid -->
        <div class="stats-grid">
          <BaseCard v-for="stat in stats" :key="stat.label" class="stat-card" :hover="true">
            <div class="stat-icon" :style="{ color: stat.color, background: stat.bg }">
              <component :is="stat.iconComponent" />
            </div>
            <div class="stat-info">
              <p class="stat-label">{{ stat.label }}</p>
              <h3 class="stat-value">{{ stat.value }}</h3>
              <p class="stat-trend" :class="stat.trend > 0 ? 'up' : 'down'">
                {{ stat.trend > 0 ? '+' : '' }}{{ Math.abs(stat.trend) }}% so với tháng trước
              </p>
            </div>
          </BaseCard>
        </div>

        <!-- Dashboard Navigation -->
        <div class="dashboard-nav">
          <button 
            v-for="tab in tabs" 
            :key="tab.id"
            class="nav-button" 
            :class="{ active: activeTab === tab.id }"
            @click="handleTabChange(tab.id)"
          >
            {{ tab.name }}
          </button>
        </div>

        <!-- Tab Content -->
        <div class="tab-content">
          <!-- Tab 1: Tuyển dụng / Nhân sự -->
          <div v-if="activeTab === 'recruitment'" class="recruitment-tab">
            <div class="recruitment-header">
              <h2 class="recruitment-title">Nhân sự</h2>
              <div class="recruitment-subnav">
                <button class="subnav-item active">Tổng quan</button>
                <button class="subnav-item">Phòng ban</button>
                <button class="subnav-item">Sinh nhật</button>
              </div>
            </div>

            <div class="recruitment-stats">
              <div class="recruitment-stat-card">
                <span class="stat-label">Tổng nhân viên</span>
                <span class="stat-number">{{ hrData?.totalEmployees || 0 }}</span>
                <span class="stat-trend up">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M18 15l-6-6-6 6"/>
                  </svg>
                  {{ newHiresPercentChange }}%
                </span>
              </div>
              <div class="recruitment-stat-card">
                <span class="stat-label">Tuyển dụng mới (tháng này)</span>
                <span class="stat-number">{{ hrData?.newHiresThisMonth || 0 }}</span>
                <span class="stat-trend up">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M18 15l-6-6-6 6"/>
                  </svg>
                  +8.3%
                </span>
              </div>
              <div class="recruitment-stat-card">
                <span class="stat-label">Phòng ban</span>
                <span class="stat-number">{{ hrData?.departments?.length || 0 }}</span>
                <span class="stat-trend up">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M18 15l-6-6-6 6"/>
                  </svg>
                  +0%
                </span>
              </div>
            </div>

            <div class="recruitment-chart-section">
              <div class="section-header">
                <h3>Phân bố nhân sự theo phòng ban</h3>
                <div class="chart-legend">
                  <span class="legend-item">
                    <span class="legend-dot" style="background: #4f46e5"></span>
                    Số lượng nhân viên
                  </span>
                </div>
              </div>
              <canvas ref="recruitmentCanvas" class="recruitment-canvas" width="800" height="250"></canvas>
            </div>

            <div class="recruitment-table-section">
              <div class="table-header">
                <div class="table-tabs">
                  <button class="table-tab active">Danh sách phòng ban</button>
                  <button class="table-tab" @click="showBirthdays = !showBirthdays">
                    {{ showBirthdays ? 'Phòng ban' : 'Sinh nhật sắp tới' }}
                  </button>
                </div>
                <button class="export-btn">Xuất báo cáo</button>
              </div>

              <table class="recruitment-table" v-if="!showBirthdays">
                <thead>
                  <tr>
                    <th>Phòng ban</th>
                    <th>Số lượng nhân viên</th>
                    <th>Tỷ lệ</th>
                    <th>Trạng thái</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="dept in hrData?.departments" :key="dept.departmentId">
                    <td>{{ dept.departmentName }}</td>
                    <td>{{ dept.employeeCount }}</td>
                    <td>
                      <div class="progress-container">
                        <div class="progress-bar-wrapper">
                          <div class="progress-bar-fill" :style="{ width: getDepartmentPercentage(dept.employeeCount) + '%' }"></div>
                        </div>
                        <span class="progress-text">{{ getDepartmentPercentage(dept.employeeCount) }}%</span>
                      </div>
                    </td>
                    <td>
                      <span :class="['status-badge', dept.employeeCount > 10 ? 'active' : 'inactive']">
                        {{ dept.employeeCount > 10 ? 'Đủ nhân sự' : 'Thiếu nhân sự' }}
                      </span>
                    </td>
                    <td>
                      <button class="action-btn">•••</button>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- Sinh nhật sắp tới -->
              <div v-else>
                <div class="performers-list" v-if="hrData?.upcomingBirthdays?.length">
                  <div v-for="(birthday, index) in hrData.upcomingBirthdays" :key="birthday.employeeId" class="performer-item">
                    <div class="performer-rank">{{ index + 1 }}</div>
                    <div class="performer-info">
                      <div class="performer-name">{{ birthday.employeeName }}</div>
                      <div class="performer-role">{{ birthday.departmentName }}</div>
                    </div>
                    <div class="performer-score">
                      <span class="score-value">{{ formatDate(birthday.dateOfBirth) }}</span>
                      <span class="score-label">{{ birthday.age }} TUỔI</span>
                    </div>
                  </div>
                </div>
                <div v-else class="empty-state">
                  <p>Không có sinh nhật nào trong tháng này</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab 2: Báo cáo công việc -->
          <div v-if="activeTab === 'work'" class="work-tab">
            <div class="work-header">
              <h2 class="work-title">Báo cáo công việc</h2>
              <p class="work-subtitle">Tổng quan hiệu suất và tiến độ công việc theo thời gian thực</p>
            </div>

            <div class="work-grid">
              <!-- Biểu đồ tròn bên trái -->
              <BaseCard class="work-chart-card">
                <div class="chart-header">
                  <h3>Tình trạng hoàn thành công việc</h3>
                  <p class="chart-period">{{ currentMonthYear }}</p>
                </div>

                <div class="donut-chart-container">
                  <canvas ref="workDonutCanvas" class="donut-canvas" width="200" height="200"></canvas>
                  <div class="donut-center">
                    <span class="donut-percent">{{ workCompletionRate }}%</span>
                    <span class="donut-label">Hoàn thành</span>
                  </div>
                </div>

                <div class="work-stats">
                  <div class="work-stat-item">
                    <div class="stat-dot" style="background: #22c55e"></div>
                    <div class="stat-info">
                      <span class="stat-label">Đúng chỉ tiêu</span>
                      <span class="stat-value">{{ workOntime }}</span>
                      <span class="stat-trend up">+12%</span>
                    </div>
                  </div>
                  <div class="work-stat-item">
                    <div class="stat-dot" style="background: #f97316"></div>
                    <div class="stat-info">
                      <span class="stat-label">Muộn/Không hoàn thành</span>
                      <span class="stat-value">{{ workLate }}</span>
                      <span class="stat-trend down">-6%</span>
                    </div>
                  </div>
                </div>
              </BaseCard>

              <!-- Top 5 nhân viên xuất sắc nhất bên phải -->
              <BaseCard class="performers-card">
                <div class="performers-header">
                  <h3>Top 5 nhân viên xuất sắc nhất</h3>
                  <span class="month-badge">{{ currentMonthYear }}</span>
                </div>

                <div class="performers-list">
                  <div v-for="(performer, index) in topPerformers" :key="performer.name" class="performer-item">
                    <div class="performer-rank">{{ index + 1 }}</div>
                    <div class="performer-info">
                      <div class="performer-name">{{ performer.name }}</div>
                      <div class="performer-role">{{ performer.role }}</div>
                    </div>
                    <div class="performer-score">
                      <span class="score-value">{{ performer.score }}%</span>
                      <span class="score-label">HIỆU SUẤT</span>
                    </div>
                  </div>
                </div>

                <button class="view-all-btn">Xem bảng xếp hạng →</button>
              </BaseCard>
            </div>

            <!-- Chỉ số hiệu suất -->
            <div class="work-metrics">
              <div class="metric-item">
                <span class="metric-label">Thời gian trung bình</span>
                <span class="metric-value">4.2h</span>
                <span class="metric-trend up">+10%</span>
              </div>
              <div class="metric-item">
                <span class="metric-label">Hoàn thành đúng hạn</span>
                <span class="metric-value">78%</span>
                <span class="metric-trend up">+4%</span>
              </div>
              <div class="metric-item">
                <span class="metric-label">Quá hạn ưu tiên</span>
                <span class="metric-value">12</span>
                <span class="metric-trend up">+2</span>
              </div>
              <div class="metric-item">
                <span class="metric-label">Vận tốc nhóm</span>
                <span class="metric-value">8.4</span>
                <span class="metric-trend up">+1.2</span>
              </div>
            </div>
          </div>

          <!-- Tab 3: Điểm danh -->
          <div v-if="activeTab === 'attendance'" class="attendance-tab">
            <div class="attendance-header">
              <h2 class="attendance-title">Điểm danh</h2>
              <div class="attendance-subnav">
                <button class="subnav-item active" @click="attendanceSubTab = 'overview'">Tổng quan</button>
                <button class="subnav-item" @click="attendanceSubTab = 'details'">Chi tiết</button>
              </div>
              <button class="export-btn">Xuất báo cáo</button>
            </div>

            <div class="attendance-grid">
              <!-- Biểu đồ tròn bên trái -->
              <BaseCard class="attendance-chart-card">
                <h3>Phân bố đi làm đúng giờ</h3>
                
                <div class="attendance-donut-container">
                  <canvas ref="attendanceDonutCanvas" class="attendance-donut" width="200" height="200"></canvas>
                  <div class="attendance-donut-center">
                    <span class="attendance-percent">{{ onTimeRate }}%</span>
                    <span class="attendance-label">ĐÚNG GIỜ</span>
                  </div>
                </div>

                <div class="attendance-stats-compact">
                  <div class="attendance-stat-row">
                    <span class="stat-label">Đúng giờ</span>
                    <span class="stat-value">{{ onTimeCount }}</span>
                    <span class="stat-percent">{{ onTimeRate }}%</span>
                  </div>
                  <div class="attendance-stat-row">
                    <span class="stat-label">Đi muộn</span>
                    <span class="stat-value">{{ lateCount }}</span>
                    <span class="stat-percent">{{ lateRate }}%</span>
                  </div>
                  <div class="attendance-stat-row">
                    <span class="stat-label">Vắng mặt</span>
                    <span class="stat-value">{{ absentCount }}</span>
                    <span class="stat-percent">{{ absentRate }}%</span>
                  </div>
                </div>

                <div class="improvement-badge">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M18 15l-6-6-6 6"/>
                  </svg>
                  <span>Cải thiện +{{ improvementRate }}% so với tháng trước</span>
                </div>
              </BaseCard>

              <!-- Top 5 nhân viên đúng giờ nhất bên phải -->
              <BaseCard class="punctual-card">
                <div class="punctual-header">
                  <h3>Top 5 nhân viên đúng giờ nhất</h3>
                  <span class="month-badge">{{ currentMonthYear }}</span>
                </div>

                <div class="punctual-list">
                  <div v-for="(person, index) in topPunctual" :key="person.name" class="punctual-item">
                    <span class="punctual-rank">{{ index + 1 }}</span>
                    <div class="punctual-info">
                      <div class="punctual-name">{{ person.name }}</div>
                      <div class="punctual-role">{{ person.role }}</div>
                    </div>
                    <div class="punctual-record">
                      <span class="record-value">{{ person.record }}%</span>
                      <span class="record-label">CHUYÊN CẦN</span>
                    </div>
                  </div>
                </div>
              </BaseCard>
            </div>

            <!-- Ngoại lệ gần đây -->
            <BaseCard class="exceptions-card">
              <div class="exceptions-header">
                <h3>Điểm danh hôm nay</h3>
                <span class="date-badge">{{ todayDate }}</span>
              </div>

              <table class="exceptions-table">
                <thead>
                  <tr>
                    <th>Nhân viên</th>
                    <th>Phòng ban</th>
                    <th>Check-in</th>
                    <th>Check-out</th>
                    <th>Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="attendance in attendanceData?.todayAttendance" :key="attendance.employeeId">
                    <td>{{ attendance.employeeName }}</td>
                    <td>{{ attendance.departmentName }}</td>
                    <td>{{ formatTime(attendance.checkIn) }}</td>
                    <td>{{ formatTime(attendance.checkOut) }}</td>
                    <td>
                      <span :class="['status-badge', getAttendanceStatusClass(attendance.status)]">
                        {{ attendance.status }}
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>

              <div class="view-all-link" v-if="attendanceData?.todayAttendance?.length">
                <a href="#">Xem tất cả →</a>
              </div>
            </BaseCard>
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-else class="loading-container">
        <div class="loading-spinner"></div>
        <p>Đang tải dữ liệu...</p>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import BaseButton from './base/BaseButton.vue'
import BaseInput from './base/BaseInput.vue'
import api from '@/services/api'
import '@/CSS/Dashboard.css'

// ==================== INTERFACES ====================
interface DepartmentStat {
  departmentId: number | null
  departmentName: string
  employeeCount: number
}

interface Birthday {
  employeeId: number
  employeeName: string
  dateOfBirth: string
  departmentName: string
  age: number
}

interface HRDashboardData {
  departments: DepartmentStat[]
  newHiresThisMonth: number
  upcomingBirthdays: Birthday[]
  totalEmployees: number
}

interface AttendanceStat {
  date: string
  presentCount: number
  lateCount: number
  absentCount: number
}

interface TodayAttendance {
  employeeId: number
  employeeName: string
  departmentName: string
  checkIn: string | null
  checkOut: string | null
  status: string
}

interface AttendanceDashboardData {
  dateRange: string
  statistics: AttendanceStat[]
  todayAttendance: TodayAttendance[]
  summary: {
    totalDays: number
    averagePresent: number
    averageLate: number
  }
}

interface OverviewDashboardData {
  totalEmployees: number
  activeEmployees: number
  totalDepartments: number
  todayAttendance: number
  employeeStatus: {
    total: number
    active: number
    inactive: number
  }
}

// ==================== STATE ====================
const searchQuery = ref('')
const activeTab = ref('recruitment')
const loading = ref(true)
const showBirthdays = ref(false)
const attendanceSubTab = ref('overview')

// Data from API
const overviewData = ref<OverviewDashboardData | null>(null)
const hrData = ref<HRDashboardData | null>(null)
const attendanceData = ref<AttendanceDashboardData | null>(null)
const currentUser = ref<{ fullName: string } | null>(null)

// ==================== CONSTANTS ====================
const tabs = [
  { id: 'recruitment', name: '📊 Nhân sự' },
  { id: 'work', name: '📋 Báo cáo công việc' },
  { id: 'attendance', name: '✅ Điểm danh' }
]

// ==================== COMPUTED ====================
const stats = computed(() => [
  {
    key: 'totalEmployees',
    label: 'Tổng nhân viên',
    value: overviewData.value?.totalEmployees || 0,
    trend: 2.5,
    color: 'var(--primary)',
    bg: 'rgba(79, 70, 229, 0.1)',
    iconComponent: 'UsersIcon'
  },
  {
    key: 'activeEmployees',
    label: 'Nhân viên đang làm việc',
    value: overviewData.value?.activeEmployees || 0,
    trend: 3.2,
    color: '#4ade80',
    bg: 'rgba(74, 222, 128, 0.1)',
    iconComponent: 'CheckIcon'
  },
  {
    key: 'totalDepartments',
    label: 'Phòng ban',
    value: overviewData.value?.totalDepartments || 0,
    trend: 0,
    color: '#f59e0b',
    bg: 'rgba(245, 158, 11, 0.1)',
    iconComponent: 'BuildingIcon'
  },
  {
    key: 'todayAttendance',
    label: 'Đi làm hôm nay',
    value: overviewData.value?.todayAttendance || 0,
    trend: 5.1,
    color: '#ef4444',
    bg: 'rgba(239, 68, 68, 0.1)',
    iconComponent: 'CalendarIcon'
  }
])

// Work report data (mock - sẽ thay bằng API sau)
const workOntime = ref(742)
const workLate = ref(110)
const workCompletionRate = computed(() => Math.round((workOntime.value / (workOntime.value + workLate.value)) * 100))

const topPerformers = ref([
  { name: 'Phạm Thùy Linh', role: 'Trưởng phòng Marketing', score: 98.5 },
  { name: 'Nguyễn Văn An', role: 'Senior Developer', score: 96.2 },
  { name: 'Lê Thị Mai', role: 'Product Designer', score: 94.8 },
  { name: 'Trần Minh Quân', role: 'Account Manager', score: 92.4 },
  { name: 'Hoàng Bảo Trâm', role: 'Tuyển dụng', score: 91.7 }
])

// Attendance computed from API
const onTimeCount = computed(() => {
  if (!attendanceData.value?.statistics?.length) return 0
  const latest = attendanceData.value.statistics[attendanceData.value.statistics.length - 1]
  return latest?.presentCount || 0
})

const lateCount = computed(() => {
  if (!attendanceData.value?.statistics?.length) return 0
  const latest = attendanceData.value.statistics[attendanceData.value.statistics.length - 1]
  return latest?.lateCount || 0
})

const absentCount = computed(() => {
  if (!attendanceData.value?.statistics?.length) return 0
  const latest = attendanceData.value.statistics[attendanceData.value.statistics.length - 1]
  return latest?.absentCount || 0
})

const totalAttendanceCount = computed(() => onTimeCount.value + lateCount.value + absentCount.value)
const onTimeRate = computed(() => totalAttendanceCount.value > 0 ? Math.round((onTimeCount.value / totalAttendanceCount.value) * 100) : 0)
const lateRate = computed(() => totalAttendanceCount.value > 0 ? Math.round((lateCount.value / totalAttendanceCount.value) * 100) : 0)
const absentRate = computed(() => totalAttendanceCount.value > 0 ? Math.round((absentCount.value / totalAttendanceCount.value) * 100) : 0)
const improvementRate = computed(() => 2)

const topPunctual = ref([
  { name: 'Nguyễn Văn An', role: 'Product Designer', record: 100 },
  { name: 'Trần Thị Bình', role: 'Trưởng phòng Marketing', record: 100 },
  { name: 'Lê Văn Cường', role: 'Senior Engineer', record: 99.2 },
  { name: 'Phạm Thị Dung', role: 'HR Specialist', record: 98.5 },
  { name: 'Hoàng Văn Em', role: 'Data Analyst', record: 98.1 }
])

// Helper computed
const currentMonthYear = computed(() => {
  const date = new Date()
  return `Tháng ${date.getMonth() + 1}, ${date.getFullYear()}`
})

const todayDate = computed(() => {
  const date = new Date()
  return date.toLocaleDateString('vi-VN')
})

const newHiresPercentChange = computed(() => 8.3)

// ==================== CANVAS REFS ====================
const recruitmentCanvas = ref<HTMLCanvasElement | null>(null)
const workDonutCanvas = ref<HTMLCanvasElement | null>(null)
const attendanceDonutCanvas = ref<HTMLCanvasElement | null>(null)

// ==================== HELPER METHODS ====================
const getDepartmentPercentage = (count: number) => {
  const total = hrData.value?.totalEmployees || 1
  return Math.round((count / total) * 100)
}

const formatDate = (dateString: string) => {
  if (!dateString) return ''
  const date = new Date(dateString)
  return `${date.getDate()}/${date.getMonth() + 1}`
}

const formatTime = (timeString: string | null) => {
  if (!timeString) return '--:--'
  return timeString.substring(0, 5)
}

const getAttendanceStatusClass = (status: string) => {
  if (status?.includes('Đúng giờ')) return 'active'
  if (status?.includes('muộn')) return 'late'
  return 'inactive'
}

// ==================== API CALLS ====================
async function loadOverviewData() {
  try {
    const response = await api.dashboard.overview()
    if (response.data?.success) {
      overviewData.value = response.data.data
    }
  } catch (error) {
    console.error('Failed to load overview data:', error)
  }
}

async function loadHRData() {
  try {
    const response = await api.dashboard.hr()
    if (response.data?.success) {
      hrData.value = response.data.data
    }
  } catch (error) {
    console.error('Failed to load HR data:', error)
  }
}

async function loadAttendanceData() {
  try {
    const response = await api.dashboard.attendance()
    if (response.data?.success) {
      attendanceData.value = response.data.data
    }
  } catch (error) {
    console.error('Failed to load attendance data:', error)
  }
}

async function loadTabData() {
  switch (activeTab.value) {
    case 'recruitment':
      await loadHRData()
      setTimeout(() => drawRecruitmentChart(), 100)
      break
    case 'attendance':
      await loadAttendanceData()
      setTimeout(() => drawAttendanceDonut(), 100)
      break
    case 'work':
      setTimeout(() => drawWorkDonut(), 100)
      break
  }
}

function handleTabChange(tabId: string) {
  activeTab.value = tabId
  loadTabData()
}

// ==================== CHART DRAWING ====================
function drawRecruitmentChart() {
  const canvas = recruitmentCanvas.value
  if (!canvas || !hrData.value?.departments?.length) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const width = canvas.clientWidth
  const height = canvas.clientHeight
  canvas.width = width
  canvas.height = height

  const departments = hrData.value.departments
  const maxEmployees = Math.max(...departments.map((d: DepartmentStat) => d.employeeCount), 1)

  ctx.clearRect(0, 0, width, height)

  // Vẽ lưới
  ctx.strokeStyle = '#e5e7eb'
  ctx.lineWidth = 0.5
  ctx.beginPath()
  for (let i = 0; i <= 4; i++) {
    const y = height - 40 - (i / 4) * (height - 80)
    ctx.moveTo(50, y)
    ctx.lineTo(width - 30, y)
    ctx.stroke()
    
    ctx.fillStyle = '#9ca3af'
    ctx.font = '10px Inter, sans-serif'
    ctx.fillText(Math.round((maxEmployees / 4) * i).toString(), 30, y + 3)
  }

  // Vẽ cột
  const barWidth = (width - 100) / departments.length * 0.7
  const barSpacing = (width - 100) / departments.length

  departments.forEach((dept: DepartmentStat, i: number) => {
    const x = 50 + i * barSpacing + (barSpacing - barWidth) / 2
    const barHeight = ((dept.employeeCount / maxEmployees) * (height - 80))
    const y = height - 40 - barHeight

    ctx.fillStyle = '#4f46e5'
    ctx.fillRect(x, y, barWidth, barHeight)

    ctx.fillStyle = '#374151'
    ctx.font = '10px Inter, sans-serif'
    ctx.textAlign = 'center'
    let label = dept.departmentName
    if (label.length > 12) label = label.substring(0, 10) + '...'
    ctx.fillText(label, x + barWidth / 2, height - 20)
  })
}

function drawWorkDonut() {
  const canvas = workDonutCanvas.value
  if (!canvas) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const width = 200
  const height = 200
  canvas.width = width
  canvas.height = height

  ctx.clearRect(0, 0, width, height)

  const centerX = width / 2
  const centerY = height / 2
  const radius = 80
  const startAngle = -Math.PI / 2
  const onTimeAngle = (workCompletionRate.value / 100) * Math.PI * 2
  const lateAngle = Math.PI * 2 - onTimeAngle

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, startAngle, startAngle + onTimeAngle)
  ctx.lineTo(centerX, centerY)
  ctx.fillStyle = '#22c55e'
  ctx.fill()

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, startAngle + onTimeAngle, startAngle + onTimeAngle + lateAngle)
  ctx.lineTo(centerX, centerY)
  ctx.fillStyle = '#f97316'
  ctx.fill()

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius * 0.6, 0, Math.PI * 2)
  ctx.fillStyle = 'white'
  ctx.fill()
}

function drawAttendanceDonut() {
  const canvas = attendanceDonutCanvas.value
  if (!canvas) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const width = 200
  const height = 200
  canvas.width = width
  canvas.height = height

  ctx.clearRect(0, 0, width, height)

  const centerX = width / 2
  const centerY = height / 2
  const radius = 80
  const startAngle = -Math.PI / 2
  const onTimeAngle = (onTimeRate.value / 100) * Math.PI * 2
  const lateAngle = (lateRate.value / 100) * Math.PI * 2

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, startAngle, startAngle + onTimeAngle)
  ctx.lineTo(centerX, centerY)
  ctx.fillStyle = '#4f46e5'
  ctx.fill()

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, startAngle + onTimeAngle, startAngle + onTimeAngle + lateAngle)
  ctx.lineTo(centerX, centerY)
  ctx.fillStyle = '#f97316'
  ctx.fill()

  if (absentRate.value > 0) {
    ctx.beginPath()
    ctx.arc(centerX, centerY, radius, startAngle + onTimeAngle + lateAngle, startAngle + Math.PI * 2)
    ctx.lineTo(centerX, centerY)
    ctx.fillStyle = '#ef4444'
    ctx.fill()
  }

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius * 0.6, 0, Math.PI * 2)
  ctx.fillStyle = 'white'
  ctx.fill()
}

// ==================== WATCHERS & LIFE CYCLE ====================
onMounted(async () => {
  loading.value = true
  await Promise.all([
    loadOverviewData(),
    loadHRData(),
    loadAttendanceData()
  ])
  loading.value = false
  
  setTimeout(() => {
    drawRecruitmentChart()
    drawWorkDonut()
    drawAttendanceDonut()
  }, 100)
  
  window.addEventListener('resize', () => {
    drawRecruitmentChart()
  })
})

// ==================== ICON COMPONENTS ====================
const UsersIcon = { template: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>' }
const CheckIcon = { template: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 11-5.93-9.14M22 4L12 14.01l-3-3"/></svg>' }
const BuildingIcon = { template: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="4" y="2" width="16" height="20" rx="2" ry="2"/><path d="M9 22v-4h6v4M8 6h.01M16 6h.01M12 6h.01M12 10h.01M12 14h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01"/></svg>' }
const CalendarIcon = { template: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><path d="M16 2v4M8 2v4M3 10h18"/></svg>' }
</script>