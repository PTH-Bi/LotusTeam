<template>
  <div class="status-master">
    <div class="page-header">
      <h1 class="page-title">Quản lý trạng thái</h1>
      <button class="btn-primary" @click="openModal()">
        <i>+</i> Thêm trạng thái
      </button>
    </div>

    <!-- Tabs by Type -->
    <div class="tabs">
      <button 
        v-for="type in statusTypes" 
        :key="type"
        :class="['tab-btn', { active: activeType === type }]"
        @click="activeType = type; fetchData()"
      >
        {{ getTypeLabel(type) }}
        <span class="tab-count">{{ getCountByType(type) }}</span>
      </button>
    </div>

    <!-- Search -->
    <div class="search-bar">
      <input 
        type="text" 
        v-model="searchKeyword" 
        placeholder="Tìm kiếm theo tên trạng thái..."
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
            <th>Tên trạng thái</th>
            <th>Loại</th>
            <th>Màu sắc</th>
            <th>Thứ tự</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in list" :key="item.id">
            <td class="code-cell">{{ item.code }}</td>
            <td>{{ item.statusName }}</td>
            <td>
              <span :class="['type-badge', getTypeClass(item.type)]">
                {{ getTypeLabel(item.type) }}
              </span>
            </td>
            <td>
              <div class="color-preview" :style="{ backgroundColor: item.color }"></div>
              <span class="color-code">{{ item.color || '#000000' }}</span>
            </td>
            <td>{{ item.sortOrder }}</td>
            <td>
              <label class="switch">
                <input type="checkbox" v-model="item.isActive" @change="toggleActive(item)" />
                <span class="slider round"></span>
              </label>
            </td>
            <td class="action-buttons">
              <button class="btn-icon edit" @click="openModal(item)" title="Sửa">
                ✏️
              </button>
              <button class="btn-icon delete" @click="confirmDelete(item)" title="Xóa">
                🗑️
              </button>
            </td>
          </tr>
          <tr v-if="list.length === 0">
            <td colspan="7" class="empty-row">Không có dữ liệu</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal Form -->
    <div class="modal" v-if="showModal" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isEdit ? 'Sửa trạng thái' : 'Thêm trạng thái mới' }}</h3>
          <button class="close-btn" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Loại <span class="required">*</span></label>
            <select v-model="form.type" class="form-control" :disabled="isEdit">
              <option v-for="type in statusTypes" :key="type" :value="type">
                {{ getTypeLabel(type) }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Mã <span class="required">*</span></label>
            <input v-model="form.code" type="text" class="form-control" :disabled="isEdit" />
          </div>
          <div class="form-group">
            <label>Tên trạng thái <span class="required">*</span></label>
            <input v-model="form.statusName" type="text" class="form-control" />
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Màu sắc</label>
              <div class="color-input-group">
                <input v-model="form.color" type="color" class="color-input" />
                <input v-model="form.color" type="text" class="form-control color-text" placeholder="#RRGGBB" />
              </div>
            </div>
            <div class="form-group half">
              <label>Thứ tự</label>
              <input v-model.number="form.sortOrder" type="number" class="form-control" />
            </div>
          </div>
          <div class="form-group">
            <label>Mô tả</label>
            <textarea v-model="form.description" class="form-control" rows="2"></textarea>
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
  name: 'StatusMaster',
  data() {
    return {
      list: [],
      statusTypes: ['Leave', 'Contract', 'Recruitment', 'Project', 'Asset', 'Training', 'WorkReport'],
      activeType: 'Leave',
      searchKeyword: '',
      showModal: false,
      isEdit: false,
      saving: false,
      form: {
        id: null,
        type: 'Leave',
        code: '',
        statusName: '',
        color: '#4361ee',
        sortOrder: 0,
        description: '',
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
          type: this.activeType,
          keyword: this.searchKeyword || undefined
        }
        const res = await api.statusMaster.list(params)
        this.list = res.data.items || res.data || []
      } catch (error) {
        console.error('Failed to fetch statuses:', error)
        this.$toast?.error('Không thể tải danh sách trạng thái')
      }
    },
    async toggleActive(item) {
      try {
        await api.statusMaster.update(item.id, { isActive: item.isActive })
        this.$toast?.success(item.isActive ? 'Đã kích hoạt' : 'Đã vô hiệu hóa')
      } catch (error) {
        console.error('Toggle active failed:', error)
        item.isActive = !item.isActive
        this.$toast?.error('Thao tác thất bại')
      }
    },
    getCountByType(type) {
      return this.list.filter(item => item.type === type).length
    },
    getTypeLabel(type) {
      const labels = {
        Leave: 'Nghỉ phép',
        Contract: 'Hợp đồng',
        Recruitment: 'Tuyển dụng',
        Project: 'Dự án',
        Asset: 'Tài sản',
        Training: 'Đào tạo',
        WorkReport: 'Báo cáo công việc'
      }
      return labels[type] || type
    },
    getTypeClass(type) {
      const classes = {
        Leave: 'leave',
        Contract: 'contract',
        Recruitment: 'recruitment',
        Project: 'project',
        Asset: 'asset',
        Training: 'training',
        WorkReport: 'workreport'
      }
      return classes[type] || 'default'
    },
    openModal(item = null) {
      this.isEdit = !!item
      if (item) {
        this.form = {
          id: item.id,
          type: item.type,
          code: item.code,
          statusName: item.statusName,
          color: item.color || '#4361ee',
          sortOrder: item.sortOrder || 0,
          description: item.description || '',
          isActive: item.isActive !== false
        }
      } else {
        this.resetForm()
        this.form.type = this.activeType
      }
      this.showModal = true
    },
    resetForm() {
      this.form = {
        id: null,
        type: this.activeType,
        code: '',
        statusName: '',
        color: '#4361ee',
        sortOrder: 0,
        description: '',
        isActive: true
      }
    },
    async save() {
      if (!this.form.code || !this.form.statusName) {
        this.$toast?.warning('Vui lòng nhập mã và tên trạng thái')
        return
      }
      this.saving = true
      try {
        if (this.isEdit) {
          await api.statusMaster.update(this.form.id, this.form)
          this.$toast?.success('Cập nhật thành công')
        } else {
          await api.statusMaster.create(this.form)
          this.$toast?.success('Thêm mới thành công')
        }
        this.closeModal()
        this.fetchData()
      } catch (error) {
        console.error('Save failed:', error)
        this.$toast?.error(error.response?.data?.message || 'Lưu thất bại')
      } finally {
        this.saving = false
      }
    },
    confirmDelete(item) {
      if (confirm(`Bạn có chắc muốn xóa trạng thái "${item.statusName}"?`)) {
        this.deleteItem(item.id)
      }
    },
    async deleteItem(id) {
      try {
        await api.statusMaster.remove(id)
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
  },
  watch: {
    activeType() {
      this.fetchData()
    }
  }
}
</script>

<style scoped>
.status-master {
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
}
.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  background: white;
  padding: 6px;
  border-radius: 40px;
  display: inline-flex;
  flex-wrap: wrap;
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
  max-width: 300px;
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
.code-cell {
  font-weight: 500;
  color: #4361ee;
  font-family: monospace;
}
.type-badge {
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 500;
}
.type-badge.leave {
  background: #e3f2fd;
  color: #1976d2;
}
.type-badge.contract {
  background: #e8f5e9;
  color: #388e3c;
}
.type-badge.recruitment {
  background: #fce4ec;
  color: #c2185b;
}
.type-badge.project {
  background: #fff3e0;
  color: #f57c00;
}
.type-badge.asset {
  background: #e0f7fa;
  color: #0097a7;
}
.type-badge.training {
  background: #f3e5f5;
  color: #7b1fa2;
}
.type-badge.workreport {
  background: #fff8e1;
  color: #ff8f00;
}
.color-preview {
  width: 24px;
  height: 24px;
  border-radius: 4px;
  display: inline-block;
  vertical-align: middle;
  margin-right: 8px;
  border: 1px solid #ddd;
}
.color-code {
  font-size: 12px;
  color: #666;
  font-family: monospace;
}
.switch {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 24px;
}
.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}
.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  transition: 0.3s;
}
.slider:before {
  position: absolute;
  content: "";
  height: 18px;
  width: 18px;
  left: 3px;
  bottom: 3px;
  background-color: white;
  transition: 0.3s;
}
input:checked + .slider {
  background-color: #28a745;
}
input:checked + .slider:before {
  transform: translateX(20px);
}
.slider.round {
  border-radius: 24px;
}
.slider.round:before {
  border-radius: 50%;
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
.btn-icon.edit:hover {
  background: #e9ecef;
}
.btn-icon.delete:hover {
  background: #f8d7da;
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
.color-input-group {
  display: flex;
  gap: 10px;
  align-items: center;
}
.color-input {
  width: 50px;
  height: 40px;
  border: 1px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
}
.color-text {
  flex: 1;
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
.empty-row {
  text-align: center;
  padding: 40px;
  color: #6c757d;
}
</style>