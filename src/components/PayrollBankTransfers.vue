<template>
  <div class="payroll-bank-transfers">
    <div class="page-header">
      <h1 class="page-title">Chuyển lương qua ngân hàng</h1>
      <button class="btn-primary" @click="openCreateModal()">
        <i>+</i> Tạo đợt chuyển lương
      </button>
    </div>

    <!-- Filter -->
    <div class="filter-bar">
      <select v-model="filters.status" class="filter-select" @change="fetchData">
        <option value="">Tất cả trạng thái</option>
        <option value="Pending">Chờ xử lý</option>
        <option value="Processing">Đang xử lý</option>
        <option value="Completed">Hoàn thành</option>
        <option value="Failed">Thất bại</option>
      </select>
      <input 
        type="month" 
        v-model="filters.payPeriod" 
        class="filter-input"
        @change="fetchData"
      />
      <button class="btn-secondary" @click="fetchData">
        <i>🔍</i> Tìm kiếm
      </button>
    </div>

    <!-- Table -->
    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Mã đợt</th>
            <th>Kỳ lương</th>
            <th>Số nhân viên</th>
            <th>Tổng tiền</th>
            <th>Tài khoản nguồn</th>
            <th>Trạng thái</th>
            <th>Ngày tạo</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in list" :key="item.id">
            <td class="code-cell">{{ item.transferCode }}</td>
            <td>{{ item.payPeriod }}</td>
            <td>{{ item.employeeCount }}</td>
            <td class="amount-cell">{{ formatCurrency(item.totalAmount) }}</td>
            <td>{{ item.sourceAccount?.accountNumber || item.sourceAccountNumber }}</td>
            <td>
              <span :class="['status-badge', getStatusClass(item.status)]">
                {{ getStatusText(item.status) }}
              </span>
            </td>
            <td>{{ formatDate(item.createdAt) }}</td>
            <td class="action-buttons">
              <button class="btn-icon view" @click="viewDetail(item)" title="Xem chi tiết">
                👁️
              </button>
              <button 
                v-if="item.status === 'Pending'"
                class="btn-icon process" 
                @click="processTransfer(item)" 
                title="Xử lý"
              >
                ⚡
              </button>
              <button 
                v-if="item.status === 'Processing'"
                class="btn-icon confirm" 
                @click="confirmTransfer(item)" 
                title="Xác nhận"
              >
                ✅
              </button>
              <button 
                class="btn-icon export" 
                @click="exportTransfer(item)" 
                title="Xuất file"
              >
                📄
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

    <!-- Create Modal -->
    <div class="modal" v-if="showCreateModal" @click.self="closeCreateModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>Tạo đợt chuyển lương mới</h3>
          <button class="close-btn" @click="closeCreateModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Kỳ lương <span class="required">*</span></label>
            <input 
              v-model="createForm.payPeriod" 
              type="month" 
              class="form-control"
              required
            />
          </div>
          <div class="form-group">
            <label>Tài khoản ngân hàng nguồn <span class="required">*</span></label>
            <select v-model="createForm.sourceAccountId" class="form-control">
              <option value="">-- Chọn tài khoản --</option>
              <option 
                v-for="account in bankAccounts" 
                :key="account.id" 
                :value="account.id"
              >
                {{ account.bankPartner?.bankName || account.bankName }} - 
                {{ account.accountNumber }}
                {{ account.isDefault ? '(Mặc định)' : '' }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Chọn nhân viên</label>
            <div class="employee-selection">
              <div class="selection-actions">
                <button type="button" class="btn-sm" @click="selectAllEmployees">Chọn tất cả</button>
                <button type="button" class="btn-sm" @click="clearAllEmployees">Bỏ chọn</button>
              </div>
              <div class="employee-list">
                <label v-for="emp in employees" :key="emp.id" class="employee-checkbox">
                  <input type="checkbox" v-model="selectedEmployeeIds" :value="emp.id" />
                  <span>{{ emp.fullName }} - {{ emp.department?.departmentName || '—' }}</span>
                  <span class="salary">{{ formatCurrency(emp.baseSalary) }}</span>
                </label>
              </div>
            </div>
          </div>
          <div class="form-group">
            <label>Ghi chú</label>
            <textarea v-model="createForm.notes" class="form-control" rows="2"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeCreateModal">Hủy</button>
          <button class="btn-primary" @click="createTransfer" :disabled="creating">
            {{ creating ? 'Đang tạo...' : 'Tạo đợt chuyển' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <div class="modal" v-if="showDetailModal" @click.self="closeDetailModal">
      <div class="modal-content modal-large">
        <div class="modal-header">
          <h3>Chi tiết đợt chuyển lương</h3>
          <button class="close-btn" @click="closeDetailModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="detail-info">
            <div class="info-row">
              <span class="label">Mã đợt:</span>
              <span class="value">{{ selectedTransfer?.transferCode }}</span>
            </div>
            <div class="info-row">
              <span class="label">Kỳ lương:</span>
              <span class="value">{{ selectedTransfer?.payPeriod }}</span>
            </div>
            <div class="info-row">
              <span class="label">Tài khoản nguồn:</span>
              <span class="value">{{ selectedTransfer?.sourceAccountNumber }}</span>
            </div>
            <div class="info-row">
              <span class="label">Tổng tiền:</span>
              <span class="value amount">{{ formatCurrency(selectedTransfer?.totalAmount) }}</span>
            </div>
            <div class="info-row">
              <span class="label">Trạng thái:</span>
              <span :class="['status-badge', getStatusClass(selectedTransfer?.status)]">
                {{ getStatusText(selectedTransfer?.status) }}
              </span>
            </div>
          </div>
          
          <h4 class="subtitle">Danh sách nhân viên</h4>
          <table class="detail-table">
            <thead>
              <tr>
                <th>STT</th>
                <th>Mã NV</th>
                <th>Họ tên</th>
                <th>Số tài khoản</th>
                <th>Ngân hàng</th>
                <th>Số tiền</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(item, idx) in transferDetails" :key="item.id">
                <td>{{ idx + 1 }}</td>
                <td>{{ item.employeeCode }}</td>
                <td>{{ item.employeeName }}</td>
                <td>{{ item.bankAccountNumber }}</td>
                <td>{{ item.bankName }}</td>
                <td class="amount-cell">{{ formatCurrency(item.amount) }}</td>
                <td>
                  <span :class="['status-badge', getDetailStatusClass(item.status)]">
                    {{ getDetailStatusText(item.status) }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeDetailModal">Đóng</button>
          <button v-if="selectedTransfer?.status === 'Completed'" class="btn-primary" @click="exportTransfer(selectedTransfer)">
            Xuất báo cáo
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import api from '../services/api'

export default {
  name: 'PayrollBankTransfers',
  data() {
    return {
      list: [],
      bankAccounts: [],
      employees: [],
      selectedEmployeeIds: [],
      transferDetails: [],
      filters: {
        status: '',
        payPeriod: ''
      },
      currentPage: 1,
      pageSize: 20,
      totalPages: 1,
      showCreateModal: false,
      showDetailModal: false,
      creating: false,
      selectedTransfer: null,
      createForm: {
        payPeriod: '',
        sourceAccountId: '',
        notes: ''
      }
    }
  },
  mounted() {
    this.fetchData()
    this.fetchBankAccounts()
    this.fetchEmployees()
    // Set default pay period to current month
    const now = new Date()
    this.createForm.payPeriod = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  },
  methods: {
    async fetchData() {
      try {
        const params = {
          page: this.currentPage,
          pageSize: this.pageSize,
          status: this.filters.status || undefined,
          payPeriod: this.filters.payPeriod || undefined
        }
        const res = await api.payrollBankTransfers.list(params)
        this.list = res.data.items || res.data || []
        this.totalPages = res.data.totalPages || 1
      } catch (error) {
        console.error('Failed to fetch transfers:', error)
        this.$toast?.error('Không thể tải danh sách chuyển lương')
      }
    },
    async fetchBankAccounts() {
      try {
        const res = await api.companyBankAccounts.list({ isActive: true })
        this.bankAccounts = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch bank accounts:', error)
      }
    },
    async fetchEmployees() {
      try {
        const res = await api.employees.list({ status: 'Active' })
        this.employees = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch employees:', error)
      }
    },
    async fetchTransferDetails(transferId) {
      try {
        const res = await api.payrollBankTransfers.get(transferId)
        this.transferDetails = res.data.details || res.data.items || []
      } catch (error) {
        console.error('Failed to fetch transfer details:', error)
      }
    },
    selectAllEmployees() {
      this.selectedEmployeeIds = this.employees.map(emp => emp.id)
    },
    clearAllEmployees() {
      this.selectedEmployeeIds = []
    },
    async createTransfer() {
      if (!this.createForm.payPeriod || !this.createForm.sourceAccountId) {
        this.$toast?.warning('Vui lòng chọn kỳ lương và tài khoản nguồn')
        return
      }
      if (this.selectedEmployeeIds.length === 0) {
        this.$toast?.warning('Vui lòng chọn ít nhất một nhân viên')
        return
      }
      
      this.creating = true
      try {
        const data = {
          payPeriod: this.createForm.payPeriod,
          sourceAccountId: this.createForm.sourceAccountId,
          employeeIds: this.selectedEmployeeIds,
          notes: this.createForm.notes
        }
        const res = await api.payrollBankTransfers.create(data)
        this.$toast?.success('Tạo đợt chuyển lương thành công')
        this.closeCreateModal()
        this.fetchData()
      } catch (error) {
        console.error('Create transfer failed:', error)
        this.$toast?.error(error.response?.data?.message || 'Tạo đợt chuyển lương thất bại')
      } finally {
        this.creating = false
      }
    },
    async processTransfer(item) {
      if (confirm(`Xử lý đợt chuyển lương "${item.transferCode}"?`)) {
        try {
          await api.payrollBankTransfers.process(item.id)
          this.$toast?.success('Đã bắt đầu xử lý chuyển lương')
          this.fetchData()
        } catch (error) {
          console.error('Process transfer failed:', error)
          this.$toast?.error('Xử lý thất bại')
        }
      }
    },
    async confirmTransfer(item) {
      if (confirm(`Xác nhận hoàn thành đợt chuyển lương "${item.transferCode}"?`)) {
        try {
          await api.payrollBankTransfers.confirm(item.id)
          this.$toast?.success('Đã xác nhận hoàn thành chuyển lương')
          this.fetchData()
        } catch (error) {
          console.error('Confirm transfer failed:', error)
          this.$toast?.error('Xác nhận thất bại')
        }
      }
    },
    async exportTransfer(item) {
      try {
        const res = await api.payrollBankTransfers.export(item.id)
        const url = window.URL.createObjectURL(new Blob([res.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `chuyen_luong_${item.transferCode}.xlsx`)
        document.body.appendChild(link)
        link.click()
        link.remove()
        window.URL.revokeObjectURL(url)
        this.$toast?.success('Xuất file thành công')
      } catch (error) {
        console.error('Export failed:', error)
        this.$toast?.error('Xuất file thất bại')
      }
    },
    async viewDetail(item) {
      this.selectedTransfer = item
      await this.fetchTransferDetails(item.id)
      this.showDetailModal = true
    },
    openCreateModal() {
      this.selectedEmployeeIds = []
      this.createForm.notes = ''
      this.showCreateModal = true
    },
    closeCreateModal() {
      this.showCreateModal = false
    },
    closeDetailModal() {
      this.showDetailModal = false
      this.selectedTransfer = null
      this.transferDetails = []
    },
    formatCurrency(value) {
      if (!value) return '0 ₫'
      return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
    },
    formatDate(date) {
      if (!date) return '—'
      const d = new Date(date)
      return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    },
    getStatusClass(status) {
      const classes = {
        Pending: 'warning',
        Processing: 'info',
        Completed: 'success',
        Failed: 'danger'
      }
      return classes[status] || 'secondary'
    },
    getStatusText(status) {
      const texts = {
        Pending: 'Chờ xử lý',
        Processing: 'Đang xử lý',
        Completed: 'Hoàn thành',
        Failed: 'Thất bại'
      }
      return texts[status] || status
    },
    getDetailStatusClass(status) {
      const classes = {
        Pending: 'warning',
        Processing: 'info',
        Completed: 'success',
        Failed: 'danger',
        Skipped: 'secondary'
      }
      return classes[status] || 'secondary'
    },
    getDetailStatusText(status) {
      const texts = {
        Pending: 'Chờ',
        Processing: 'Đang xử lý',
        Completed: 'Thành công',
        Failed: 'Thất bại',
        Skipped: 'Bỏ qua'
      }
      return texts[status] || status
    },
    changePage(page) {
      if (page >= 1 && page <= this.totalPages) {
        this.currentPage = page
        this.fetchData()
      }
    }
  }
}
</script>

<style scoped>
.payroll-bank-transfers {
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
  margin: 0;
}
.btn-primary {
  background: #4361ee;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.3s;
}
.btn-primary:hover {
  background: #3a56d4;
}
.btn-secondary {
  background: #e9ecef;
  color: #495057;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
}
.btn-sm {
  background: #e9ecef;
  border: none;
  padding: 4px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
}
.filter-bar {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}
.filter-select, .filter-input {
  padding: 8px 15px;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: white;
  min-width: 150px;
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
  color: #495057;
}
.data-table tr:hover {
  background: #f8f9fa;
}
.code-cell {
  font-weight: 500;
  color: #4361ee;
}
.amount-cell {
  font-weight: 500;
  color: #28a745;
}
.status-badge {
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 500;
  display: inline-block;
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
.status-badge.info {
  background: #d1ecf1;
  color: #0c5460;
}
.status-badge.secondary {
  background: #e9ecef;
  color: #6c757d;
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
  border-radius: 4px;
  transition: background 0.2s;
}
.btn-icon:hover {
  background: #f0f0f0;
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
  width: 500px;
  max-width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}
.modal-large {
  width: 900px;
  max-width: 95%;
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
  color: #333;
}
.form-control {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
}
.required {
  color: #dc3545;
}
.employee-selection {
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
}
.selection-actions {
  padding: 10px;
  background: #f8f9fa;
  border-bottom: 1px solid #ddd;
  display: flex;
  gap: 10px;
}
.employee-list {
  max-height: 300px;
  overflow-y: auto;
}
.employee-checkbox {
  display: flex;
  align-items: center;
  padding: 10px;
  border-bottom: 1px solid #eee;
  cursor: pointer;
  gap: 10px;
}
.employee-checkbox:hover {
  background: #f8f9fa;
}
.employee-checkbox .salary {
  margin-left: auto;
  color: #28a745;
  font-size: 13px;
}
.detail-info {
  background: #f8f9fa;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 20px;
}
.info-row {
  display: flex;
  margin-bottom: 10px;
}
.info-row .label {
  width: 120px;
  font-weight: 500;
  color: #6c757d;
}
.info-row .value {
  flex: 1;
  color: #333;
}
.info-row .value.amount {
  color: #28a745;
  font-weight: 600;
}
.subtitle {
  font-size: 16px;
  font-weight: 600;
  margin: 20px 0 15px 0;
  color: #1a1a2e;
}
.detail-table {
  width: 100%;
  border-collapse: collapse;
}
.detail-table th,
.detail-table td {
  padding: 10px 12px;
  text-align: left;
  border-bottom: 1px solid #eee;
}
.detail-table th {
  background: #f8f9fa;
  font-weight: 600;
  font-size: 13px;
}
.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
}
.empty-row {
  text-align: center;
  padding: 40px;
  color: #6c757d;
}
</style>