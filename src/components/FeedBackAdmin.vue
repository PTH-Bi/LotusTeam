<template>
  <div class="feedback-admin-page">
    <Sidebar activeItem="Quản lý góp ý" />
    <main class="feedback-admin-main">
      <header class="feedback-admin-header glass">
        <h1 class="feedback-title">Quản lý góp ý</h1>
        <div class="filter-container">
          <select v-model="statusFilter" class="filter-select">
            <option value="all">Tất cả</option>
            <option value="Chờ">Chưa phản hồi</option>
            <option value="Đã xử lý">Đã phản hồi</option>
          </select>
        </div>
      </header>

      <section class="feedback-admin-content">
        
          <div class="feedback-list-card">
            <div class="feedback-list-header">
              <h2>Danh sách góp ý</h2>
              <div class="feedback-stats">Tổng: {{ filteredFeedbackList.length }}</div>
            </div>
            <div class="feedback-items-horizontal-scroll">
              <div v-if="filteredFeedbackList.length === 0" class="empty">Chưa có góp ý nào.</div>
              <div v-for="(item, index) in filteredFeedbackList" :key="item.id" class="feedback-item-horizontal-scroll">
                <div class="feedback-item-header">
                  <h3 class="feedback-title">Góp ý mới</h3>
                  <span :class="['status-badge', item.status === 'Chờ' ? 'status-wait' : 'status-done']">{{ item.status }}</span>
                </div>
                
                <p class="feedback-content">{{ item.content || item.text || item.body || 'Không có nội dung' }}</p>
                
                <div class="feedback-meta">
                  <span class="feedback-author">{{ item.sender || 'Ẩn danh' }}</span>
                  <span class="feedback-date">{{ item.date || 'Không rõ thời gian' }}</span>
                </div>

                <div class="feedback-reply-list" v-if="item.replies && item.replies.length">
                  <div v-for="(reply, rindex) in item.replies" :key="`${item.id}-reply-${rindex}`" class="feedback-reply-item">
                    <strong>Phản hồi Admin:</strong>
                    <span>{{ reply.text }}</span>
                  </div>
                </div>

                <div class="feedback-actions">
                  <button class="btn-reply" @click="toggleReplyInput(item)">
                    <span v-if="!item.showReplyInput">[Phản hồi]</span>
                    <span v-else>[Đóng]</span>
                  </button>
                  
                  <div v-if="item.showReplyInput" class="reply-input-container">
                    <textarea v-model="item.replyDraft" class="reply-input" placeholder="Nhập phản hồi..."></textarea>
                    <button class="btn-submit-reply" @click="submitReply(item)">Gửi</button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import Sidebar from './Sidebar.vue'
import '@/CSS/FeedBackAdmin.css'

const STORAGE_KEY = 'feedback_items'

const feedbackList = ref([])
const statusFilter = ref('all')

const filteredFeedbackList = computed(() => {
  if (statusFilter.value === 'all') {
    return feedbackList.value
  }
  return feedbackList.value.filter(item => item.status === statusFilter.value)
})

const loadFeedbacks = () => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    feedbackList.value = []
    return
  }
  try {
    feedbackList.value = JSON.parse(raw) || []
    // Thêm thuộc tính showReplyInput cho mỗi item
    feedbackList.value.forEach(item => {
      item.showReplyInput = false
    })
  } catch {
    feedbackList.value = []
  }
}

const saveFeedbacks = () => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(feedbackList.value))
}

const toggleReplyInput = (item) => {
  item.showReplyInput = !item.showReplyInput
  if (!item.showReplyInput) {
    item.replyDraft = ''
  }
}

const submitReply = (item) => {
  const text = (item.replyDraft || '').trim()
  if (!text) return
  item.replies = item.replies || []
  item.replies.push({
    text,
    date: new Date().toLocaleString('vi-VN')
  })
  item.status = 'Đã xử lý'
  item.replyDraft = ''
  item.showReplyInput = false
  saveFeedbacks()
}

onMounted(loadFeedbacks)
</script>