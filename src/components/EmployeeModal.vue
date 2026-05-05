<template>
  <div v-if="modelValue" class="modal-overlay flex-center" @click.self="close">
    <BaseCard class="employee-modal" :glass="true">
      <template #header>
        <div class="modal-header-content">
          <h3 class="modal-title">{{ isEdit ? 'Chỉnh sửa nhân viên' : 'Thêm nhân viên mới' }}</h3>
          <BaseButton variant="ghost" iconOnly @click="close">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </BaseButton>
        </div>
      </template>

      <form @submit.prevent="submit" class="employee-form">
        <div class="form-grid">
          <div class="form-column">
            <h4 class="section-title">Thông tin cá nhân</h4>
            <BaseInput v-model="form.name" label="Họ và tên" required placeholder="Nhập họ tên..." />
            <BaseInput v-model="form.email" label="Email" type="email" required placeholder="example@company.com" />
            <BaseInput v-model="form.phone" label="Số điện thoại" placeholder="0123..." />
            <BaseInput v-model="form.birthDate" label="Ngày sinh" type="date" />
            <div class="base-input-group">
              <label class="base-label">Giới tính</label>
              <select v-model="form.genderId" class="base-select glass">
                <option :value="null">Chọn giới tính...</option>
                <option v-for="g in genders" :key="g.id" :value="g.id">{{ g.name }}</option>
              </select>
            </div>
          </div>

          <div class="form-column">
            <h4 class="section-title">Thông tin công việc</h4>
            <BaseInput v-model="form.employeeCode" label="Mã nhân viên" placeholder="NV-0000" :disabled="isEdit" />
            
            <div class="base-input-group">
              <label class="base-label">Phòng ban</label>
              <select v-model="form.department" class="base-select glass" required>
                <option value="">Chọn phòng ban...</option>
                <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
              </select>
            </div>

            <div class="base-input-group">
              <label class="base-label">Chức vụ</label>
              <select v-model="form.positionId" class="base-select glass" required>
                <option :value="null">Chọn chức vụ...</option>
                <option v-for="pos in positions" :key="pos.positionId" :value="pos.positionId">
                  {{ pos.positionName }}
                </option>
              </select>
            </div>

            <BaseInput v-model="form.startDate" label="Ngày bắt đầu" type="date" required />
            <BaseInput v-model.number="form.baseSalary" label="Lương cơ bản" type="number" placeholder="0" />
          </div>
        </div>

        <div class="form-grid mt-4">
          <div class="form-column">
            <h4 class="section-title">Thông tin bổ sung</h4>
            <BaseInput v-model="form.address" label="Địa chỉ" placeholder="Nhập địa chỉ" />
            <div class="base-input-group">
              <label class="base-label">Tình trạng hôn nhân</label>
              <select v-model.number="form.maritalStatus" class="base-select glass" required>
                <option :value="null">Chọn tình trạng...</option>
                <option v-for="s in maritalStatusOptions" :key="s.id" :value="s.id">{{ s.label }}</option>
              </select>
            </div>
            <BaseInput v-model="form.identityNumber" label="Số CMND/CCCD" placeholder="Số chứng minh" />
            <BaseInput v-model="form.bankAccount" label="Tài khoản ngân hàng" placeholder="Số TK" />
            <BaseInput v-model="form.taxCode" label="Mã số thuế" placeholder="Mã số thuế" />
            <BaseInput v-model="form.emergencyContactName" label="Người liên hệ khẩn cấp" placeholder="Họ tên" />
            <BaseInput v-model="form.emergencyContactPhone" label="SĐT liên hệ khẩn cấp" placeholder="SĐT" />
          </div>

          <div class="form-column">
            <h4 class="section-title">Tài khoản & Hợp đồng</h4>
            <h5 class="subsection-title">Tài khoản người dùng</h5>
            <BaseInput v-model="form.username" label="Tên đăng nhập" placeholder="username" />
            <BaseInput v-model="form.password" label="Mật khẩu" type="password" placeholder="password" />

            <h5 class="subsection-title">Hợp đồng khởi tạo</h5>
            <div class="base-input-group">
              <label class="base-label">Loại hợp đồng</label>
              <select v-model.number="form.initialContract.contractTypeId" class="base-select glass">
                <option :value="null">Chọn loại hợp đồng...</option>
                <option v-for="ct in contractTypes" :key="ct.id" :value="ct.id">{{ ct.name }}</option>
              </select>
            </div>
            <BaseInput v-model="form.initialContract.startDate" label="Start Date" type="date" />
            <BaseInput v-model="form.initialContract.endDate" label="End Date" type="date" />
            <BaseInput v-model.number="form.initialContract.salary" label="Contract Salary" type="number" />
            <BaseInput v-model="form.initialContract.signedDate" label="Signed Date" type="date" />
          </div>
        </div>

        <div class="modal-actions">
          <BaseButton variant="secondary" @click="close" type="button">Hủy bỏ</BaseButton>
          <BaseButton variant="primary" :loading="loading" type="submit">
            {{ isEdit ? 'Cập nhật' : 'Thêm mới' }}
          </BaseButton>
        </div>
      </form>
    </BaseCard>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue';
