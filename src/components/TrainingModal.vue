<template>
  <div v-if="modelValue" class="modal-overlay" @click.self="close">
    <div class="modal-container">
      <div class="modal-header">
        <h3 class="modal-title">{{ isEdit ? 'Chỉnh sửa khóa đào tạo' : 'Thêm khóa đào tạo mới' }}</h3>
        <button class="close-btn" @click="close">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 6L6 18M6 6l12 12" />
          </svg>
        </button>
      </div>

      <form @submit.prevent="submit" class="modal-form">
        <!-- THÔNG TIN KHÓA HỌC -->
        <div class="form-section">
          <h4 class="section-title">THÔNG TIN KHÓA HỌC</h4>
          
          <div class="form-group">
            <label>Tên khóa học</label>
            <input 
              v-model="form.name" 
              type="text"
              placeholder="VD: Lập trình Vue.js nâng cao..."
              required
            />
          </div>

          <div class="form-group">
            <label>Mô tả chi tiết</label>
            <textarea 
              v-model="form.description" 
              rows="3"
              placeholder="Nhập mô tả chi tiết nội dung khóa học..."
            ></textarea>
          </div>

          <div class="form-row">
            <div class="form-group flex-1">
              <label>Phòng ban</label>
              <select v-model="form.department" required>
                <option value="" disabled>Chọn phòng ban</option>
                <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
              </select>
            </div>
            <div class="form-group flex-1">
              <label>Hình thức</label>
              <select v-model="form.format">
                <option value="Online">Online</option>
                <option value="Offline">Offline</option>
              </select>
            </div>
          </div>

          <div class="form-group">
            <label>Địa điểm / Link cuộc họp</label>
            <input 
              v-model="form.location" 
              type="text"
              placeholder="Phòng họp A1 hoặc Google Meet link..."
            />
          </div>
        </div>

        <!-- NGƯỜI CHỦ TRÌ & THỜI GIAN -->
        <div class="form-section">
          <h4 class="section-title">NGƯỜI CHỦ TRÌ & THỜI GIAN</h4>

          <div class="form-group">
            <label>Người chủ trì</label>
            <input 
              v-model="form.instructor" 
              type="text"
              placeholder="Tìm kiếm nhân viên..."
              required
            />
          </div>

          <div class="form-group">
            <label>Chức danh</label>
            <input 
              v-model="form.instructorTitle" 
              type="text"
              placeholder="Giảng viên nội bộ"
            />
          </div>

          <div class="form-group">
            <label>Ngày tổ chức</label>
            <input 
              v-model="form.date" 
              type="date"
              required
            />
          </div>
        </div>

        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="close">Hủy</button>
          <button type="submit" class="btn-primary">{{ isEdit ? 'Cập nhật' : 'Lưu khóa học' }}</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import '@/CSS/TrainingModal.css'

const props = defineProps({
  modelValue: Boolean,
  item: Object
})

const emit = defineEmits(['update:modelValue', 'save'])

const isEdit = ref(false)
const form = ref({
  name: '',
  instructor: '',
  instructorTitle: '',
  department: '',
  format: 'Online',
  location: '',
  date: new Date().toISOString().split('T')[0],
  description: ''
})

const departments = ['Công nghệ', 'Kỹ thuật', 'Kế toán', 'Nhân sự', 'Kinh doanh', 'Marketing']

watch(() => props.item, (newVal) => {
  if (newVal) {
    form.value = {
      name: newVal.trainingName || newVal.name || '',
      instructor: newVal.trainer || newVal.instructor || '',
      instructorTitle: newVal.instructorTitle || 'Giảng viên nội bộ',
      department: newVal.department || '',
      format: newVal.format || (newVal.location ? 'Offline' : 'Online'),
      location: newVal.location || '',
      date: newVal.startDate ? newVal.startDate.split('T')[0] : new Date().toISOString().split('T')[0],
      description: newVal.description || ''
    }
    isEdit.value = true
  } else {
    resetForm()
    isEdit.value = false
  }
}, { immediate: true })

function resetForm() {
  form.value = {
    name: '',
    instructor: '',
    instructorTitle: '',
    department: '',
    format: 'Online',
    location: '',
    date: new Date().toISOString().split('T')[0],
    description: ''
  }
}

function close() {
  emit('update:modelValue', false)
}

function submit() {
  if (!form.value.name || !form.value.instructor || !form.value.department || !form.value.date) {
    alert('Vui lòng điền đầy đủ thông tin bắt buộc')
    return
  }

  emit('save', { ...form.value })
}
</script>