<template>
  <div class="company-bank-accounts">
    <div class="page-header">
      <h1 class="page-title">Tài khoản ngân hàng công ty</h1>
      <button class="btn-primary" @click="openModal()">
        <i class="icon-plus"></i> Thêm tài khoản
      </button>
    </div>

    <!-- Filter -->
    <div class="filter-bar">
      <select v-model="filters.bankPartnerId" class="filter-select" @change="fetchData">
        <option value="">Tất cả ngân hàng</option>
        <option v-for="bank in bankPartners" :key="bank.id" :value="bank.id">
          {{ bank.bankName }}
        </option>
      </select>
      <select v-model="filters.currency" class="filter-select" @change="fetchData">
        <option value="">Tất cả loại tiền</option>
        <option value="VND">VND</option>
        <option value="USD">USD</option>
      </select>
    </div>

    <!-- Cards Grid -->
    <div class="accounts-grid">
      <div v-for="account in list" :key="account.id" class="account-card">
        <div class="card-header">
          <div class="bank-info">
            <span class="bank-name">{{ account.bankPartner?.bankName || account.bankName }}</span>
            <span v-if="account.isDefault" class="default-badge">Mặc định</span>
          </div>
          <div class="card-actions">
            <button class="icon-btn edit" @click="openModal(account)">✏️</button>
            <button class="icon-btn delete" @click="confirmDelete(account)">🗑️</button>
          </div>
        </div>
        <div class="card-body">
          <div class="account-number">
            <span class="label">Số tài khoản:</span>
            <span class="value">{{ account.accountNumber }}</span>
          </div>
          <div class="account-name">
            <span class="label">Chủ tài khoản:</span>
            <span class="value">{{ account.accountName }}</span>
          </div>
          <div class="branch">
            <span class="label">Chi nhánh:</span>
            <span class="value">{{ account.branch || '—' }}</span>
          </div>
          <div class="currency">
            <span class="label">Loại tiền:</span>
            <span class="value">{{ account.currency }}</span>
          </div>
        </div>
        <div class="card-footer">
          <button v-if="!account.isDefault" class="btn-set-default" @click="setDefault(account.id)">
            Đặt làm mặc định
          </button>
        </div>
      </div>
      <div v-if="list.length === 0" class="empty-state">
        <p>Chưa có tài khoản ngân hàng nào</p>
        <button class="btn-primary" @click="openModal()">Thêm tài khoản đầu tiên</button>
      </div>
    </div>

    <!-- Modal Form -->
    <div class="modal" v-if="showModal" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isEdit ? 'Sửa tài khoản' : 'Thêm tài khoản mới' }}</h3>
          <button class="close-btn" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Ngân hàng <span class="required">*</span></label>
            <select v-model="form.bankPartnerId" class="form-control">
              <option value="">-- Chọn ngân hàng --</option>
              <option v-for="bank in bankPartners" :key="bank.id" :value="bank.id">
                {{ bank.bankName }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Số tài khoản <span class="required">*</span></label>
            <input v-model="form.accountNumber" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Chủ tài khoản <span class="required">*</span></label>
            <input v-model="form.accountName" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Chi nhánh</label>
            <input v-model="form.branch" type="text" class="form-control" />
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Loại tiền</label>
              <select v-model="form.currency" class="form-control">
                <option value="VND">VND</option>
                <option value="USD">USD</option>
              </select>
            </div>
            <div class="form-group half">
              <label>Swift/BIC Code</label>
              <input v-model="form.swiftCode" type="text" class="form-control" />
            </div>
          </div>
          <div class="form-group">
            <label class="checkbox-label">
              <input type="checkbox" v-model="form.isDefault" />
              Đặt làm tài khoản mặc định
            </label>
          </div>
          <div class="form-group">
            <label>Ghi chú</label>
            <textarea v-model="form.notes" class="form-control" rows="2"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-secondary" @click="closeModal">Hủy</button>
          <button class="btn-primary" @click="save" :disabled="saving">
            {{ saving ? 'Đang lưu...' : 'Lưu' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import api from '../services/api'

export default {
  name: 'CompanyBankAccounts',
  data() {
    return {
      list: [],
      bankPartners: [],
      filters: {
        bankPartnerId: '',
        currency: ''
      },
      showModal: false,
      isEdit: false,
      saving: false,
      form: {
        id: null,
        bankPartnerId: '',
        accountNumber: '',
        accountName: '',
        branch: '',
        currency: 'VND',
        swiftCode: '',
        isDefault: false,
        notes: ''
      }
    }
  },
  mounted() {
    this.fetchBankPartners()
    this.fetchData()
  },
  methods: {
    async fetchBankPartners() {
      try {
        const res = await api.bankPartners.list({ isActive: true })
        this.bankPartners = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch bank partners:', error)
      }
    },
    async fetchData() {
      try {
        const params = {
          bankPartnerId: this.filters.bankPartnerId || undefined,
          currency: this.filters.currency || undefined
        }
        const res = await api.companyBankAccounts.list(params)
        this.list = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch bank accounts:', error)
        this.$toast?.error('Không thể tải danh sách tài khoản')
      }
    },
    openModal(account = null) {
      this.isEdit = !!account
      if (account) {
        this.form = { ...account }
      } else {
        this.resetForm()
      }
      this.showModal = true
    },
    resetForm() {
      this.form = {
        id: null,
        bankPartnerId: '',
        accountNumber: '',
        accountName: '',
        branch: '',
        currency: 'VND',
        swiftCode: '',
        isDefault: false,
        notes: ''
      }
    },
    async save() {
      if (!this.form.bankPartnerId || !this.form.accountNumber || !this.form.accountName) {
        this.$toast?.warning('Vui lòng nhập đầy đủ thông tin bắt buộc')
        return
      }
      this.saving = true
      try {
        if (this.isEdit) {
          await api.companyBankAccounts.update(this.form.id, this.form)
          this.$toast?.success('Cập nhật thành công')
        } else {
          await api.companyBankAccounts.create(this.form)
          this.$toast?.success('Thêm mới thành công')
        }
        this.closeModal()
        this.fetchData()
      } catch (error) {
        console.error('Save failed:', error)
        this.$toast?.error('Lưu thất bại')
      } finally {
        this.saving = false
      }
    },
    async setDefault(id) {
      try {
        await api.companyBankAccounts.setDefault(id)
        this.$toast?.success('Đặt tài khoản mặc định thành công')
        this.fetchData()
      } catch (error) {
        console.error('Set default failed:', error)
        this.$toast?.error('Thao tác thất bại')
      }
    },
    confirmDelete(account) {
      if (confirm(`Xóa tài khoản "${account.accountNumber}"?`)) {
        this.deleteItem(account.id)
      }
    },
    async deleteItem(id) {
      try {
        await api.companyBankAccounts.remove(id)
        this.$toast?.success('Xóa thành công')
        this.fetchData()
      } catch (error) {
        console.error('Delete failed:', error)
        this.$toast?.error('Xóa thất bại')
      }
    },
    closeModal() {
      this.showModal = false
      this.resetForm()
    }
  }
}
</script>

<style scoped>
.company-bank-accounts {
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
.filter-bar {
  display: flex;
  gap: 15px;
  margin-bottom: 24px;
}
.filter-select {
  padding: 8px 15px;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: white;
  min-width: 150px;
}
.accounts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
  gap: 20px;
}
.account-card {
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;
}
.account-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(0,0,0,0.12);
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}
.bank-info {
  display: flex;
  align-items: center;
  gap: 10px;
}
.bank-name {
  font-weight: 600;
  font-size: 16px;
}
.default-badge {
  background: rgba(255,255,255,0.2);
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
}
.card-actions {
  display: flex;
  gap: 8px;
}
.icon-btn {
  background: rgba(255,255,255,0.2);
  border: none;
  border-radius: 6px;
  padding: 5px 8px;
  cursor: pointer;
  color: white;
  transition: background 0.2s;
}
.icon-btn:hover {
  background: rgba(255,255,255,0.4);
}
.card-body {
  padding: 16px 20px;
}
.card-body .label {
  color: #6c757d;
  font-size: 12px;
  display: block;
  margin-bottom: 2px;
}
.card-body .value {
  font-weight: 500;
  color: #333;
}
.account-number,
.account-name,
.branch,
.currency {
  margin-bottom: 12px;
}
.card-footer {
  padding: 12px 20px;
  border-top: 1px solid #eee;
}
.btn-set-default {
  background: none;
  border: 1px solid #4361ee;
  color: #4361ee;
  padding: 6px 12px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 12px;
  transition: all 0.2s;
}
.btn-set-default:hover {
  background: #4361ee;
  color: white;
}
.empty-state {
  grid-column: 1 / -1;
  text-align: center;
  padding: 60px;
  background: white;
  border-radius: 16px;
  color: #6c757d;
}
.btn-primary {
  background: #4361ee;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
}
.btn-secondary {
  background: #e9ecef;
  color: #495057;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
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
.checkbox-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}
.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
}
</style>