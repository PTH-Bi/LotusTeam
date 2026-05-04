<template>
  <div class="recruitment-page">
    <Sidebar activeItem="Tuyển dụng" />

    <main class="recruitment-main">
      <!-- Header với tổng số ứng viên -->
      <header class="recruitment-header">
        <div class="header-left">
          <h1 class="recruitment-title">Quản lý ứng viên</h1>
          <p class="page-subtitle">Quản lý và theo dõi tiến độ của tất cả các ứng viên tiềm năng.</p>
        </div>
        
        <div class="header-right">
          <BaseButton variant="primary" @click="openCreateModal">
            Thêm ứng viên
          </BaseButton>
        </div>
      </header>

      <!-- Tabs và Filters -->
      <div class="recruitment-content">
        <!-- Tabs Navigation -->
        <div class="tabs-container">

          
          <!-- Department và Status Filters -->
          <div class="filters-wrapper">
            <select v-model="filters.department" class="filter-select department-select">
              <option value="">Tất cả phòng ban</option>
              <option v-for="dept in departments" :key="dept.id" :value="dept.name">{{ dept.name }}</option>
            </select>
            
            <select v-model="filters.status" class="filter-select status-select">
              <option value="">Tất cả trạng thái</option>
              <option value="active">Đang tuyển</option>
              <option value="interview">Phỏng vấn</option>
              <option value="hired">Đã tuyển</option>
              <option value="rejected">Từ chối</option>
              <option value="closed">Đã đóng</option>
            </select>
          </div>
        </div>

        <!-- Stats Cards -->
        <div class="stats-container">
          <div class="total-candidates-card">
            <span class="total-label">Tổng số ứng viên</span>
            <span class="total-value">{{ totalCandidates }}</span>
            <span class="total-growth">3.17% từ với tổng trả lời</span>
          </div>
          
          <div v-for="stat in statsList" :key="stat.label" class="stat-card">
            <span class="stat-label">{{ stat.label }}</span>
            <span class="stat-value">{{ stat.value }}</span>
          </div>
        </div>

        <!-- Main Content Area với List và Details -->
        <div class="main-content-area">
          <!-- Left Column - Candidate List -->
          <div class="candidates-list-section">
            <div class="search-wrapper">
              <input 
                v-model="searchQuery" 
                type="text" 
                placeholder="Tìm kiếm ứng viên..." 
                class="search-input"
              />
            </div>

            <div class="candidates-list">
              <div 
                v-for="candidate in filteredItems" 
                :key="candidate.id" 
                class="candidate-item"
                :class="{ 'selected': selectedCandidate?.id === candidate.id }"
                @click="selectCandidate(candidate)"
              >
                <div class="candidate-avatar">
                  {{ getInitials(candidate.name) }}
                </div>
                <div class="candidate-info">
                  <h3 class="candidate-name">{{ candidate.name }}</h3>
                  <p class="candidate-title">{{ candidate.position }}</p>
                  <a :href="'mailto:' + candidate.email" class="candidate-email">{{ candidate.email || 'Không có email' }}</a>
                </div>
                <div class="candidate-status">
                  <span :class="['status-badge', candidate.status]">{{ getStatusText(candidate.status) }}</span>
                </div>
              </div>
            </div>

            <div class="pagination-info">
              <span>1 - {{ filteredItems.length }} trong {{ totalCandidates }} ứng viên</span>
            </div>
          </div>

          <!-- Right Column - Candidate Details -->
          <div class="candidate-details-section" v-if="selectedCandidate">
            <div class="details-header">
              <h2>Thông tin ứng viên</h2>
            </div>
            
            <div class="details-content">
              <div class="detail-row">
                <span class="detail-label">Ngày cập nhật:</span>
                <span class="detail-value">{{ formatDate(selectedCandidate.appliedDate) }}</span>
              </div>
              
              <div class="detail-row">
                <span class="detail-label">Đơn hàng:</span>
                <span class="detail-value">{{ formatDate(selectedCandidate.appliedDate) }}</span>
              </div>
              
              <div class="detail-row">
                <span class="detail-label">Đối tượng:</span>
                <span class="detail-value">{{ selectedCandidate.position }}</span>
              </div>
              
              <div class="detail-row">
                <span class="detail-label">Địa chỉ:</span>
                <span class="detail-value">Trụ chính</span>
              </div>
              
              <div class="detail-row">
                <span class="detail-label">Thời gian:</span>
                <span class="detail-value">{{ formatDate(selectedCandidate.appliedDate) }}</span>
              </div>
              
              <div class="detail-row">
                <span class="detail-label">Chỉ định:</span>
                <span class="detail-value">Chủ nhân</span>
              </div>
            </div>

            <div class="details-actions">
              <BaseButton variant="ghost" size="small" @click="editItem(selectedCandidate)">
                Sửa
              </BaseButton>
              <BaseButton variant="ghost" size="small" @click="advanceCandidate(selectedCandidate)">
                Chuyển vòng
              </BaseButton>
              <BaseButton variant="ghost" size="small" @click="reviewCandidate(selectedCandidate)">
                Đánh giá
              </BaseButton>
              <BaseButton variant="ghost" size="small" @click="hireCandidate(selectedCandidate)">
                Tuyển
              </BaseButton>
            </div>
          </div>

          <!-- Empty State khi chưa chọn candidate -->
          <div class="candidate-details-section empty-state" v-else>
            <p>Chọn một ứng viên để xem chi tiết</p>
          </div>
        </div>
      </div>
    </main>

    <!-- Modal Support -->
    <RecruitmentModal
      v-model="showModal"
      :item="editingItem"
      :positions="positions"
      :departments="departments"
      :templates="workflowTemplates"
      @save="handleSave"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import RecruitmentModal from './RecruitmentModal.vue'
