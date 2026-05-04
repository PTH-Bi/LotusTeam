<template>
  <div class="feedback-page">
    <Sidebar activeItem="Góp ý" />
    <main class="feedback-main">
      <header class="feedback-header glass">
        <h1 class="feedback-title">Góp ý & Khiếu nại</h1>
      </header>
      <div class="feedback-content">
        <BaseCard class="feedback-card">
          <div class="feedback-sender" v-if="!isAdmin">
            <h2>Gửi góp ý ẩn danh</h2>
            <div class="feedback-type-select">
              <label for="feedback-type">Loại phản hồi</label>
              <select id="feedback-type" v-model="newFeedbackType" class="filter-input">
                <option v-for="type in feedbackTypes" :key="type" :value="type">{{ type }}</option>
              </select>
            </div>
            <textarea
              class="feedback-textarea"
              v-model="newFeedbackText"
              placeholder="Viết góp ý của bạn ở đây..."
              rows="5"
            ></textarea>
            <button class="btn-submit-feedback" @click="submitFeedback">Gửi góp ý</button>
            <p class="submission-message" v-if="submissionMessage">{{ submissionMessage }}</p>
          </div>

          <div v-if="isAdmin || feedbackList.length > 0">
            <div class="filter-bar">
              <div class="filter-group">
                <label for="reply-filter">Trạng thái phản hồi</label>
                <select
                  id="reply-filter"
                  class="filter-select"
                  v-model="replyFilter"
                  @change="applyFilters"
                >
                  <option value="all">Tất cả</option>
                  <option value="responded">Đã được phản hồi</option>
                  <option value="unresponded">Chưa được phản hồi</option>
                </select>
              </div>
              <div class="filter-group">
                <label for="category">Loại phản hồi</label>
                <select id="category" v-model="filters.category" class="filter-input">
                  <option value="">Tất cả</option>
                  <option v-for="type in feedbackTypes" :key="type" :value="type">{{ type }}</option>
                </select>
              </div>
              <div class="filter-group">
                <label for="search">Tìm kiếm</label>
                <input
                  type="text"
                  id="search"
                  class="filter-input"
                  v-model="filters.search"
                  placeholder="Nhập tiêu đề..."
                />
              </div>
              <button class="btn-filter" @click="applyFilters">Lọc</button>
            </div>

          <!-- Bảng danh sách góp ý (hiển thị ngang) -->
          <div class="feedback-list-horizontal">
            <div v-for="(item, idx) in feedbackList" :key="idx" class="feedback-card-horizontal">
              <div class="feedback-card-header">
                <span class="feedback-card-index">#{{ idx + 1 }}</span>
                <span :class="['status-badge', statusClass(item.status)]">{{ item.status }}</span>
              </div>
              <h3 class="feedback-card-title">{{ item.title || 'Góp ý #' + (idx + 1) }}</h3>
              <p class="feedback-card-text">{{ item.content }}</p>
              <div class="feedback-card-meta">
                <span class="feedback-category">{{ item.category || 'Chưa xác định' }}</span>
                <span>{{ item.sender }}</span>
                <span>{{ item.date }}</span>
              </div>
              <div class="feedback-card-reply">
                <strong>Phản hồi Admin:</strong>
                <p>{{ item.replies && item.replies.length ? item.replies.slice(-1)[0].text : 'Chưa có phản hồi' }}</p>
              </div>
              <button class="btn-view" @click="openReplyModal(item)">Phản hồi</button>
            </div>
          </div>

          <!-- Phân trang đơn giản -->
          <div class="pagination">
            <button class="pagination-btn" disabled>Trước</button>
            <span class="pagination-info">1 / 3</span>
            <button class="pagination-btn">Sau</button>
          </div>

          <div v-if="replyModalVisible" class="reply-modal-overlay" @click.self="closeReplyModal">
            <div class="reply-modal">
              <h3>Phản hồi góp ý</h3>
              <p><strong>Góp ý:</strong> {{ selectedFeedback?.content }}</p>
              <textarea
                class="feedback-textarea"
                v-model="adminReplyText"
                rows="4"
                placeholder="Viết phản hồi cho người dùng..."
              ></textarea>
              <div class="reply-actions">
                <button class="btn-filter" @click="submitReply">Gửi phản hồi</button>
                <button class="btn-reset" @click="closeReplyModal">Đóng</button>
              </div>
            </div>
          </div>
        </div>
        </BaseCard>
      </div>
    </main>
  </div>
