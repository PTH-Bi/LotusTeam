<template>
  <div class="employee-leave-page">
    <Sidebar activeItem="Nghỉ phép" />
    <main class="employee-leave-main">
      <div class="leave-card">
        <div class="leave-card-header">
          <h2>Viết đơn xin nghỉ</h2>
          <p>Vui lòng cung cấp đầy đủ thông tin để gửi yêu cầu nghỉ phép.</p>
        </div>

        <div class="leave-card-body">
          <div class="form-row">
            <div class="field-group">
              <label>Loại nghỉ phép</label>
              <select v-model="leaveType">
                <option value="">Chọn loại nghỉ phép</option>
                <option value="Nghỉ ốm">Nghỉ ốm</option>
                <option value="Nghỉ phép có lương">Nghỉ phép có lương</option>
                <option value="Nghỉ phép không lương">Nghỉ phép không lương</option>
              </select>
            </div>

            <div class="field-group">
              <label>Người duyệt / Quản lý</label>
              <input type="text" v-model="approver" :placeholder="approvalLevel || 'Tự gán theo số ngày nghỉ'" />
              <div class="approval-hint">Cấp duyệt dự kiến: <strong>{{ approvalLevel || 'Chưa rõ' }}</strong></div>
            </div>
          </div>

          <div class="form-row">
            <div class="field-group">
              <label>Ngày bắt đầu</label>
              <input type="date" v-model="fromDate" />
            </div>

            <div class="field-group">
              <label>Ngày kết thúc</label>
              <input type="date" v-model="toDate" />
            </div>

            <div class="field-group">
              <label>Số ngày nghỉ</label>
              <input type="number" :value="daysCount" disabled />
            </div>
          </div>

          <div class="checkbox-row">
            <label>
              <input type="checkbox" v-model="isUnpaid" /> Nghỉ không lương (IsUnpaid)
            </label>
          </div>

          <div class="field-group">
            <label>Lý do nghỉ phép</label>
            <textarea v-model="reason" rows="5" placeholder="Nhập lý do chi tiết tại đây..."></textarea>
          </div>

          <div class="bottom-note">Hệ thống sẽ tự động cập nhật số ngày phép còn lại sau khi đơn được duyệt.</div>

          <div class="actions">
            <button class="btn-cancel" type="button" @click="resetForm">Hủy</button>
            <button class="btn-submit" @click="submitRequest">Gửi yêu cầu</button>
          </div>

          <p class="message" v-if="message">{{ message }}</p>
        </div>
      </div>

      <div class="own-requests-card" v-if="userRequests.length > 0">
        <h3>Danh sách yêu cầu của bạn</h3>
        <table class="requests-table">
          <thead>
            <tr>
              <th>Mã</th>
              <th>Loại</th>
              <th>Ngày</th>
              <th>Số ngày</th>
              <th>Người duyệt</th>
              <th>Loại</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in userRequests" :key="item.id">
              <td>{{ item.id }}</td>
              <td>{{ item.leaveType }}</td>
              <td>{{ item.fromDate }} - {{ item.toDate }}</td>
              <td>{{ item.daysCount }}</td>
              <td>{{ item.approver }}</td>
              <td>{{ item.isUnpaid ? 'Không lương' : 'Có lương' }}</td>
              <td>{{ statusText(item.status) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import '@/CSS/EmployeeLeave.css'

const leaveType = ref('')
const fromDate = ref('')
const toDate = ref('')
const approver = ref('')
const reason = ref('')
const isUnpaid = ref(false)
const message = ref('')

const STORAGE_KEY = 'employee_leave_requests'
const leaveRequests = ref([])

const currentUser = computed(() => {
  try {
    return JSON.parse(localStorage.getItem('user') || '{}').username || 'Ẩn danh'
  } catch (e) {
    return 'Ẩn danh'
  }
})

const daysCount = computed(() => {
  if (!fromDate.value || !toDate.value) return 0
  const from = new Date(fromDate.value)
  const to = new Date(toDate.value)
  const diff = Math.ceil((to - from) / (1000 * 3600 * 24)) + 1
  return diff > 0 ? diff : 0
})

const approvalLevel = computed(() => {
  const days = daysCount.value
  if (days === 0) return ''
  if (days <= 2) return 'Team Leader'
  if (days <= 5) return 'Manager'
  if (days < 10) return 'Director'
  return 'Board + HR'
})

const userRequests = computed(() => {
  return leaveRequests.value
    .filter(r => r.sender === currentUser.value)
    .map(r => ({
      ...r,
      daysCount: r.daysCount ?? calculateDays(r.fromDate, r.toDate),
      approver: r.approver || getApprovalLevel(r.daysCount ?? calculateDays(r.fromDate, r.toDate))
    }))
})

function calculateDays(from, to) {
  if (!from || !to) return 0
  const interval = Math.ceil((new Date(to) - new Date(from)) / (1000 * 3600 * 24)) + 1
  return interval > 0 ? interval : 0
}

function getApprovalLevel(days) {
  if (!days || days <= 0) return ''
  if (days <= 2) return 'Team Leader'
  if (days <= 5) return 'Manager'
  if (days < 10) return 'Director'
  return 'Board + HR'
}

function statusText(status) {
  if (status === 'pending') return 'Đang chờ'
  if (status === 'approved') return 'Đã duyệt'
  if (status === 'rejected') return 'Từ chối'
  return status
}

function saveLeaveRequest(request) {
  const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]')
  stored.unshift(request)
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stored))
}

function submitRequest() {
  if (!leaveType.value || !fromDate.value || !toDate.value || !reason.value.trim()) {
    message.value = 'Vui lòng điền đầy đủ thông tin bắt buộc.'
    return
  }

  if (new Date(toDate.value) < new Date(fromDate.value)) {
    message.value = 'Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.'
    return
  }

  const request = {
    id: `LV-${Date.now()}`,
    leaveType: leaveType.value,
    fromDate: fromDate.value,
    toDate: toDate.value,
    approver: approver.value || approvalLevel.value || 'Chưa rõ',
    reason: reason.value.trim(),
    isUnpaid: isUnpaid.value,
    daysCount: daysCount.value,
    status: 'pending',
    createdAt: new Date().toISOString(),
    sender: currentUser.value
  }

  saveLeaveRequest(request)
  message.value = 'Gửi yêu cầu nghỉ phép thành công. Vui lòng đợi phê duyệt.'
  resetForm(false)
  loadUserRequests()
}

function resetForm(clearMessage = true) {
  leaveType.value = ''
  fromDate.value = ''
  toDate.value = ''
  approver.value = ''
  reason.value = ''
  isUnpaid.value = false
  if (clearMessage) message.value = ''
}

function loadUserRequests() {
  const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]')
  leaveRequests.value = Array.isArray(stored) ? stored : []
}

onMounted(() => {
  loadUserRequests()
})
</script>
