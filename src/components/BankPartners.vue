<template>
  <div class="bank-partners-container">
    <!-- Header -->
    <div class="page-header">
      <h1 class="page-title">Đối tác ngân hàng</h1>
      <button class="btn-primary" @click="openModal()">
        <i class="icon-plus"></i> Thêm đối tác
      </button>
    </div>

    <!-- Search -->
    <div class="search-bar">
      <input 
        type="text" 
        v-model="searchKeyword" 
        placeholder="Tìm kiếm theo tên ngân hàng..."
        class="search-input"
        @keyup.enter="fetchData"
      />
      <button class="btn-secondary" @click="fetchData">Tìm kiếm</button>
    </div>

    <!-- Table -->
    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Mã</th>
            <th>Tên ngân hàng</th>
            <th>Tên giao dịch</th>
            <th>Số điện thoại</th>
            <th>Website</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in list" :key="item.id">
            <td>{{ item.code }}</td>
            <td>{{ item.bankName }}</td>
            <td>{{ item.tradingName }}</td>
            <td>{{ item.phone }}</td>
            <td>{{ item.website }}</td>
            <td>
              <span :class="['status-badge', item.isActive ? 'active' : 'inactive']">
                {{ item.isActive ? 'Hoạt động' : 'Không hoạt động' }}
              </span>
            </td>
            <td class="action-buttons">
              <button class="btn-icon edit" @click="openModal(item)" title="Sửa">
                <i class="icon-edit"></i>
              </button>
              <button class="btn-icon delete" @click="confirmDelete(item)" title="Xóa">
                <i class="icon-delete"></i>
              </button>
            </td>
          </tr>
          <tr v-if="list.length === 0">
            <td colspan="7" class="empty-row">Không có dữ liệu</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button @click="changePage(currentPage - 1)" :disabled="currentPage === 1">
        &laquo; Trước
      </button>
      <span class="page-info">Trang {{ currentPage }} / {{ totalPages }}</span>
      <button @click="changePage(currentPage + 1)" :disabled="currentPage === totalPages">
        Sau &raquo;
      </button>
    </div>

    <!-- Modal Form -->
    <div class="modal" v-if="showModal" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isEdit ? 'Sửa đối tác' : 'Thêm đối tác mới' }}</h3>
          <button class="close-btn" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Mã đối tác <span class="required">*</span></label>
            <input v-model="form.code" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Tên ngân hàng <span class="required">*</span></label>
            <input v-model="form.bankName" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Tên giao dịch</label>
            <input v-model="form.tradingName" type="text" class="form-control" />
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Số điện thoại</label>
              <input v-model="form.phone" type="text" class="form-control" />
            </div>
            <div class="form-group half">
              <label>Email</label>
              <input v-model="form.email" type="email" class="form-control" />
            </div>
          </div>
          <div class="form-group">
            <label>Website</label>
            <input v-model="form.website" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Địa chỉ</label>
            <textarea v-model="form.address" class="form-control" rows="2"></textarea>
          </div>
          <div class="form-group">
            <label class="checkbox-label">
              <input type="checkbox" v-model="form.isActive" />
              Hoạt động
            </label>
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
  name: 'BankPartners',
  data() {
    return {
      list: [],
      searchKeyword: '',
      currentPage: 1,
      pageSize: 20,
      totalPages: 1,
      showModal: false,
      isEdit: false,
      saving: false,
      form: {
        id: null,
        code: '',
        bankName: '',
        tradingName: '',
        phone: '',
        email: '',
        website: '',
        address: '',
        isActive: true
      }
    }
  },
  mounted() {
    this.fetchData()
  },
  methods: {
    async fetchData() {
      try {
        const params = {
          page: this.currentPage,
          pageSize: this.pageSize,
          keyword: this.searchKeyword
        }
        const res = await api.bankPartners.list(params)
        if (res.data) {
          this.list = res.data.items || res.data
          this.totalPages = res.data.totalPages || 1
        }
      } catch (error) {
        console.error('Failed to fetch bank partners:', error)
        this.$toast?.error('Không thể tải danh sách đối tác ngân hàng')
      }
    },
    openModal(item = null) {
      this.isEdit = !!item
      if (item) {
        this.form = { ...item }
      } else {
        this.resetForm()
      }
      this.showModal = true
    },
    resetForm() {
      this.form = {
        id: null,
        code: '',
        bankName: '',
        tradingName: '',
        phone: '',
        email: '',
        website: '',
        address: '',
        isActive: true
      }
    },
    async save() {
      if (!this.form.code || !this.form.bankName) {
        this.$toast?.warning('Vui lòng nhập mã và tên ngân hàng')
        return
      }
      this.saving = true
      try {
        if (this.isEdit) {
          await api.bankPartners.update(this.form.id, this.form)
          this.$toast?.success('Cập nhật thành công')
        } else {
          await api.bankPartners.create(this.form)
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
    confirmDelete(item) {
      if (confirm(`Bạn có chắc muốn xóa đối tác "${item.bankName}"?`)) {
        this.deleteItem(item.id)
      }
    },
    async deleteItem(id) {
      try {
        await api.bankPartners.remove(id)
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
/* CSS sẽ để riêng - bạn có thể tách ra file BankPartners.css */
.bank-partners-container {
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
.search-bar {
  display: flex;
  gap: 10px;
  margin-bottom: 20px;
}
.search-input {
  flex: 1;
  padding: 10px 15px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
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
.status-badge {
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 500;
}
.status-badge.active {
  background: #d4edda;
  color: #155724;
}
.status-badge.inactive {
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
  padding: 5px;
  border-radius: 4px;
}
.btn-icon.edit {
  color: #4361ee;
}
.btn-icon.delete {
  color: #dc3545;
}
.empty-row {
  text-align: center;
  color: #6c757d;
  padding: 40px;
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
  color: #999;
}
</style>