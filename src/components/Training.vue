<template>
  <div class="training-page">
    <Sidebar activeItem="Đào tạo" />

    <main class="training-main">
      <header class="training-header glass">
        <h1 class="training-title">Thông báo Đào tạo</h1>
        <p class="page-subtitle">Danh sách thông báo từ Admin (chỉ xem, không thao tác).</p>
      </header>

      <div class="training-content">
        <div v-if="loading" class="loading-state">
          <div class="spinner"></div>
          <p>Đang tải dữ liệu...</p>
        </div>

        <div v-else-if="error" class="error-state">
          <p>{{ error }}</p>
          <button class="retry-btn" @click="loadNotifications">Thử lại</button>
        </div>

        <BaseCard class="training-card" v-else-if="notifications.length">
          <div
            class="training-item"
            v-for="(item, index) in notifications"
            :key="item.id || index"
          >
            <div class="training-item-header">
              <h2 class="training-item-title">{{ item.title }}</h2>
              <span class="training-item-date">{{ item.date }}</span>
            </div>
            <p class="training-item-desc">{{ item.description }}</p>

            <div class="training-item-meta">
              <span>Giảng viên: {{ item.instructor }}</span>
              <span>Hình thức: {{ item.format }}</span>
              <span>Địa điểm: {{ item.location }}</span>
            </div>

            <div :class="['training-status', item.statusClass]">{{ item.status }}</div>
          </div>
        </BaseCard>

        <div class="training-empty" v-else>
          Chưa có thông báo đào tạo mới.
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import api from '../services/api'
import '@/CSS/Training.css'

const notifications = ref([])
const loading = ref(false)
const error = ref('')

function getStatusInfo(startDate) {
  if (!startDate) return { status: 'Sắp diễn ra', class: 'status-upcoming' }
  
  const today = new Date()
  const courseDate = new Date(startDate)
  const diffDays = (courseDate - today) / (1000 * 60 * 60 * 24)
  
  if (diffDays < -7) {
    return { status: 'Đã hoàn thành', class: 'status-completed' }
  } else if (diffDays < 0) {
    return { status: 'Đã hoàn thành', class: 'status-completed' }
  } else if (diffDays <= 7) {
    return { status: 'Sắp diễn ra', class: 'status-upcoming' }
  } else {
    return { status: 'Mở đăng ký', class: 'status-open' }
  }
}

function formatDate(dateString) {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('vi-VN')
  } catch (e) {
    return ''
  }
}

async function loadNotifications() {
  loading.value = true
  error.value = ''
  try {
    console.log('Loading notifications...')
    const response = await api.training.getAll()
    console.log('Notifications response:', response)
    
    let data = []
    if (response && Array.isArray(response)) {
      data = response
    } else if (response && response.data && Array.isArray(response.data)) {
      data = response.data
    } else if (response && response.items && Array.isArray(response.items)) {
      data = response.items
    } else if (response && response.value && Array.isArray(response.value)) {
      data = response.value
    }
    
    notifications.value = data.map(item => {
      const statusInfo = getStatusInfo(item.startDate)
      return {
        id: item.trainingId || item.id,
        title: item.trainingName || item.name || item.title || 'Không có tiêu đề',
        date: formatDate(item.startDate) || 'Chưa có ngày',
        instructor: item.trainer || item.instructor || 'Chưa có',
        location: item.location || 'Phòng họp',
        format: item.format || (item.location ? 'Offline' : 'Online'),
        status: statusInfo.status,
        statusClass: statusInfo.class,
        description: item.description || ''
      }
    })
  } catch (e) {
    console.error('Load notifications error:', e)
    if (e.message?.includes('Network Error')) {
      error.value = 'Không thể kết nối đến server. Vui lòng kiểm tra backend.'
    } else {
      error.value = 'Không thể tải dữ liệu. Vui lòng thử lại sau.'
    }
    notifications.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadNotifications()
})
</script>