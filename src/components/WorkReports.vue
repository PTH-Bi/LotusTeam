<template>
  <div class="reports-page">
    <Sidebar activeItem="Báo cáo" />
    <main class="reports-main">
      <header class="reports-header glass">
        <h1 class="reports-title">Báo cáo công việc</h1>
      </header>

      <div class="reports-content">
        <BaseCard class="report-card">
          <div class="report-header">
            <h2 class="report-title">Báo cáo công việc nhân viên</h2>
            <div class="report-actions">
              <button class="btn-icon" title="Xuất báo cáo">Xuất</button>
              <button class="btn-icon" title="In báo cáo">In</button>
            </div>
          </div>

          <div class="file-submission">
            <button class="btn-submit" @click="triggerFileInput">Nộp</button>
            <input
              type="file"
              ref="fileInput"
              multiple
              @change="handleFileSelection"
              style="display: none"
            />
            <div
              class="dropzone"
              :class="{ 'drag-over': dragOver }"
              @dragover.prevent="handleDragOver"
              @dragleave.prevent="handleDragLeave"
              @drop.prevent="handleDrop"
            >
              Kéo thả file vào đây hoặc chọn file để nộp
            </div>
            <div class="submission-message" v-if="submissionMessage">{{ submissionMessage }}</div>
          </div>

          <div class="table-responsive">
            <table class="report-table">
              <thead>
                <tr>
                  <th>STT</th>
                  <th>Công việc</th>
                  <th>Mô tả</th>
                  <th>Thời gian thực hiện</th>
                  <th>Kết quả</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in reportData" :key="index">
                  <td>{{ index + 1 }}</td>
                  <td>{{ item.task }}</td>
                  <td class="description-cell">{{ item.description }}</td>
                  <td>{{ item.date }}</td>
                  <td>
                    <span :class="['status-badge', statusClass(item.status)]">
                      {{ item.status }}
                    </span>
                  </td>
                  <td>
                    <button class="btn-detail" @click="openDetail(item)">Chi tiết</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="report-footer">
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
import { ref, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import '@/CSS/WorkReports.css'

const STORAGE_KEY = 'work_reports_submissions'

// Dữ liệu mẫu mở rộng
const reportData = ref([
  {
    task: 'Thực hiện nhiệm vụ chuyên môn',
    description: '...',
    date: '28/2/2026',
    status: 'Hoàn thành',
    detailDescription: 'Hoàn thành báo cáo tài chính quý I, bao gồm phân tích số liệu và đề xuất điều chỉnh.',
    startTime: '08:00 28/02/2026',
    endTime: '17:00 28/02/2026',
    deadlineStatus: 'Đúng hạn',
    employee: 'Nguyễn Văn A',
    supporters: 'Trần Thị B',
    approved: true
  },
  {
    task: 'Hỗ trợ đồng nghiệp',
    description: '...',
    date: '2/3/2026',
    status: 'Hoàn thành',
    detailDescription: 'Hỗ trợ kiểm thử module đăng nhập, phát hiện và fix 2 lỗi.',
    startTime: '09:00 02/03/2026',
    endTime: '16:00 02/03/2026',
    deadlineStatus: 'Sớm',
    employee: 'Nguyễn Văn A',
    supporters: 'Lê Văn C',
    approved: true
  },
  {
    task: 'Tham gia họp dự án',
    description: '...',
    date: '3/3/2026',
    status: 'Hoàn thành',
    detailDescription: 'Họp sprint planning, thống nhất mục tiêu sprint 12.',
    startTime: '14:00 03/03/2026',
    endTime: '15:30 03/03/2026',
    deadlineStatus: 'Đúng hạn',
    employee: 'Nguyễn Văn A',
    supporters: 'Không',
    approved: true
  },
  {
    task: 'Báo cáo tiến độ',
    description: '...',
    date: '4/3/2026',
    status: 'Đang thực hiện',
    detailDescription: 'Đang tổng hợp số liệu từ các bộ phận để chuẩn bị báo cáo tuần.',
    startTime: '08:30 04/03/2026',
    endTime: 'Chưa kết thúc',
    deadlineStatus: 'Đang tiến hành',
    employee: 'Nguyễn Văn A',
    supporters: 'Phạm Thị D',
    approved: false
  },
  {
    task: 'Đào tạo nhân viên mới',
    description: '...',
    date: '5/3/2026',
    status: 'Chưa bắt đầu',
    detailDescription: 'Chuẩn bị tài liệu và kế hoạch đào tạo cho nhân viên mới.',
    startTime: 'Chưa bắt đầu',
    endTime: 'Chưa bắt đầu',
    deadlineStatus: 'Chưa bắt đầu',
    employee: 'Nguyễn Văn A',
    supporters: 'Trần Thị B, Lê Văn C',
    approved: false
  }
])

const submittedProjectFiles = ref([])
const showModal = ref(false)
const selectedItem = ref(null)
const fileInput = ref(null)
const dragOver = ref(false)
const submissionMessage = ref('')

const statusClass = (status) => {
  if (status === 'Hoàn thành') return 'status-success'
  if (status === 'Đang thực hiện') return 'status-warning'
  if (status === 'Đã nộp') return 'status-success'
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

const loadSubmissionsFromStorage = () => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (raw) {
    try {
      submittedProjectFiles.value = JSON.parse(raw) || []
    } catch (error) {
      submittedProjectFiles.value = []
      console.error('Failed to parse stored submissions:', error)
    }
  }
}

const saveSubmissionsToStorage = () => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(submittedProjectFiles.value || []))
}

