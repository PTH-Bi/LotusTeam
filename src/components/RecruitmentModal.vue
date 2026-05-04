<template>
  <div v-if="modelValue" class="modal-overlay" @click.self="close">
    <div class="recruitment-modal">
      <div class="modal-header-content">
        <h3 class="modal-title">{{ isEdit ? 'Chỉnh sửa ứng viên' : 'Thêm tuyển dụng mới' }}</h3>
        <BaseButton variant="ghost" iconOnly @click="close">
          <span class="modal-close-icon">×</span>
        </BaseButton>
      </div>
      
      <div class="modal-description">
        Điền thông tin chi tiết để bắt đầu chiến dịch tìm kiếm nhân tài cho hệ thống TalentFlow Pro.
      </div>

      <form @submit.prevent="submit" class="recruitment-form">
        <!-- Thông tin người đăng -->
        <div class="form-section">
          <h4 class="section-title">Thông tin người đăng</h4>
          <div class="section-content">
            <div class="base-input-group">
              <label class="base-label">Người viết post tuyển dụng</label>
              <input v-model="form.name" type="text" class="base-input" placeholder="Nhập họ và tên" required />
            </div>

            <div class="form-row">
              <div class="base-input-group flex-1">
                <label class="base-label">Email liên hệ</label>
                <input v-model="form.email" type="email" class="base-input" placeholder="example@talentflow.pro" required />
              </div>
              <div class="base-input-group flex-1">
                <label class="base-label">Số điện thoại</label>
                <input v-model="form.phone" type="text" class="base-input" placeholder="09xx xxx xxx" />
              </div>
            </div>
          </div>
        </div>

        <!-- Phân loại -->
        <div class="form-section">
          <h4 class="section-title">Phân loại</h4>
          <div class="section-content">
            <div class="form-row">
              <div class="base-input-group flex-1">
                <label class="base-label">Vị trí ứng tuyển</label>
                <div class="select-wrapper">
                  <select v-model="form.position" class="base-select">
                    <option value="">Chọn vị trí</option>
                    <option v-for="pos in positionsLocal" :key="pos.id ?? pos" :value="pos.id ?? pos">{{ pos.name ?? pos.title ?? pos }}</option>
                  </select>
                  <span class="dropdown-icon">▼</span>
                </div>
              </div>
              <div class="base-input-group flex-1">
                <label class="base-label">Phòng ban</label>
                <div class="select-wrapper">
                  <select v-model="form.department" class="base-select">
                    <option value="">Chọn phòng ban</option>
                    <option v-for="dept in departmentsLocal" :key="dept.id ?? dept" :value="dept.id ?? dept">{{ dept.name ?? dept.title ?? dept }}</option>
                  </select>
                  <span class="dropdown-icon">▼</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Nội dung chi tiết bài viết -->
        <div class="form-section">
          <h4 class="section-title">Nội dung chi tiết bài viết</h4>
          <div class="section-content">
            <div class="base-input-group">
              <label class="base-label">Link bài viết ở các nền tảng mxh</label>
              <input v-model="form.resume" type="text" class="base-input" placeholder="https://facebook.com/posts/..." />
              <span class="input-hint">Hỗ trợ Facebook, LinkedIn, Twitter và các website tuyển dụng.</span>
            </div>

            <div class="base-input-group">
              <label class="base-label">Ghi chú thêm</label>
              <textarea v-model="form.note" class="base-textarea" rows="3" placeholder="Nhập các yêu cầu bổ sung hoặc mô tả công việc tóm tắt..."></textarea>
            </div>
          </div>
        </div>


        <!-- Preview note -->
        <div class="preview-note">
          Xem trước hiển thị sẽ xuất hiện sau khi nhập đủ thông tin.
        </div>

        <div class="modal-actions">
          <BaseButton variant="secondary" @click="close" type="button">Hủy bỏ</BaseButton>
          <BaseButton variant="primary" type="submit">
            {{ isEdit ? 'Cập nhật' : 'Tạo tin mới' }}
          </BaseButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue';
import api from '@/services/api'
import BaseButton from './base/BaseButton.vue';
import '@/CSS/RecruitmentModal.css'

const props = defineProps({
  modelValue: Boolean,
  item: Object,
  positions: Array,
  departments: Array,
  templates: Array
});

const emit = defineEmits(['update:modelValue', 'save']);

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

const isEdit = ref(false);
const form = ref({
  name: '',
  email: '',
  phone: '',
  position: '',
  department: '',
  resume: '',
  note: ''
});

const positionsLocal = ref(props.positions?.length ? props.positions : defaultPositions)
const departmentsLocal = ref(props.departments?.length ? props.departments : defaultDepartments)

function extractArray(resp) {
  if (!resp) return []
  if (Array.isArray(resp)) return resp
  if (resp.data && Array.isArray(resp.data)) return resp.data
  if (resp.items && Array.isArray(resp.items)) return resp.items
  return []
}

watch(() => props.positions, (v) => { positionsLocal.value = v || [] })
watch(() => props.departments, (v) => { departmentsLocal.value = v || [] })

onMounted(async () => {
  if (!positionsLocal.value.length) {
    try {
      const resp = await api.positions.list()
      const arr = extractArray(resp)
      positionsLocal.value = arr.length ? arr.map(p => ({ id: p.id ?? p.positionId ?? p.positionId, name: p.name ?? p.title ?? p.positionName ?? p })) : defaultPositions
    } catch (e) {
      positionsLocal.value = defaultPositions
    }
  }

  if (!departmentsLocal.value.length) {
    try {
      const resp = await api.departments?.list?.() ?? []
      const arr = extractArray(resp)
      departmentsLocal.value = arr.length ? arr.map(d => ({ id: d.id ?? d.departmentId ?? d.id, name: d.name ?? d.title ?? d.departmentName ?? d })) : defaultDepartments
    } catch (e) {
      departmentsLocal.value = defaultDepartments
    }
  }
})

watch(() => props.item, (newVal) => {
  if (newVal) {
    let positionId = newVal.positionId ?? (newVal.position && (newVal.position.id ?? newVal.position)) ?? null
    if (!positionId && newVal.position) {
      const found = (positionsLocal.value || []).find(p => {
        const name = (p.name || p.title || p.positionName || p).toString().toLowerCase()
        return name === newVal.position.toString().toLowerCase()
      })
      if (found) positionId = found.id ?? found.positionId
    }
    form.value = {
      name: newVal.name ?? newVal.fullName ?? newVal.candidateName ?? '',
      email: newVal.email ?? '',
      phone: newVal.phone ?? newVal.contactPhone ?? '',
      position: positionId ?? '',
      department: newVal.departmentName ?? newVal.department ?? newVal.department_id ?? '',
      resume: newVal.resumeUrl ?? newVal.cvUrl ?? '',
      note: newVal.note ?? ''
    }
    isEdit.value = true;
  } else {
    resetForm();
    isEdit.value = false;
  }
}, { immediate: true });

function resetForm() {
  form.value = {
    name: '',
    email: '',
    phone: '',
    position: '',
    department: '',
    resume: '',
    note: ''
  };
}

function close() {
  emit('update:modelValue', false);
}

function submit() {
  const payload = {
    name: form.value.name,
    email: form.value.email,
    phone: form.value.phone,
    positionId: form.value.position || null,
    departmentId: form.value.department || null,
    resumeUrl: form.value.resume || '',
    note: form.value.note || ''
  }
  emit('save', payload);
}
</script>