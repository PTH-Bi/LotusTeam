<template>
  <div class="job-positions">
    <div class="page-header">
      <h1 class="page-title">Vị trí công việc</h1>
      <button class="btn-primary" @click="openModal()">
        <i>+</i> Thêm vị trí
      </button>
    </div>

    <!-- Tree View -->
    <div class="tree-container">
      <div class="tree-actions">
        <button class="btn-icon" @click="expandAll" title="Mở rộng tất cả">
          📂
        </button>
        <button class="btn-icon" @click="collapseAll" title="Thu gọn tất cả">
          📁
        </button>
        <button class="btn-icon" @click="fetchData" title="Làm mới">
          🔄
        </button>
      </div>
      
      <div class="tree-view">
        <TreeNode 
          v-for="node in treeData" 
          :key="node.id"
          :node="node"
          :level="0"
          @edit="openModal"
          @delete="confirmDelete"
          @addChild="openModalWithParent"
        />
      </div>
    </div>

    <!-- Modal Form -->
    <div class="modal" v-if="showModal" @click.self="closeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isEdit ? 'Sửa vị trí' : (parentId ? 'Thêm vị trí con' : 'Thêm vị trí mới') }}</h3>
          <button class="close-btn" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Mã vị trí <span class="required">*</span></label>
            <input v-model="form.code" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Tên vị trí <span class="required">*</span></label>
            <input v-model="form.positionName" type="text" class="form-control" />
          </div>
          <div class="form-group">
            <label>Vị trí cấp trên</label>
            <select v-model="form.parentId" class="form-control">
              <option :value="null">-- Không có (cấp cao nhất) --</option>
              <option v-for="pos in allPositions" :key="pos.id" :value="pos.id" :disabled="pos.id === form.id">
                {{ '— '.repeat(pos.level || 0) }}{{ pos.positionName }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>Mô tả</label>
            <textarea v-model="form.description" class="form-control" rows="3"></textarea>
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

// TreeNode Component
const TreeNode = {
  name: 'TreeNode',
  props: ['node', 'level'],
  emits: ['edit', 'delete', 'addChild'],
  template: `
    <div class="tree-node" :style="{ marginLeft: level * 24 + 'px' }">
      <div class="tree-node-header" :class="{ hasChildren: node.children?.length }">
        <span class="expand-icon" v-if="node.children?.length" @click="node.expanded = !node.expanded">
          {{ node.expanded ? '▼' : '▶' }}
        </span>
        <span class="expand-placeholder" v-else></span>
        <span class="node-name">{{ node.positionName }}</span>
        <span class="node-code">({{ node.code }})</span>
        <div class="node-actions">
          <button class="node-btn" @click="$emit('addChild', node)" title="Thêm cấp dưới">+</button>
          <button class="node-btn" @click="$emit('edit', node)" title="Sửa">✏️</button>
          <button class="node-btn" @click="$emit('delete', node)" title="Xóa">🗑️</button>
        </div>
      </div>
      <div v-if="node.expanded && node.children?.length" class="tree-node-children">
        <TreeNode 
          v-for="child in node.children" 
          :key="child.id"
          :node="child"
          :level="level + 1"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
          @addChild="$emit('addChild', $event)"
        />
      </div>
    </div>
  `
}

export default {
  name: 'JobPositions',
  components: { TreeNode },
  data() {
    return {
      treeData: [],
      allPositions: [],
      showModal: false,
      isEdit: false,
      parentId: null,
      saving: false,
      form: {
        id: null,
        code: '',
        positionName: '',
        parentId: null,
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
        const res = await api.jobPositions.tree()
        this.treeData = this.addExpandedFlag(res.data || [])
        
        // Get flat list for parent selection
        const flatRes = await api.jobPositions.list({ all: true })
        this.allPositions = this.buildFlatList(flatRes.data.items || flatRes.data || [])
      } catch (error) {
        console.error('Failed to fetch job positions:', error)
        this.$toast?.error('Không thể tải danh sách vị trí')
      }
    },
    addExpandedFlag(nodes) {
      return nodes.map(node => ({
        ...node,
        expanded: true,
        children: node.children ? this.addExpandedFlag(node.children) : []
      }))
    },
    buildFlatList(nodes, level = 0) {
      let result = []
      for (const node of nodes) {
        result.push({ ...node, level })
        if (node.children) {
          result = result.concat(this.buildFlatList(node.children, level + 1))
        }
      }
      return result
    },
    expandAll() {
      const expand = (nodes) => {
        nodes.forEach(node => {
          node.expanded = true
          if (node.children) expand(node.children)
        })
      }
      expand(this.treeData)
    },
    collapseAll() {
      const collapse = (nodes) => {
        nodes.forEach(node => {
          node.expanded = false
          if (node.children) collapse(node.children)
        })
      }
      collapse(this.treeData)
    },
    openModal(item = null, parent = null) {
      this.isEdit = !!item
      this.parentId = parent?.id || null
      
      if (item) {
        this.form = {
          id: item.id,
          code: item.code,
          positionName: item.positionName,
          parentId: item.parentId,
          description: item.description || '',
          isActive: item.isActive !== false
        }
      } else if (parent) {
        this.form = {
          id: null,
          code: '',
          positionName: '',
          parentId: parent.id,
          description: '',
          isActive: true
        }
      } else {
        this.resetForm()
      }
      this.showModal = true
    },
    openModalWithParent(parent) {
      this.openModal(null, parent)
    },
    resetForm() {
      this.form = {
        id: null,
        code: '',
        positionName: '',
        parentId: null,
        description: '',
        isActive: true
      }
    },
    async save() {
      if (!this.form.code || !this.form.positionName) {
        this.$toast?.warning('Vui lòng nhập mã và tên vị trí')
        return
      }
      this.saving = true
      try {
        if (this.isEdit) {
          await api.jobPositions.update(this.form.id, this.form)
          this.$toast?.success('Cập nhật thành công')
        } else {
          await api.jobPositions.create(this.form)
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
      if (confirm(`Bạn có chắc muốn xóa vị trí "${item.positionName}"? Các vị trí con cũng sẽ bị ảnh hưởng.`)) {
        this.deleteItem(item.id)
      }
    },
    async deleteItem(id) {
      try {
        await api.jobPositions.remove(id)
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
      this.parentId = null
    }
  }
}
</script>

<style scoped>
.job-positions {
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
.tree-container {
  background: white;
  border-radius: 12px;
  padding: 16px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
}
.tree-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid #eee;
}
.btn-icon {
  background: #f0f0f0;
  border: none;
  border-radius: 6px;
  padding: 6px 10px;
  cursor: pointer;
  font-size: 16px;
}
.tree-view {
  max-height: 70vh;
  overflow-y: auto;
}
.tree-node-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;
}
.tree-node-header:hover {
  background: #f8f9fa;
}
.expand-icon {
  width: 20px;
  font-size: 12px;
  cursor: pointer;
  color: #666;
}
.expand-placeholder {
  width: 20px;
}
.node-name {
  font-weight: 500;
  color: #333;
}
.node-code {
  font-size: 12px;
  color: #999;
}
.node-actions {
  margin-left: auto;
  display: flex;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.2s;
}
.tree-node-header:hover .node-actions {
  opacity: 1;
}
.node-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 14px;
  padding: 4px 6px;
  border-radius: 4px;
}
.node-btn:hover {
  background: #e9ecef;
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
}
.form-control {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
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
.btn-secondary {
  background: #e9ecef;
  color: #495057;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
}
</style>