</template>

<script setup>
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import '@/CSS/Feedback.css'
import { ref, computed, onMounted } from 'vue'
import axiosClient from '@/services/axiosClient'

const feedbackList = ref([])
const loading = ref(false)
const error = ref(null)

const userRole = ref('employee')
const currentUser = ref(null)
const isAdmin = computed(() => userRole.value === 'admin')

const newFeedbackText = ref('')
const newFeedbackType = ref('Góp ý')
const feedbackTypes = ['Góp ý', 'Khiếu nại', 'Báo lỗi', 'Cải tiến', 'Khác']
const submissionMessage = ref('')
const feedbackNotifications = ref(0)
const replyFilter = ref('all')

const selectedFeedback = ref(null)
const adminReplyText = ref('')
const replyModalVisible = ref(false)

const filters = ref({ status: '', category: '', search: '' })

const statusClass = (status) => {
  if (status === 'Chờ xử lý') return 'status-pending'
  if (status === 'Đang xử lý') return 'status-processing'
  return 'status-done'
}

const STORAGE_KEY = 'feedback_items'

const loadStoredFeedbacks = () => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return []
  try {
    return JSON.parse(raw) || []
  } catch (err) {
    console.warn('Không parse local feedback:', err)
    return []
  }
}

const getOwnerId = (item) => {
  return (item?.senderId || item?.userId || item?.sender || item?.username || '').toString().toLowerCase()
}

const isVisibleToCurrentUser = (item) => {
  if (isAdmin.value) return true
  const userId = currentUser.value?.id?.toString().toLowerCase() || currentUser.value?.username?.toString().toLowerCase()
  if (!userId) return false

  const ownerId = (item?.senderId || item?.userId || item?.employeeId || '').toString().toLowerCase()
  const ownerName = (item?.senderName || item?.sender || item?.username || '').toString().toLowerCase()

  return ownerId === userId || ownerName === userId
}

const saveStoredFeedbacks = (items) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(items || []))
}

const openReplyModal = (item) => {
  selectedFeedback.value = item
  adminReplyText.value = ''
  replyModalVisible.value = true
}

const closeReplyModal = () => {
  selectedFeedback.value = null
  adminReplyText.value = ''
  replyModalVisible.value = false
}

const submitReply = () => {
  if (!selectedFeedback.value || !adminReplyText.value.trim()) return
  const item = selectedFeedback.value
  item.replies = item.replies || []
  item.replies.push({
    text: adminReplyText.value.trim(),
    date: new Date().toLocaleString('vi-VN'),
    responder: 'Admin'
  })
  item.status = 'Đã xử lý'
  saveStoredFeedbacks(feedbackList.value)
  closeReplyModal()
}

async function tryEndpoints() {
  // Try likely endpoint variants in order
  const candidates = ['/Feedbacks', '/feedbacks', '/suggestions', '/suggestions/list', '/surveys']
  for (const ep of candidates) {
    try {
      const resp = await axiosClient.get(ep)
      return { resp, endpoint: ep }
    } catch (err) {
      // continue trying
    }
  }
  // last resort: try generic '/feedback'
  try {
    const resp = await axiosClient.get('/feedback')
    return { resp, endpoint: '/feedback' }
  } catch (err) {
    throw new Error('Không tìm thấy endpoint feedback nào. Vui lòng cung cấp endpoint API.')
  }
}

function normalizeResponse(resp) {
  if (!resp) return []
  if (Array.isArray(resp)) return resp
  if (resp.data && Array.isArray(resp.data)) return resp.data
  if (resp.items && Array.isArray(resp.items)) return resp.items
  if (resp.success && Array.isArray(resp.data)) return resp.data
  // try nested
  return resp?.data?.data ?? []
}