import BaseCard from './base/BaseCard.vue';
import BaseButton from './base/BaseButton.vue';
import BaseInput from './base/BaseInput.vue';
import '@/CSS/EmployeeModal.css'
import api from '@/services/api'

const props = defineProps({
  modelValue: Boolean,
  employee: Object,
  loading: Boolean,
  departments: Array,
  positions: Array  // có thể truyền từ parent hoặc tự load
});

const emit = defineEmits(['update:modelValue', 'save']);

const isEdit = ref(false);

// Form data structure
const form = ref({
  name: '',
  email: '',
  phone: '',
  birthDate: '',
  employeeCode: '',
  department: '',
  positionId: null,
  startDate: '',
  status: 'Đang làm việc',
  genderId: null,
  baseSalary: null,
  address: '',
  maritalStatus: null,
  identityNumber: null,
  bankAccount: null,
  taxCode: null,
  emergencyContactName: null,
  emergencyContactPhone: null,
  initialContract: {
    contractTypeId: null,
    startDate: null,
    endDate: null,
    salary: null,
    signedDate: null
  },
  createUserAccount: true,
  username: '',
  password: ''
});

// Local data from API
const genders = ref([])
const contractTypes = ref([])
const positions = ref([])  // tự load từ API

const maritalStatusOptions = [
  { id: 1, label: 'Đã kết hôn' },
  { id: 2, label: 'Chưa kết hôn' }
]

// Load positions từ API
async function loadPositions() {
  try {
    console.log('🔄 Loading positions from /Positions...')
    const response = await api.positions.list()
    console.log('📦 Raw positions response:', response)
    
    // Xử lý response từ API
    let positionsData = []
    if (response?.data && Array.isArray(response.data)) {
      positionsData = response.data
    } else if (Array.isArray(response)) {
      positionsData = response
    } else if (response?.items && Array.isArray(response.items)) {
      positionsData = response.items
    }
    
    // Map đúng cấu trúc từ API: positionId, positionName
    positions.value = positionsData.map(p => ({
      positionId: p.positionId,
      positionName: p.positionName
    }))
    
    console.log('✅ Loaded positions:', positions.value)
  } catch (err) {
    console.error('❌ Failed to load positions:', err)
    positions.value = []
  }
}

// Load genders từ API
async function loadGenders() {
  try {
    console.log('🔄 Loading genders from /Genders...')
    const response = await api.genders.list()
    
    let gendersData = []
    if (response?.data && Array.isArray(response.data)) {
      gendersData = response.data
    } else if (Array.isArray(response)) {
      gendersData = response
    } else if (response?.items && Array.isArray(response.items)) {
      gendersData = response.items
    }
    
    genders.value = gendersData.map(g => ({
      id: g.id ?? g.genderId ?? g.GenderID,
      name: g.name ?? g.genderName ?? g.GenderName
    }))
    
    console.log('✅ Loaded genders:', genders.value)
  } catch (err) {
    console.error('❌ Failed to load genders:', err)
  }
}

