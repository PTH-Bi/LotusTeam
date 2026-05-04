<template>
  <div class="app-container">
    <sidebar active-item="CV" />

    <div class="cv-page">
      <main class="cv-main">
        <header class="cv-header">
          <h1 class="cv-title">Danh sách CV phù hợp</h1>

          <div class="filter-panel">
            <label>
              <input type="checkbox" v-model="showViewed" />
              Đã xem
            </label>
            <label>
              <input type="checkbox" v-model="showNotViewed" />
              Chưa xem
            </label>
          </div>
        </header>

        <!-- LOADING -->
        <div v-if="loading">Đang tải CV...</div>

        <!-- DANH SÁCH -->
        <section class="cv-grid">
          <article
            v-for="cv in filteredCVs"
            :key="cv.candidateCVID"
            class="cv-card"
            :class="{ approved: cv.isViewedByHR }"
          >
            <div class="cv-card-top">
              <div class="cv-card-thumb">
                <span>{{ getFileTypeLabel(cv.fileName) }}</span>
              </div>
              <div class="cv-card-file">
                <p class="file-label">{{ cv.fileName || 'Không rõ tên file' }}</p>
                <p class="email-line">{{ cv.candidateEmail || cv.email || 'Không có email' }}</p>
              </div>
            </div>

            <div class="cv-card-content">
              <div class="cv-card-details">
                <p class="info"><strong>Score</strong> {{ cv.score ?? '-' }}</p>
                <p class="info"><strong>Ngày</strong> {{ formatDate(cv.createdAt) }}</p>
              </div>

              <div class="cv-card-actions">
                <span
                  class="status"
                  :class="{ pending: !cv.isViewedByHR }"
                >
                  {{ cv.isViewedByHR ? 'Đã xem' : 'Chưa xem' }}
                </span>

                <button class="view-btn" @click="viewCV(cv)">
                  Xem
                </button>
              </div>
            </div>
          </article>

        </section>
      </main>
    </div>
  </div>
</template>

<script>
import Sidebar from './Sidebar.vue'
import '@/CSS/CV.css'


export default {
  name: 'CV',
  components: { Sidebar },

  data() {
    return {
      cvs: [],
      loading: false,
      showViewed: true,
      showNotViewed: true
    }
  },

  computed: {
    filteredCVs() {
      return this.cvs.filter(cv => {
        if (this.showViewed && cv.isViewedByHR) return true
        if (this.showNotViewed && !cv.isViewedByHR) return true
        return false
      })
    }
  },

  methods: {
    async fetchCVs() {
      try {
        this.loading = true

        const token = localStorage.getItem('token')
        console.log('Token:', token)

        const res = await fetch('https://localhost:7010/api/recruitment/cvs', {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        })

        console.log('Response status:', res.status)

        if (!res.ok) {
          if (res.status === 403) {
            console.error('Không có quyền truy cập. User cần role: HR_MANAGER, HR_STAFF, ADMIN, hoặc SUPER_ADMIN')
            alert('Bạn không có quyền xem danh sách CV. Vui lòng liên hệ HR.')
            return
          }
          throw new Error(`HTTP error! status: ${res.status}`)
        }

        const data = await res.json()
        this.cvs = data
      } catch (err) {
        console.error('Lỗi load CV:', err)
      } finally {
        this.loading = false
      }
    },

    async viewCV(cv) {
      window.open(cv.fileUrl, '_blank')

      try {
        await fetch(
          `https://localhost:7010/api/recruitment/cvs/${cv.candidateCVID}/view`,
          {
            method: 'POST',
            headers: {
              Authorization: `Bearer ${localStorage.getItem('token')}`
            }
          }
        )

        cv.isViewedByHR = true
      } catch (err) {
        console.error('Lỗi mark viewed:', err)
      }
    },

    getFileTypeLabel(fileName) {
      if (!fileName) return 'FILE'
      const parts = fileName.split('.')
      const ext = parts.length > 1 ? parts.pop().toUpperCase() : 'FILE'
      return ext.length <= 4 ? ext : ext.slice(0, 4)
    },

    getInitials(name) {
      if (!name) return 'CV'
      return name
        .split(' ')
        .map(w => w[0])
        .join('')
        .toUpperCase()
        .slice(0, 2)
    },

    formatDate(date) {
      return new Date(date).toLocaleDateString('vi-VN')
    }
  },

  mounted() {
    this.fetchCVs()
  }
  
}
</script>