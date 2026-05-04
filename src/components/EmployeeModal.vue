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

            <div v-if="form.department" class="base-input-group">
              <label class="base-label">Chức vụ</label>
              <select v-model="form.position" class="base-select glass" required>
                <option value="">Chọn chức vụ...</option>
                <option v-for="pos in filteredPositions" :key="pos.id" :value="pos.name">{{ pos.name }}</option>
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
              <select v-model="form.maritalStatus" class="base-select glass" required>
                <option value="">Chọn tình trạng...</option>
                <option v-for="status in maritalStatusOptions" :key="status" :value="status">{{ status }}</option>
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
import { ref, watch, onMounted, computed } from 'vue';
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
  positions: Array
});

const emit = defineEmits(['update:modelValue', 'save']);

const isEdit = ref(false);
const form = ref({
  name: '',
  email: '',
  phone: '',
  birthDate: '',
  employeeCode: '',
  department: '',
  position: '',
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

const contractTypes = ref([
  { id: 1, name: '1 năm' },
  { id: 2, name: '3 năm' },
  { id: 3, name: '5 năm' }
])
const genders = ref([
  { id: 1, name: 'Nam' },
  { id: 2, name: 'Nữ' },
  { id: 3, name: 'Khác' }
])
const maritalStatusOptions = ref([
  'Độc thân',
  'Đã kết hôn'
])
const positions = ref([
  { id: 1, name: 'Nhân viên' },
  { id: 2, name: 'Trưởng phòng' },
  { id: 3, name: 'Giám đốc' },
  { id: 4, name: 'Trưởng dự án' }
])

// Mapping of departments to their available positions
const departmentPositionsMap = {
  'công nghệ': [
    { id: 1, name: 'Nhân viên BackEnd' },
    { id: 2, name: 'Nhân viên FrontEnd' },
    { id: 3, name: 'Full Stack' },
    { id: 4, name: 'Trưởng phòng' }
  ],
  'it': [
    { id: 1, name: 'Nhân viên BackEnd' },
    { id: 2, name: 'Nhân viên FrontEnd' },
    { id: 3, name: 'Full Stack' },
    { id: 4, name: 'Trưởng phòng' }
  ],
  'phòng it': [
    { id: 1, name: 'Nhân viên BackEnd' },
    { id: 2, name: 'Nhân viên FrontEnd' },
    { id: 3, name: 'Full Stack' },
    { id: 4, name: 'Trưởng phòng' }
  ],
  'marketing': [
    { id: 5, name: 'Nhân viên' },
    { id: 6, name: 'Trưởng phòng' }
  ],
  'phòng marketing': [
    { id: 5, name: 'Nhân viên' },
    { id: 6, name: 'Trưởng phòng' }
  ],
  'nhân sự': [
    { id: 7, name: 'Nhân viên' },
    { id: 8, name: 'Trưởng phòng' }
  ],
  'phòng nhân sự': [
    { id: 7, name: 'Nhân viên' },
    { id: 8, name: 'Trưởng phòng' }
  ],
  'kế toán': [
    { id: 9, name: 'Nhân viên' },
    { id: 10, name: 'Trưởng phòng' }
  ],
  'phòng kế toán': [
    { id: 9, name: 'Nhân viên' },
    { id: 10, name: 'Trưởng phòng' }
  ]
}

// Compute filtered positions based on selected department
const filteredPositions = computed(() => {
  const deptKey = String(form.value.department || '').trim().toLowerCase()
  return departmentPositionsMap[deptKey] || positions.value
})

onMounted(async () => {
  // Load contract types
  try {
    console.log('🔄 Loading contract types from /ContractTypes...')
    const res = await api.contractTypes.list()
    console.log('📦 Contract types response:', res)
    
    let list = []
    if (Array.isArray(res)) {
      list = res
    } else if (res?.data && Array.isArray(res.data)) {
      list = res.data
    } else if (res?.items && Array.isArray(res.items)) {
      list = res.items
    } else {
      console.warn('⚠️ Unexpected contract types response format:', typeof res, res)
      list = []
    }
    
    if (list.length > 0) {
      const apiContractTypes = list.map(ct => ({
        id: ct.id ?? ct.contractTypeId ?? ct.ContractTypeId ?? ct.ContractTypeID ?? ct.ID ?? ct.Id,
        name: ct.name ?? ct.title ?? ct.contractTypeName ?? ct.typeName ?? ct.type ?? ct.contractName ?? ct.nameVi ?? ct.nameEn ?? `Contract Type ${ct.id || '?'}`
      }))
      contractTypes.value = apiContractTypes
      console.log('✅ Loaded contract types from API:', contractTypes.value)
    } else {
      console.warn('⚠️ No contract types returned from API, using defaults')
    }
  } catch (err) {
    console.error('❌ Failed to load contract types from API:', err?.message || err)
    console.error('   Error details:', err)
    console.log('   Using fallback defaults')
  }
  
  // Load genders
  try {
    console.log('🔄 Loading genders from /Genders...')
    const resG = await api.genders.list()
    console.log('📦 Genders response:', resG)
    
    let gList = []
    if (Array.isArray(resG)) {
      gList = resG
    } else if (resG?.data && Array.isArray(resG.data)) {
      gList = resG.data
    } else if (resG?.items && Array.isArray(resG.items)) {
      gList = resG.items
    } else {
      console.warn('⚠️ Unexpected genders response format:', typeof resG, resG)
      gList = []
    }
    
    if (gList.length > 0) {
      const apiGenders = gList.map(g => ({
        id: g.id ?? g.GenderID ?? g.genderId ?? g.ID ?? g.Id,
        name: g.name ?? g.GenderName ?? g.genderName ?? g.nameVi ?? g.nameEn ?? `Gender ${g.id || '?'}`
      }))
      genders.value = apiGenders
      console.log('✅ Loaded genders from API:', genders.value)
    } else {
      console.warn('⚠️ No genders returned from API, using defaults')
    }
  } catch (err) {
    console.error('❌ Failed to load genders from API:', err?.message || err)
    console.error('   Error details:', err)
    console.log('   Using fallback defaults')
  }

  // Load positions
  try {
    console.log('🔄 Loading positions from /Positions...')
    const resP = await api.positions.list()
    console.log('📦 Positions response:', resP)
    
    let pList = []
    if (Array.isArray(resP)) {
      pList = resP
    } else if (resP?.data && Array.isArray(resP.data)) {
      pList = resP.data
    } else if (resP?.items && Array.isArray(resP.items)) {
      pList = resP.items
    } else {
      console.warn('⚠️ Unexpected positions response format:', typeof resP, resP)
      pList = []
    }
    
    if (pList.length > 0) {
      const apiPositions = pList.map(p => ({
        id: p.id ?? p.PositionID ?? p.positionId ?? p.ID ?? p.Id,
        name: p.name ?? p.PositionName ?? p.positionName ?? p.nameVi ?? p.nameEn ?? `Position ${p.id || '?'}`
      }))
      positions.value = apiPositions
      console.log('✅ Loaded positions from API:', positions.value)
    } else {
      console.warn('⚠️ No positions returned from API, using defaults')
    }
  } catch (err) {
    console.error('❌ Failed to load positions from API:', err?.message || err)
    console.error('   Error details:', err)
    console.log('   Using fallback defaults')
  }
})

watch(() => props.employee, (newVal) => {
  if (newVal) {
    form.value = { ...newVal };
    // ensure account fields are present and enabled by default
    form.value.createUserAccount = form.value.createUserAccount ?? true;
    form.value.username = form.value.username ?? '';
    form.value.password = '';
    isEdit.value = true;
  } else {
    resetForm();
    isEdit.value = false;
  }
}, { immediate: true });

watch(() => form.value.department, (newDept) => {
  // When department changes, reset position to empty
  if (newDept) {
    form.value.position = '';
  }
});

function resetForm() {
  form.value = {
    name: '',
    email: '',
    phone: '',
    birthDate: '',
    employeeCode: '',
    department: '',
    position: '',
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
  };
}

function close() {
  emit('update:modelValue', false);
}

function submit() {
  emit('save', { ...form.value });
}
</script>