const addSubmissionReport = (entry) => {
  reportData.value.unshift({
    task: `Nộp dự án: ${entry.projectName}`,
    description: `File: ${entry.fileName}`,
    date: entry.submittedAt,
    status: 'Đã nộp',
    detailDescription: `File nộp: ${entry.fileName}, loại: ${entry.fileType}, kích thước: ${entry.fileSize} bytes`,
    startTime: entry.submittedAt,
    endTime: entry.submittedAt,
    deadlineStatus: 'Đã nộp',
    employee: 'Nhân viên',
    supporters: '---',
    approved: false,
    submittedFileName: entry.fileName,
    submittedFileType: entry.fileType,
    fileDataUrl: entry.fileDataUrl,
    id: entry.id,
    status: entry.status || 'Đang thực hiện'
  })
}

const initializeReportData = () => {
  loadSubmissionsFromStorage()
  if (submittedProjectFiles.value.length > 0) {
    submittedProjectFiles.value.forEach((entry) => addSubmissionReport(entry))
  }
}

const triggerFileInput = () => {
  fileInput.value?.click()
}

const fileToDataURL = (file) => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result)
    reader.onerror = reject
    reader.readAsDataURL(file)
  })
}

const submitFiles = async (files) => {
  if (!files || files.length === 0) {
    submissionMessage.value = 'Vui lòng chọn file để nộp.'
    return
  }

  for (const file of files) {
    const now = new Date()
    const fileDataUrl = await fileToDataURL(file)

    const submission = {
      id: `${file.name}-${now.getTime()}`,
      projectName: file.name,
      fileName: file.name,
      fileSize: file.size,
      fileType: file.type || 'unknown',
      submittedAt: now.toLocaleString('vi-VN'),
      fileDataUrl,
      status: 'Đang thực hiện'
    }

    submittedProjectFiles.value.push(submission)
    saveSubmissionsToStorage()
    addSubmissionReport(submission)
  }

  submissionMessage.value = `Đã nộp ${files.length} file thành công.`
  dragOver.value = false
}

const handleFileSelection = (event) => {
  const files = Array.from(event.target.files || [])
  submitFiles(files)
  event.target.value = null
}

const handleDragOver = () => {
  dragOver.value = true
}

const handleDragLeave = () => {
  dragOver.value = false
}

const handleDrop = (event) => {
  dragOver.value = false
  const files = Array.from(event.dataTransfer.files || [])
  submitFiles(files)
}

const openDetail = (item) => {
  selectedItem.value = item
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  selectedItem.value = null
}

onMounted(() => {
  initializeReportData()
})
</script>