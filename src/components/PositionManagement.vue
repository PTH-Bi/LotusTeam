<template>
  <div class="position-management">
    <div class="page-header">
      <h1 class="page-title">Quản lý đề xuất vị trí</h1>
      <button class="btn-primary" @click="openModal()">
        <i>+</i> Đề xuất vị trí mới
      </button>
    </div>

    <!-- Tabs -->
    <div class="tabs">
      <button 
        v-for="tab in tabs" 
        :key="tab.value"
        :class="['tab-btn', { active: activeTab === tab.value }]"
        @click="activeTab = tab.value; fetchData()"
      >
        {{ tab.label }}
        <span v-if="tab.count !== undefined" class="tab-count">{{ tab.count }}</span>
      </button>
    </div>

    <!-- Table -->
    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Mã đề xuất</th>
            <th>Vị trí</th>
            <th>Phòng ban</th>
            <th>Số lượng</th>
            <th>Người đề xuất</th>
            <th>Ngày đề xuất</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in list" :key="item.id">
            <td>{{ item.requestCode }}</td>
            <td>{{ item.positionName }}</td>
            <td>{{ item.departmentName }}</td>
            <td>{{ item.quantity }}</td>
            <td>{{ item.requestedByName }}</td>
            <td>{{ formatDate(item.requestDate) }}</td>
            <td>
              <span :class="['status-badge', getStatusClass(item.status)]">
                {{ getStatusText(item.status) }}
              </span>
            </td>
            <td class="action-buttons">
              <button class="btn-icon view" @click="viewDetail(item)" title="Xem chi tiết">
                👁️
              </button>
              <button 
                v-if="item.status === 'Pending' && canApprove"
                class="btn-icon approve" 
                @click="approve(item)" 
                title="Duyệt"
              >
                ✅
              </button>
              <button 
                v-if="item.status === 'Pending' && canApprove"
                class="btn-icon reject" 
                @click="reject(item)" 
                title="Từ chối"
              >
                ❌
              </button>
            </td>
          </tr>
          <tr v-if="list.length === 0">
            <td colspan="8" class="empty-row">Không có dữ liệu</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button @click="changePage(currentPage - 1)" :disabled="currentPage === 1">
        &laquo; Trước
      </button>
      <span>Trang {{ currentPage }} / {{ totalPages }}</span>
      <button @click="changePage(currentPage + 1)" :disabled="currentPage === totalPages">
        Sau &raquo;
      </button>
    </div>

    <!-- Modal Form -->
    <div class="modal" v-if="showModal" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isView ? 'Chi tiết đề xuất' : 'Đề xuất vị trí mới' }}</h3>
          <button class="close-btn" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Vị trí cần tuyển <span class="required">*</span></label>
            <input v-model="form.positionName" type="text" class="form-control" :disabled="isView" />
          </div>
          <div class="form-group">
            <label>Phòng ban <span class="required">*</span></label>
            <select v-model="form.departmentId" class="form-control" :disabled="isView">
              <option value="">-- Chọn phòng ban --</option>
              <option v-for="dept in departments" :key="dept.id" :value="dept.id">
                {{ dept.departmentName }}
              </option>
            </select>
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Số lượng <span class="required">*</span></label>
              <input v-model.number="form.quantity" type="number" min="1" class="form-control" :disabled="isView" />
            </div>
            <div class="form-group half">
              <label>Mức lương đề xuất</label>
              <input v-model.number="form.proposedSalary" type="number" class="form-control" :disabled="isView" />
            </div>
          </div>
          <div class="form-group">
            <label>Ngày cần</label>
            <input v-model="form.requiredDate" type="date" class="form-control" :disabled="isView" />
          </div>
          <div class="form-group">
            <label>Mô tả công việc</label>
            <textarea v-model="form.jobDescription" class="form-control" rows="4" :disabled="isView"></textarea>
          </div>
          <div class="form-group">
            <label>Yêu cầu</label>
            <textarea v-model="form.requirements" class="form-control" rows="3" :disabled="isView"></textarea>
          </div>
          <div class="form-group" v-if="isView && form.rejectReason">
            <label>Lý do từ chối</label>
            <div class="reject-reason">{{ form.rejectReason }}</div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeModal">Đóng</button>
          <button v-if="!isView" class="btn-primary" @click="submitRequest" :disabled="saving">
            {{ saving ? 'Đang gửi...' : 'Gửi đề xuất' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Reject Modal -->
    <div class="modal" v-if="showRejectModal" @click.self="closeRejectModal">
      <div class="modal-content" style="width: 400px;">
        <div class="modal-header">
          <h3>Từ chối đề xuất</h3>
          <button class="close-btn" @click="closeRejectModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Lý do từ chối</label>
            <textarea v-model="rejectReason" class="form-control" rows="3"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeRejectModal">Hủy</button>
          <button class="btn-danger" @click="submitReject">Xác nhận từ chối</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import api from '../services/api'

export default {
  name: 'PositionManagement',
  data() {
    return {
      list: [],
      departments: [],
      activeTab: 'pending',
      tabs: [
        { value: 'pending', label: 'Chờ duyệt' },
        { value: 'approved', label: 'Đã duyệt' },
        { value: 'rejected', label: 'Từ chối' },
        { value: 'all', label: 'Tất cả' }
      ],
      currentPage: 1,
      pageSize: 20,
      totalPages: 1,
      showModal: false,
      showRejectModal: false,
      isView: false,
      saving: false,
      selectedItem: null,
      rejectReason: '',
      form: {
        positionName: '',
        departmentId: '',
        quantity: 1,
        proposedSalary: null,
        requiredDate: '',
        jobDescription: '',
        requirements: ''
      }
    }
  },
  computed: {
    canApprove() {
      const user = JSON.parse(localStorage.getItem('user') || '{}')
      return user.role === 'Admin' || user.role === 'HR' || user.role === 'Manager'
    }
  },
  mounted() {
    this.fetchDepartments()
    this.fetchData()
  },
  methods: {
    async fetchDepartments() {
      try {
        const res = await api.departments.list()
        this.departments = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch departments:', error)
      }
    },
    async fetchData() {
      try {
        const params = {
          page: this.currentPage,
          pageSize: this.pageSize,
          status: this.activeTab !== 'all' ? this.activeTab : undefined
        }
        const res = await api.positionManagement.list(params)
        this.list = res.data.items || res.data || []
        this.totalPages = res.data.totalPages || 1
      } catch (error) {
        console.error('Failed to fetch position requests:', error)
        this.$toast?.error('Không thể tải danh sách đề xuất')
      }
    },
    openModal() {
      this.isView = false
      this.resetForm()
      this.showModal = true
    },
    viewDetail(item) {
      this.isView = true
      this.form = {
        positionName: item.positionName,
        departmentId: item.departmentId,
        quantity: item.quantity,
        proposedSalary: item.proposedSalary,
        requiredDate: item.requiredDate?.split('T')[0] || '',
        jobDescription: item.jobDescription || '',
        requirements: item.requirements || '',
        rejectReason: item.rejectReason
      }
      this.selectedItem = item
      this.showModal = true
    },
    resetForm() {
      this.form = {
        positionName: '',
        departmentId: '',
        quantity: 1,
        proposedSalary: null,
        requiredDate: '',
        jobDescription: '',
        requirements: ''
      }
    },
    async submitRequest() {
      if (!this.form.positionName || !this.form.departmentId) {
        this.$toast?.warning('Vui lòng nhập đầy đủ thông tin')
        return
      }
      this.saving = true
      try {
        await api.positionManagement.create(this.form)
        this.$toast?.success('Đã gửi đề xuất thành công')
        this.closeModal()
        this.fetchData()
      } catch (error) {
        console.error('Submit failed:', error)
        this.$toast?.error('Gửi đề xuất thất bại')
      } finally {
        this.saving = false
      }
    },
    approve(item) {
      if (confirm(`Duyệt đề xuất tuyển "${item.positionName}"?`)) {
        this.updateStatus(item.id, 'Approved')
      }
    },
    reject(item) {
      this.selectedItem = item
      this.rejectReason = ''
      this.showRejectModal = true
    },
    async updateStatus(id, status, reason = null) {
      try {
        if (status === 'Approved') {
          await api.positionManagement.approve(id, { notes: 'Đã duyệt' })
          this.$toast?.success('Đã duyệt đề xuất')
        } else {
          await api.positionManagement.reject(id, { reason })
          this.$toast?.success('Đã từ chối đề xuất')
        }
        this.fetchData()
      } catch (error) {
        console.error('Update status failed:', error)
        this.$toast?.error('Thao tác thất bại')
      }
    },
    async submitReject() {
      if (this.selectedItem) {
        await this.updateStatus(this.selectedItem.id, 'Rejected', this.rejectReason)
        this.closeRejectModal()
      }
    },
    formatDate(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    },
    getStatusClass(status) {
      const classes = {
        Pending: 'warning',
        Approved: 'success',
        Rejected: 'danger'
      }
      return classes[status] || 'secondary'
    },
    getStatusText(status) {
      const texts = {
        Pending: 'Chờ duyệt',
        Approved: 'Đã duyệt',
        Rejected: 'Từ chối'
      }
      return texts[status] || status
    },
    changePage(page) {
      if (page >= 1 && page <= this.totalPages) {
        this.currentPage = page
        this.fetchData()
      }
    },
    closeModal() {
      this.showModal = false
      this.selectedItem = null
    },
    closeRejectModal() {
      this.showRejectModal = false
      this.selectedItem = null
      this.rejectReason = ''
    }
  }
}
</script>

<style scoped>
.position-management {
  padding: 20px;
  background: #f5f7fa;
  min-height: 100vh;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.page-title {
  font-size: 24px;
  font-weight: 600;
  color: #1a1a2e;
}
.btn-primary {
  background: #4361ee;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
}
.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  background: white;
  padding: 8px;
  border-radius: 40px;
  display: inline-flex;
}
.tab-btn {
  padding: 8px 20px;
  border: none;
  background: transparent;
  border-radius: 32px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}
.tab-btn.active {
  background: #4361ee;
  color: white;
}
.tab-count {
  background: rgba(0,0,0,0.1);
  border-radius: 20px;
  padding: 0 6px;
  margin-left: 6px;
  font-size: 11px;
}
.table-container {
  background: white;
  border-radius: 12px;
  overflow-x: auto;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
}
.data-table {
  width: 100%;
  border-collapse: collapse;
}
.data-table th,
.data-table td {
  padding: 12px 15px;
  text-align: left;
  border-bottom: 1px solid #eee;
}
.data-table th {
  background: #f8f9fa;
  font-weight: 600;
}
.status-badge {
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 500;
}
.status-badge.success {
  background: #d4edda;
  color: #155724;
}
.status-badge.warning {
  background: #fff3cd;
  color: #856404;
}
.status-badge.danger {
  background: #f8d7da;
  color: #721c24;
}
.action-buttons {
  display: flex;
  gap: 8px;
}
.btn-icon {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 18px;
  padding: 4px;
}
.pagination {
  display: flex;
  justify-content: center;
  gap: 15px;
  margin-top: 20px;
  align-items: center;
}
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0,0,0,0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}
.modal-content {
  background: white;
  border-radius: 16px;
  width: 550px;
  max-width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #eee;
}
.modal-body {
  padding: 20px;
}
.modal-footer {
  padding: 16px 20px;
  border-top: 1px solid #eee;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.form-group {
  margin-bottom: 16px;
}
.form-group label {
  display: block;
  margin-bottom: 6px;
  font-weight: 500;
}
.form-control {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
}
.form-row {
  display: flex;
  gap: 15px;
}
.form-group.half {
  flex: 1;
}
.required {
  color: #dc3545;
}
.reject-reason {
  padding: 10px;
  background: #f8d7da;
  border-radius: 8px;
  color: #721c24;
}
.btn-secondary {
  background: #e9ecef;
  color: #495057;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
}
.btn-danger {
  background: #dc3545;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
}
.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
}
.empty-row {
  text-align: center;
  padding: 40px;
  color: #6c757d;
}
</style>