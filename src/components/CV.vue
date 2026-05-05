<template>
  <div class="app-container">
    <sidebar active-item="CV" />

    <div class="cv-page">
      <main class="cv-main">
        <header class="cv-header">
          <div class="header-title">
            <h1 class="cv-title">Danh sách CV ứng viên</h1>
            <p class="cv-subtitle">Quản lý và đánh giá hồ sơ ứng viên</p>
          </div>

          <div class="header-actions">
            <button class="refresh-btn" @click="fetchCVs" :disabled="loading">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <path d="M12 8v8M8 12h8" />
              </svg>
              Làm mới
            </button>
          </div>
        </header>

        <!-- Filter Panel -->
        <div class="filter-panel">
          <div class="filter-group">
            <label class="filter-label">Trạng thái xem:</label>
            <div class="filter-buttons">
              <button 
                class="filter-btn" 
                :class="{ active: showViewed }"
                @click="showViewed = !showViewed"
              >
                ✅ Đã xem
              </button>
              <button 
                class="filter-btn" 
                :class="{ active: showNotViewed }"
                @click="showNotViewed = !showNotViewed"
              >
                📄 Chưa xem
              </button>
            </div>
          </div>

          <div class="filter-group">
            <label class="filter-label">Lọc theo vị trí:</label>
            <select v-model="filterPosition" class="filter-select">
              <option value="">Tất cả vị trí</option>
              <option v-for="pos in uniquePositions" :key="pos" :value="pos">{{ pos }}</option>
            </select>
          </div>

          <div class="filter-group">
            <label class="filter-label">Điểm tối thiểu:</label>
            <input type="range" v-model.number="minScore" min="0" max="100" step="10" class="score-slider" />
            <span class="score-value">{{ minScore }}+</span>
          </div>
        </div>

        <!-- LOADING -->
        <div v-if="loading" class="loading-state">
          <div class="loading-spinner"></div>
          <p>Đang tải danh sách CV...</p>
        </div>

        <!-- DANH SÁCH CV -->
        <section v-else class="cv-grid">
          <article
            v-for="cv in filteredCVs"
            :key="cv.candidateCVID"
            class="cv-card"
            :class="{ 
              viewed: cv.isViewedByHR,
              suitable: cv.isSuitable 
            }"
          >
            <div class="cv-card-header">
              <div class="cv-file-icon">
                <span class="file-ext">{{ getFileTypeLabel(cv.fileName) }}</span>
              </div>
              <div class="cv-file-info">
                <p class="file-name" :title="cv.fileName">{{ truncateFileName(cv.fileName) }}</p>
                <p class="candidate-name">
                  {{ getCandidateName(cv) }}
                </p>
                <p class="candidate-email">{{ cv.candidateEmail || getEmailFromFileName(cv.fileName) || 'Chưa có email' }}</p>
              </div>
            </div>

            <div class="cv-card-body">
              <div class="cv-score-section">
                <div class="score-circle" :class="getScoreClass(cv.score)">
                  <span class="score-number">{{ cv.score || 0 }}</span>
                  <span class="score-max">/100</span>
                </div>
                <div class="score-label">Điểm đánh giá</div>
              </div>

              <div class="cv-position-section">
                <div class="position-badge" :class="getPositionClass(cv.isSuitable)">
                  {{ cv.bestMatchedPosition || 'Chưa phân loại' }}
                </div>
                <div class="suitability" :class="cv.isSuitable ? 'suitable' : 'not-suitable'">
                  {{ cv.isSuitable ? '✅ Phù hợp' : '⚠️ Cần xem xét' }}
                </div>
              </div>

              <div class="cv-skills-section" v-if="cv.matchedSkills">
                <p class="skills-label">Kỹ năng phù hợp:</p>
                <div class="skills-tags">
                  <span v-for="skill in parseSkills(cv.matchedSkills)" :key="skill" class="skill-tag">
                    {{ skill }}
                  </span>
                </div>
              </div>
            </div>

            <div class="cv-card-footer">
              <div class="cv-meta">
                <span class="meta-date">📅 {{ formatDate(cv.createdAt) }}</span>
                <span class="status-badge" :class="cv.isViewedByHR ? 'viewed' : 'pending'">
                  {{ cv.isViewedByHR ? 'Đã xem' : 'Chưa xem' }}
                </span>
              </div>
              <div class="cv-actions">
                <button class="btn-view" @click="viewCV(cv)">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                    <circle cx="12" cy="12" r="3" />
                  </svg>
                  Xem CV
                </button>
                <button v-if="!cv.isViewedByHR" class="btn-mark" @click="markAsViewed(cv)">
                  Đánh dấu đã xem
                </button>
              </div>
            </div>
          </article>

          <div v-if="filteredCVs.length === 0" class="empty-state">
            <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
              <polyline points="10 9 9 9 8 9" />
            </svg>
            <p>Không có CV nào phù hợp với bộ lọc</p>
          </div>
        </section>

        <!-- Thống kê nhanh -->
        <div v-if="cvs.length > 0" class="stats-footer">
          <div class="stat-item">
            <span class="stat-value">{{ cvs.length }}</span>
            <span class="stat-label">Tổng CV</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{{ unviewedCount }}</span>
            <span class="stat-label">Chưa xem</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{{ suitableCount }}</span>
            <span class="stat-label">Phù hợp</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{{ avgScore }}</span>
            <span class="stat-label">Điểm TB</span>
          </div>
        </div>
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
      showNotViewed: true,
      filterPosition: '',
      minScore: 0
    }
  },

  computed: {
    filteredCVs() {
      return this.cvs.filter(cv => {
        if (this.showViewed && cv.isViewedByHR) return true
        if (this.showNotViewed && !cv.isViewedByHR) return true
        return false
      }).filter(cv => {
        if (!this.filterPosition) return true
        return cv.bestMatchedPosition === this.filterPosition
      }).filter(cv => {
        return (cv.score || 0) >= this.minScore
      }).sort((a, b) => {
        return (b.score || 0) - (a.score || 0)
      })
    },

    uniquePositions() {
      const positions = new Set()
      this.cvs.forEach(cv => {
        if (cv.bestMatchedPosition && cv.bestMatchedPosition !== 'Chưa phân loại') {
          positions.add(cv.bestMatchedPosition)
        }
      })
      return Array.from(positions).sort()
    },

    unviewedCount() {
      return this.cvs.filter(cv => !cv.isViewedByHR).length
    },

    suitableCount() {
      return this.cvs.filter(cv => cv.isSuitable).length
    },

    avgScore() {
      if (this.cvs.length === 0) return 0
      const total = this.cvs.reduce((sum, cv) => sum + (cv.score || 0), 0)
      return Math.round(total / this.cvs.length)
    }
  },

  methods: {
    async fetchCVs() {
      try {
        this.loading = true

        const token = localStorage.getItem('token')
        console.log('Token:', token)

        const res = await fetch('http://localhost:5000/api/recruitment/cvs', {
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        })

        console.log('Response status:', res.status)

        if (!res.ok) {
          if (res.status === 403) {
            console.error('Không có quyền truy cập')
            alert('Bạn không có quyền xem danh sách CV. Vui lòng liên hệ HR.')
            return
          }
          throw new Error(`HTTP error! status: ${res.status}`)
        }

        const data = await res.json()
        if (data.success && data.data) {
          this.cvs = data.data
        } else if (Array.isArray(data)) {
          this.cvs = data
        } else {
          this.cvs = data.data || []
        }
        
        console.log('Loaded CVs:', this.cvs.length)
      } catch (err) {
        console.error('Lỗi load CV:', err)
        alert('Có lỗi xảy ra khi tải danh sách CV')
      } finally {
        this.loading = false
      }
    },

    // Cải thiện hàm lấy tên ứng viên
    getCandidateName(cv) {
      // Ưu tiên lấy từ API trước
      if (cv.candidateName && cv.candidateName !== 'Unknown' && cv.candidateName !== 'topcv.vn') {
        return cv.candidateName
      }
      
      // Nếu có email, lấy tên từ email
      const email = cv.candidateEmail || this.getEmailFromFileName(cv.fileName)
      if (email && email.includes('@')) {
        const nameFromEmail = email.split('@')[0]
          .replace(/\./g, ' ')
          .replace(/_/g, ' ')
          .split(' ')
          .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
          .join(' ')
        if (nameFromEmail && nameFromEmail.length > 2) {
          return nameFromEmail
        }
      }
      
      // Lấy tên từ tên file
      const nameFromFile = this.extractNameFromFileName(cv.fileName)
      if (nameFromFile && nameFromFile !== 'Unknown' && nameFromFile !== 'topcv.vn') {
        return nameFromFile
      }
      
      return 'Ứng viên'
    },

    // Lấy email từ tên file nếu có
    getEmailFromFileName(fileName) {
      if (!fileName) return null
      // Tìm email pattern trong tên file
      const emailMatch = fileName.match(/[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/)
      if (emailMatch) return emailMatch[0]
      return null
    },

    // Cải thiện hàm trích xuất tên từ file name
    extractNameFromFileName(fileName) {
      if (!fileName) return 'Ứng viên'
      
      // Loại bỏ phần GUID ở đầu (dạng: 2734fdd2-a76f-4b71-ae79-39f520c7394a_)
      let name = fileName.replace(/^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}_/i, '')
      
      // Loại bỏ phần TopCV.vn-xxxxx
      name = name.replace(/TopCV\.vn-\d+\.\d+\.\d+/i, '')
      name = name.replace(/topcv\.vn/i, '')
      
      // Loại bỏ extension
      name = name.replace(/\.(pdf|doc|docx)$/i, '')
      
      // Thay thế underscore và dash bằng space
      name = name.replace(/[_-]/g, ' ')
      
      // Loại bỏ các từ không cần thiết
      name = name.replace(/CV|cv|resume|hồ sơ/gi, '')
      
      // Chuẩn hóa: viết hoa chữ cái đầu mỗi từ
      name = name.split(' ')
        .filter(word => word.length > 0 && !word.match(/^\d+$/))
        .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
        .join(' ')
      
      // Nếu tên quá ngắn hoặc không hợp lệ
      if (name.length < 2 || name === 'Pdf' || name === 'Doc') {
        return 'Ứng viên'
      }
      
      return name.trim() || 'Ứng viên'
    },

    truncateFileName(fileName) {
      if (!fileName) return ''
      if (fileName.length <= 40) return fileName
      return fileName.substring(0, 37) + '...'
    },

    async viewCV(cv) {
      if (cv.fileUrl) {
        window.open(cv.fileUrl, '_blank')
      } else if (cv.filePath) {
        window.open(`http://localhost:5000/${cv.filePath}`, '_blank')
      }

      if (!cv.isViewedByHR) {
        await this.markAsViewed(cv)
      }
    },

    async markAsViewed(cv) {
      try {
        const token = localStorage.getItem('token')
        const res = await fetch(
          `http://localhost:5000/api/recruitment/cvs/${cv.candidateCVID}/view`,
          {
            method: 'POST',
            headers: {
              Authorization: `Bearer ${token}`,
              'Content-Type': 'application/json'
            }
          }
        )

        if (res.ok) {
          cv.isViewedByHR = true
          console.log('Đã đánh dấu đã xem:', cv.fileName)
        }
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

    formatDate(date) {
      if (!date) return 'Chưa rõ'
      const d = new Date(date)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    },

    parseSkills(skillsString) {
      if (!skillsString) return []
      if (typeof skillsString === 'string') {
        return skillsString.split(',').map(s => s.trim()).filter(s => s)
      }
      if (Array.isArray(skillsString)) {
        return skillsString
      }
      return []
    },

    getScoreClass(score) {
      if (score >= 70) return 'high'
      if (score >= 50) return 'medium'
      return 'low'
    },

    getPositionClass(isSuitable) {
      return isSuitable ? 'suitable-pos' : 'pending-pos'
    }
  },

  mounted() {
    this.fetchCVs()
  }
}
</script>

<style scoped>
/* CSS bổ sung cho CV page */
.cv-subtitle {
  font-size: 14px;
  color: #6b7280;
  margin-top: 4px;
}

.candidate-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 4px;
  background: linear-gradient(135deg, #1f2937 0%, #4f46e5 100%);
  background-clip: text;
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.candidate-email {
  font-size: 11px;
  color: #6b7280;
  word-break: break-all;
}

.file-name {
  font-size: 12px;
  font-weight: 500;
  color: #4b5563;
  margin-bottom: 6px;
  word-break: break-all;
  line-height: 1.3;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.refresh-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
}

.refresh-btn:hover:not(:disabled) {
  background: #e5e7eb;
}

.refresh-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.filter-panel {
  display: flex;
  flex-wrap: wrap;
  gap: 24px;
  padding: 16px 20px;
  background: white;
  border-radius: 12px;
  margin-bottom: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.filter-label {
  font-size: 13px;
  font-weight: 500;
  color: #6b7280;
}

.filter-buttons {
  display: flex;
  gap: 8px;
}

.filter-btn {
  padding: 6px 12px;
  background: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-btn.active {
  background: #4f46e5;
  color: white;
  border-color: #4f46e5;
}

.filter-select {
  padding: 6px 12px;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  font-size: 13px;
  background: white;
  min-width: 150px;
}

.score-slider {
  width: 150px;
  height: 4px;
  border-radius: 2px;
  background: #e5e7eb;
}

.score-value {
  font-size: 13px;
  font-weight: 600;
  color: #4f46e5;
  min-width: 40px;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 300px;
  gap: 16px;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #f3f4f6;
  border-top-color: #4f46e5;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.cv-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
  gap: 20px;
}

.cv-card {
  background: white;
  border-radius: 16px;
  border: 1px solid #e5e7eb;
  overflow: hidden;
  transition: all 0.2s;
}

.cv-card:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  transform: translateY(-2px);
}

.cv-card.viewed {
  opacity: 0.85;
  background: #f9fafb;
}

.cv-card.suitable {
  border-left: 4px solid #22c55e;
}

.cv-card-header {
  display: flex;
  gap: 16px;
  padding: 16px;
  border-bottom: 1px solid #f3f4f6;
}

.cv-file-icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  color: white;
  font-weight: 700;
  font-size: 12px;
}

.cv-file-info {
  flex: 1;
}

.file-name {
  font-size: 14px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 4px;
  word-break: break-all;
}

.candidate-name {
  font-size: 13px;
  font-weight: 500;
  color: #4f46e5;
  margin-bottom: 2px;
}

.candidate-email {
  font-size: 11px;
  color: #9ca3af;
}

.cv-card-body {
  padding: 16px;
  display: flex;
  gap: 16px;
}

.cv-score-section {
  text-align: center;
  min-width: 80px;
}

.score-circle {
  width: 60px;
  height: 60px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: #f3f4f6;
}

.score-circle.high {
  background: #dcfce7;
  border: 2px solid #22c55e;
}

.score-circle.medium {
  background: #fed7aa;
  border: 2px solid #f97316;
}

.score-circle.low {
  background: #fee2e2;
  border: 2px solid #ef4444;
}

.score-number {
  font-size: 20px;
  font-weight: 800;
  color: #1a1a1a;
}

.score-max {
  font-size: 10px;
  color: #6b7280;
}

.score-label {
  font-size: 10px;
  color: #9ca3af;
  margin-top: 4px;
}

.cv-position-section {
  flex: 1;
}

.position-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
  margin-bottom: 8px;
}

.position-badge.suitable-pos {
  background: #dcfce7;
  color: #166534;
}

.position-badge.pending-pos {
  background: #fef3c7;
  color: #92400e;
}

.suitability {
  font-size: 11px;
}

.suitability.suitable {
  color: #22c55e;
}

.suitability.not-suitable {
  color: #f97316;
}

.cv-skills-section {
  grid-column: span 2;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid #f3f4f6;
}

.skills-label {
  font-size: 11px;
  color: #6b7280;
  margin-bottom: 6px;
}

.skills-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.skill-tag {
  padding: 2px 8px;
  background: #e0e7ff;
  color: #4338ca;
  border-radius: 12px;
  font-size: 10px;
  font-weight: 500;
}

.cv-card-footer {
  padding: 12px 16px;
  background: #f9fafb;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.cv-meta {
  display: flex;
  gap: 12px;
  align-items: center;
}

.meta-date {
  font-size: 11px;
  color: #9ca3af;
}

.status-badge {
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 10px;
  font-weight: 600;
}

.status-badge.viewed {
  background: #dcfce7;
  color: #166534;
}

.status-badge.pending {
  background: #fef3c7;
  color: #92400e;
}

.cv-actions {
  display: flex;
  gap: 8px;
}

.btn-view, .btn-mark {
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 4px;
}

.btn-view {
  background: #4f46e5;
  border: none;
  color: white;
}

.btn-view:hover {
  background: #4338ca;
}

.btn-mark {
  background: transparent;
  border: 1px solid #e5e7eb;
  color: #6b7280;
}

.btn-mark:hover {
  background: #f3f4f6;
}

.empty-state {
  grid-column: 1 / -1;
  text-align: center;
  padding: 60px 20px;
  background: white;
  border-radius: 16px;
  color: #9ca3af;
}

.empty-state svg {
  margin-bottom: 16px;
  color: #d1d5db;
}

.stats-footer {
  display: flex;
  gap: 32px;
  justify-content: center;
  margin-top: 32px;
  padding: 16px;
  background: white;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.stat-item {
  text-align: center;
}

.stat-value {
  display: block;
  font-size: 24px;
  font-weight: 700;
  color: #1a1a1a;
}

.stat-label {
  font-size: 12px;
  color: #6b7280;
}

/* Responsive */
@media (max-width: 768px) {
  .cv-grid {
    grid-template-columns: 1fr;
  }
  
  .filter-panel {
    flex-direction: column;
    align-items: stretch;
  }
  
  .filter-group {
    justify-content: space-between;
  }
  
  .cv-card-body {
    flex-direction: column;
  }
  
  .stats-footer {
    gap: 16px;
    flex-wrap: wrap;
  }
}
</style>