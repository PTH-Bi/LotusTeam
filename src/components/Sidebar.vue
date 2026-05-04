<template>
  <aside class="sidebar glass">
    <div class="sidebar-header" @click="goToDashboard">
      <div class="logo-box">
        <!-- Thay đổi từ chữ L sang hình ảnh logo -->
        <img src="@/assets/logo.jpg" alt="Lotus Logo" class="logo-image">
      </div>
      <div class="brand-info">
        <h1 class="brand-name">Lotus</h1>
        <p class="brand-tagline">Hệ thống quản trị</p>
      </div>
    </div>

    <nav class="sidebar-nav">
      <div v-for="item in menuItems" :key="item.title" class="nav-group">
        <div 
          :class="['nav-item', { active: isActive(item) }]"
          @click="navigateTo(item)"
        >
          <div class="nav-content">
            <span class="nav-title">{{ item.title }}</span>
            <span v-if="item.title === 'Góp ý' && feedbackNotificationCount > 0" class="notification-badge">{{ feedbackNotificationCount }}</span>
          </div>
          <span v-if="item.children" class="chevron" :class="{ rotated: isExpanded(item.title) }">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 18l6-6-6-6" />
            </svg>
          </span>
        </div>
        
        <transition name="expand">
          <div v-if="item.children && isExpanded(item.title)" class="submenu">
            <div 
              v-for="child in item.children" 
              :key="child.title"
              :class="['submenu-item', { active: isActive(child) }]"
              @click.stop="router.push(child.route)"
            >
              {{ child.title }}
            </div>
          </div>
        </transition>
      </div>
    </nav>

    <div class="sidebar-footer">
      <button class="logout-btn" @click="logout">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" />
        </svg>
        <span>Đăng xuất</span>
      </button>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import '@/CSS/Sidebar.css'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const feedbackNotificationCount = ref(0)

const loadFeedbackNotificationCount = () => {
  const raw = localStorage.getItem('feedback_new_count')
  const v = Number(raw)
  feedbackNotificationCount.value = Number.isFinite(v) ? v : 0
}

const clearFeedbackNotifications = () => {
  feedbackNotificationCount.value = 0
  localStorage.setItem('feedback_new_count', '0')
}

const props = defineProps({
  activeItem: {
    type: String,
    default: ''
  }
})

const allMenuItems = [
  { title: 'Tổng quan', route: '/dashboard', roles: ['admin'] },
  { title: 'Danh sách nhân viên', route: '/employees', roles: ['admin', 'employee'] },
  { title: 'Báo cáo công việc', route: '/work-reports', roles: ['employee'] },
  { title: 'Báo cáo công việc (Admin)', route: '/work-reports-admin', roles: ['admin'] },
  { title: 'Góp ý', route: '/feedback', roles: ['employee'] },
  { title: 'Góp ý (admin)', route: '/feedback-admin', roles: ['admin'] },
  { 
    title: 'Điểm danh', 
    route: '/attendanceDashboard',
    roles: ['admin', 'employee'],
    children: [
      { title: 'Chấm theo ngày', route: '/attendanceDashboard' },
      { title: 'Chấm theo năm', route: '/attendance-yearly' }
    ]
  },
  { title: 'Xem bảng lương', route: '/payroll', roles: ['admin', 'employee'] },
  { title: 'Nghỉ phép', route: '/leave', roles: ['admin'] },
  { title: 'Nghỉ phép', route: '/leave-request', roles: ['employee'] },
  { title: 'Tuyển dụng', route: '/recruitment', roles: ['admin'] },
  { title: 'CV ứng viên', route: '/cv', roles: ['admin'] },
  { title: 'Đào tạo', route: '/training', roles: ['employee'] },
  { title: 'Đào tạo (Admin)', route: '/training-admin', roles: ['admin'] },
  { title: 'Thiết lập tài khoản', route: '/setting', roles: ['admin', 'employee'] },
]

const menuItems = computed(() => {
  const userRole = String(authStore.user?.role || 'employee').toLowerCase()
  return allMenuItems.filter(item => item.roles.includes(userRole))
})

const expandedItems = ref(['Điểm danh'])

const isActive = (item) => {
  if (props.activeItem) {
    return item.title === props.activeItem
  }
  return route.path === item.route
}

const isExpanded = (title) => expandedItems.value.includes(title)

const toggleExpand = (title) => {
  const index = expandedItems.value.indexOf(title)
  if (index > -1) {
    expandedItems.value.splice(index, 1)
  } else {
    expandedItems.value.push(title)
  }
}

const navigateTo = (item) => {
  if (item.children) {
    toggleExpand(item.title)
    return
  }
  if (item.route) {
    router.push(item.route)
  }
  if (item.title === 'Góp ý') {
    clearFeedbackNotifications()
  }
}

const goToDashboard = () => router.push('/dashboard')

const logout = async () => {
  await authStore.logout()
  router.push('/login')
}

onMounted(() => {
  loadFeedbackNotificationCount()
})
</script>