import BaseButton from './base/BaseButton.vue'
import api from '@/services/api'
import axiosClient from '@/services/axiosClient'
import '@/CSS/Recruitment.css'

const recruitmentItems = ref([])
const loading = ref(false)
const error = ref(null)
const selectedCandidate = ref(null)

const defaultPositions = [
  { id: 1, name: 'Backend Developer' },
  { id: 2, name: 'Frontend Developer' },
  { id: 3, name: 'Fullstack Developer' },
  { id: 4, name: 'Giám đốc' },
  { id: 5, name: 'HR Specialist' },
  { id: 6, name: 'Kế toán viên' },
  { id: 7, name: 'Marketing Executive' },
  { id: 8, name: 'Nhân viên' },
  { id: 9, name: 'Trưởng phòng' }
]

const defaultDepartments = [
  { id: 1, name: 'Ban Giám Đốc' },
  { id: 2, name: 'Phòng Kế Toán' },
  { id: 3, name: 'Phòng Kỹ Thuật' },
  { id: 4, name: 'Phòng Marketing' },
  { id: 5, name: 'Phòng Nhân Sự' }
]

const defaultTemplates = [
  { id: 1, name: 'Tuyển dụng nhanh' },
  { id: 2, name: 'Sàng lọc CV' },
  { id: 3, name: 'Phỏng vấn 2 vòng' }
]

const positions = ref([])
const departments = ref([])
const workflowTemplates = ref([])

const searchQuery = ref('')
const filters = ref({ department: '', status: '' })
const showModal = ref(false)
const editingItem = ref(null)

const filteredItems = computed(() => {
  let list = recruitmentItems.value
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(i => 
      (i.name || '').toLowerCase().includes(q) || 
      (i.email || '').toLowerCase().includes(q) || 
      (i.position || '').toLowerCase().includes(q)
    )
  }
  if (filters.value.department) {
    list = list.filter(i => i.department === filters.value.department)
  }
  if (filters.value.status) {
    list = list.filter(i => i.status === filters.value.status)
  }
  return list
})

const statsList = computed(() => {
  const items = recruitmentItems.value || []
  const activeCount = items.filter(i => i.status === 'active' || i.status === 'applied').length
  const interviewCount = items.filter(i => i.status === 'interview').length
  const hiredCount = items.filter(i => i.status === 'hired').length
  const rejectedCount = items.filter(i => i.status === 'rejected').length

  return [
    { label: 'Đang tuyển', value: activeCount },
    { label: 'Phỏng vấn', value: interviewCount },
    { label: 'Đã tuyển', value: hiredCount },
    { label: 'Từ chối', value: rejectedCount }
  ]
})

const totalCandidates = computed(() => recruitmentItems.value.length)

function selectCandidate(candidate) {
  selectedCandidate.value = candidate
}

function getInitials(name) {
  if (!name) return 'NA'
  return name.split(' ').map(n => n[0]).slice(-2).join('').toUpperCase()
}

function formatDate(dateString) {
  if (!dateString) return 'N/A'
  const date = new Date(dateString)
  return date.toLocaleDateString('vi-VN')
}

function openCreateModal() {
  editingItem.value = null
  showModal.value = true
}

function editItem(item) {
  editingItem.value = { ...item }
  showModal.value = true
}