// Load contract types từ API
async function loadContractTypes() {
  try {
    console.log('🔄 Loading contract types from /ContractTypes...')
    const response = await api.contractTypes.list()
    
    let contractData = []
    if (response?.data && Array.isArray(response.data)) {
      contractData = response.data
    } else if (Array.isArray(response)) {
      contractData = response
    } else if (response?.items && Array.isArray(response.items)) {
      contractData = response.items
    }
    
    contractTypes.value = contractData.map(ct => ({
      id: ct.id ?? ct.contractTypeId ?? ct.ContractTypeID,
      name: ct.name ?? ct.contractTypeName ?? ct.title
    }))
    
    console.log('✅ Loaded contract types:', contractTypes.value)
  } catch (err) {
    console.error('❌ Failed to load contract types:', err)
  }
}

// Load all data khi component mount
onMounted(async () => {
  await Promise.all([
    loadPositions(),
    loadGenders(),
    loadContractTypes()
  ])
})

// Watch for employee prop changes (edit mode)
watch(() => props.employee, (newVal) => {
  if (newVal) {
    form.value = { ...newVal }
    form.value.createUserAccount = form.value.createUserAccount ?? true
    form.value.username = form.value.username ?? ''
    form.value.password = ''
    isEdit.value = true
  } else {
    resetForm()
    isEdit.value = false
  }
}, { immediate: true, deep: true })

function resetForm() {
  form.value = {
    name: '',
    email: '',
    phone: '',
    birthDate: '',
    employeeCode: '',
    department: '',
    positionId: null,
    startDate: '',
    status: 'Đang làm việc',
    genderId: null,
    baseSalary: null,
    address: '',
    maritalStatus: null,
    identityNumber: null,
    bankAccount: null,
    taxCode: null,
    emergencyContactName: null,
    emergencyContactPhone: null,
    initialContract: {
      contractTypeId: null,
      startDate: null,
      endDate: null,
      salary: null,
      signedDate: null
    },
    createUserAccount: true,
    username: '',
    password: ''
  }
}

function close() {
  emit('update:modelValue', false)
}

function submit() {
  // Debug: kiểm tra positionId trước khi submit
  console.log('🔍 Submitting form with positionId:', form.value.positionId)
  console.log('📋 Full form data:', JSON.parse(JSON.stringify(form.value)))
  
  // Kiểm tra nếu positionId bị null hoặc undefined
  if (!form.value.positionId && form.value.positionId !== 0) {
    console.warn('⚠️ positionId is null or undefined!')
  }
  
  emit('save', { ...form.value })
}
</script>

<style scoped>
/* Your existing styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 1000;
}

.employee-modal {
  width: 90%;
  max-width: 1200px;
  max-height: 90vh;
  overflow-y: auto;
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.mt-4 {
  margin-top: 24px;
}

  .section-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
  color: #000;
}

.subsection-title {
  font-size: 0.95rem;
  font-weight: 500;
  margin: 16px 0 12px 0;
  color: #333;
}

  .base-input-group {
  margin-bottom: 16px;
}

.base-label {
  display: block;
  margin-bottom: 6px;
  font-size: 0.85rem;
  font-weight: 500;
  color: #000;
}

  .base-select {
  width: 100%;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid rgba(0, 0, 0, 0.08);
  background: rgba(255, 255, 255, 0.95);
  color: #000;
  font-size: 0.9rem;
}

.base-select:focus {
  outline: none;
  border-color: #4f46e5;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 32px;
  padding-top: 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.modal-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.modal-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
}

.flex-center {
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>