async function loadFeedbacks() {
  loading.value = true
  error.value = null

  userRole.value = 'employee'
  currentUser.value = null
  const userRaw = localStorage.getItem('user')
  if (userRaw) {
    try {
      const user = JSON.parse(userRaw)
      currentUser.value = user
      userRole.value = user.username === 'admin' ? 'admin' : 'employee'
    } catch {
      userRole.value = 'employee'
    }
  }

  try {
    const { resp } = await tryEndpoints()
    const apiItems = normalizeResponse(resp).map(item => ({
      id: item.id ?? item._id ?? Math.random().toString(36).slice(2),
      title: item.title ?? item.subject ?? item.name ?? '',
      content: item.content ?? item.message ?? item.body ?? '',
      sender: item.senderName ?? item.employeeName ?? item.user ?? 'Không rõ',
      senderId: item.senderId ?? item.userId ?? item.employeeId ?? item.creatorId ?? '',
      senderName: item.senderName ?? item.employeeName ?? item.user ?? 'Ẩn danh',
      date: item.date ?? item.createdAt ?? item.sentAt ?? new Date().toLocaleString('vi-VN'),
      status: item.status ?? item.state ?? 'Chờ xử lý',
      category: item.category ?? item.type ?? 'Góp ý',
      replies: item.replies || []
    }))


    const stored = loadStoredFeedbacks()
    const merged = [...stored]
    apiItems.forEach(apiItem => {
      if (!merged.find(m => m.id === apiItem.id)) merged.push(apiItem)
    })
    const visible = merged.filter(isVisibleToCurrentUser)
    feedbackList.value = visible
    saveStoredFeedbacks(merged)
  } catch (err) {
    // fallback to local storage only
    const stored = loadStoredFeedbacks()
    feedbackList.value = stored.filter(isVisibleToCurrentUser)
    error.value = err.message || String(err)
    console.error('Failed loading feedbacks:', err)
  } finally {
    loading.value = false
    applyFilters()
  }
}

function applyFilters() {
  let list = loadStoredFeedbacks().filter(isVisibleToCurrentUser)
  if (filters.value.search) {
    const q = filters.value.search.toLowerCase()
    list = list.filter((f) => (f.title + f.content).toLowerCase().includes(q))
  }

  if (replyFilter.value === 'responded') {
    list = list.filter((f) => {
      const hasReplies = f.replies && f.replies.length > 0
      const resolvedStatus = String(f.status || '').toLowerCase().includes('đã') || String(f.status || '').toLowerCase().includes('done') || String(f.status || '').toLowerCase().includes('replied')
      return hasReplies || resolvedStatus
    })
  } else if (replyFilter.value === 'unresponded') {
    list = list.filter((f) => {
      const hasReplies = f.replies && f.replies.length > 0
      const unresolvedStatus = !String(f.status || '').toLowerCase().includes('đã') && !String(f.status || '').toLowerCase().includes('done') && !String(f.status || '').toLowerCase().includes('replied')
      return !hasReplies && unresolvedStatus
    })
  }

  if (filters.value.status) {
    list = list.filter((f) => f.status === filters.value.status)
  }

  if (filters.value.category) {
    list = list.filter((f) => f.category === filters.value.category)
  }

  feedbackList.value = list
}

function resetFilters() {
  filters.value = { status: '', search: '' }
  applyFilters()
}

const NOTIFY_KEY = 'feedback_new_count'

const loadNotificationCount = () => {
  const raw = localStorage.getItem(NOTIFY_KEY)
  const v = Number(raw)
  feedbackNotifications.value = Number.isFinite(v) ? v : 0
}

const saveNotificationCount = () => {
  localStorage.setItem(NOTIFY_KEY, String(feedbackNotifications.value))
}

function addNotification() {
  feedbackNotifications.value += 1
  saveNotificationCount()
}

function clearNotifications() {
  feedbackNotifications.value = 0
  saveNotificationCount()
}

function submitFeedback() {
  if (!newFeedbackText.value.trim()) {
    submissionMessage.value = 'Vui lòng nhập nội dung góp ý.'
    return
  }

  const userId = currentUser.value?.id?.toString() || currentUser.value?.username?.toString() || 'unknown'
  const userName = currentUser.value?.username || 'Ẩn danh'

  const newItem = {
    id: Math.random().toString(36).slice(2),
    title: `${newFeedbackType.value} - Góp ý mới`,
    category: newFeedbackType.value,
    content: newFeedbackText.value.trim(),
    sender: 'Ẩn danh',
    senderId: userId,
    senderName: userName,
    date: new Date().toLocaleString('vi-VN'),
    status: 'Chờ xử lý',
    replies: []
  }
  const stored = loadStoredFeedbacks()
  stored.unshift(newItem)
  saveStoredFeedbacks(stored)
  feedbackList.value.unshift(newItem)
  addNotification()
  newFeedbackText.value = ''
  newFeedbackType.value = 'Góp ý'
  submissionMessage.value = 'Góp ý đã gửi (ẩn danh). Cảm ơn bạn!'
}

onMounted(() => {
  loadFeedbacks()
  loadNotificationCount()
})
</script>

<style src="@/CSS/Feedback.css" scoped></style>