async function handleSave(data) {
  loading.value = true
  error.value = null
  try {
    const payload = {
      name: data.name,
      email: data.email,
      phone: data.phone,
      departmentId: data.department || null,
      resumeUrl: data.resume,
      workflowTemplateId: data.workflowTemplateId || null,
      note: data.note || ''
    }
    if (data.position) payload.positionId = data.position

    const res = await api.recruitment.createCandidate(payload)
    const created = (res && res.data) ? res.data : (res && res) ? res : null
    if (!created) throw new Error('Server trả về dữ liệu ứng viên không hợp lệ')
    const positionName = created.positionTitle ?? created.position ?? (positions.value.find(p => p.id == data.position)?.name) ?? ''
    
    const newCandidate = {
      id: created.id ?? created.candidateId ?? created.applicantId,
      candidateCode: created.code ?? created.candidateCode ?? `C-${Date.now()}`,
      name: created.name ?? created.fullName ?? data.name,
      position: positionName,
      department: created.departmentName ?? created.department ?? data.department ?? '',
      appliedDate: created.appliedAt ?? created.createdAt ?? new Date().toISOString(),
      status: created.status ?? 'applied',
      email: created.email ?? data.email,
      phone: created.phone ?? data.phone,
      resume: created.resumeUrl ?? created.cvUrl ?? data.resume
    }
    
    recruitmentItems.value.unshift(newCandidate)
    selectedCandidate.value = newCandidate
  } catch (err) {
    console.error('Save candidate failed:', err)
    error.value = err.message || String(err)
    alert('Lưu ứng viên thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
    showModal.value = false
    editingItem.value = null
  }
}

async function advanceCandidate(item) {
  loading.value = true
  try {
    await api.recruitment.advanceCandidate(item.id, { note: 'Advanced from UI' })
    await loadRecruitments()
    selectedCandidate.value = recruitmentItems.value.find(c => c.id === item.id) || null
  } catch (err) {
    console.error('Advance failed', err)
    alert('Chuyển bước thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
  }
}

async function reviewCandidate(item) {
  const note = prompt('Ghi chú đánh giá (tùy chọn):', '')
  if (note === null) return
  loading.value = true
  try {
    await api.recruitment.review({ candidateId: item.id, note })
    await loadRecruitments()
    selectedCandidate.value = recruitmentItems.value.find(c => c.id === item.id) || null
  } catch (err) {
    console.error('Review failed', err)
    alert('Đánh giá thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
  }
}

async function hireCandidate(item) {
  if (!confirm(`Xác nhận tuyển ${item.name || item.candidateCode}?`)) return
  loading.value = true
  try {
    await api.recruitment.hire(item.id)
    await loadRecruitments()
    selectedCandidate.value = recruitmentItems.value.find(c => c.id === item.id) || null
  } catch (err) {
    console.error('Hire failed', err)
    alert('Tuyển dụng thất bại: ' + (err.message || err))
  } finally {
    loading.value = false
  }
}

function getStatusText(status) {
  const map = { 
    active: 'Đang tuyển', 
    applied: 'Đã ứng tuyển',
    interview: 'Phỏng vấn', 
    hired: 'Đã tuyển', 
    rejected: 'Từ chối',
    paused: 'Tạm dừng', 
    closed: 'Đã đóng', 
    draft: 'Nháp' 
  }
  return map[status] || status || 'Nháp'
}

function normalizeResponse(resp) {
  if (!resp) return []
  if (Array.isArray(resp)) return resp
  if (resp.data && Array.isArray(resp.data)) return resp.data
  if (resp.items && Array.isArray(resp.items)) return resp.items
  if (resp.success && Array.isArray(resp.data)) return resp.data
  return resp?.data?.data ?? []
}

async function loadRecruitments() {
  loading.value = true
  error.value = null
  try {
    const resp = await api.recruitment.candidates()
    const list = normalizeResponse(resp)
    recruitmentItems.value = list.map(item => ({
      id: item.id ?? item.candidateId ?? item.applicantId,
      candidateCode: item.code ?? item.candidateCode ?? `C-${item.id}`,
      name: item.name ?? item.fullName ?? item.candidateName ?? '',
      position: item.positionTitle ?? item.position ?? item.appliedPosition ?? '',
      appliedDate: item.appliedAt ?? item.createdAt ?? item.submittedAt ?? '',
      status: item.status ?? item.stage ?? 'applied',
      email: item.email ?? item.contactEmail ?? '',
      phone: item.phone ?? item.contactPhone ?? '',
      resume: item.resumeUrl ?? item.cvUrl ?? item.resume,
      department: item.departmentName ?? item.department ?? item.department_id ?? ''
    }))

    if (recruitmentItems.value.length > 0 && !selectedCandidate.value) {
      selectedCandidate.value = recruitmentItems.value[0]
    }

    try {
      const tpl = await api.recruitment.workflowTemplates()
      const result = Array.isArray(tpl) ? tpl : (tpl?.data ?? [])
      workflowTemplates.value = result.length ? result : defaultTemplates
    } catch (e) {
      workflowTemplates.value = defaultTemplates
    }

    try {
      const presp = await api.positions.list()
      const plist = normalizeResponse(presp)
      positions.value = plist.length ? plist.map(p => ({ id: p.id ?? p.positionId ?? p.id, name: p.name ?? p.title ?? p.positionName })) : defaultPositions
    } catch (e) {
      positions.value = defaultPositions
    }

    try {
      const dresp = await api.departments?.list?.() ?? []
      const dlist = normalizeResponse(dresp)
      departments.value = dlist.length ? dlist.map(d => ({ id: d.id ?? d.departmentId ?? d.id, name: d.name ?? d.title ?? d.departmentName })) : defaultDepartments
    } catch (e) {
      departments.value = defaultDepartments
    }
  } catch (err) {
    error.value = err.message || String(err)
    recruitmentItems.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => loadRecruitments())
</script>