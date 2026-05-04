<template>
  <div class="payroll-page">
    <Sidebar activeItem="Bảng lương" />
    <main class="payroll-main">
      <header class="payroll-header glass">
        <h1 class="payroll-title">Bảng lương</h1>
      </header>
      <div class="payroll-content">
        <div class="summary-cards">
          <div class="summary-card">
            <div class="summary-label">Tổng nhân viên</div>
            <div class="summary-value">{{ totalEmployees }}</div>
          </div>
          <div class="summary-card">
            <div class="summary-label">Tổng lương tháng</div>
            <div class="summary-value">{{ formatMoney(totalPayroll) }}</div>
          </div>
          <div class="summary-card">
            <div class="summary-label">Đã chi trả</div>
            <div class="summary-value">{{ formatMoney(paidAmount) }}</div>
          </div>
          <div class="summary-card">
            <div class="summary-label">Chờ thanh toán</div>
            <div class="summary-value">{{ formatMoney(pendingAmount) }}</div>
          </div>
        </div>

        <div v-if="error" class="error-simple">
          ⚠️ {{ error }}
        </div>

        <BaseCard class="payroll-card">
          <div class="filter-section">
            <div class="filter-left">
              <div class="filter-item">
                <label>Tháng</label>
                <select class="filter-select" v-model="filters.month">
                  <option v-for="m in 12" :key="m" :value="m">{{ String(m).padStart(2, '0') }}</option>
                </select>
              </div>
              <div class="filter-item">
                <label>Năm</label>
                <select class="filter-select" v-model="filters.year">
                  <option v-for="y in years" :key="y" :value="y">{{ y }}</option>
                </select>
              </div>
              <button class="btn-primary" @click="loadPayrolls" :disabled="loading">Áp dụng</button>
              <button class="btn-secondary" @click="resetFilters">Đặt lại</button>
            </div>
            <div class="filter-right">
              <input type="text" class="filter-search" placeholder="Tìm nhân viên..." v-model="filters.search">
            </div>
          </div>

          <div class="table-wrapper">
            <div v-if="loading" class="loading">Đang tải dữ liệu...</div>
            <table class="payroll-table" v-if="!loading">
              <thead>
                <tr>
                  <th>STT</th>
                  <th>Nhân viên</th>
                  <th>Phòng ban</th>
                  <th>Lương cơ bản</th>
                  <th>Phụ cấp</th>
                  <th>Thưởng</th>
                  <th>Khấu trừ</th>
                  <th>Thực lĩnh</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, idx) in pagedData" :key="idx">
                  <td>{{ (page - 1) * pageSize + idx + 1 }}</td>
                  <td><strong>{{ item.employeeName }}</strong></td>
                  <td>{{ item.departmentName }}</td>
                  <td>{{ formatMoney(item.basicSalary) }}</td>
                  <td>{{ formatMoney(item.allowance) }}</td>
                  <td>{{ formatMoney(item.bonus) }}</td>
                  <td>{{ formatMoney(item.deduction) }}</td>
                  <td>{{ formatMoney(item.netPay) }}</td>
                  <td>
                    <span class="status" :class="statusClass(item.status)">
                      {{ item.status }}
                    </span>
                  </td>
                </tr>
                <tr v-if="pagedData.length === 0 && !loading">
                  <td colspan="9" style="text-align: center; padding: 40px;">
                    Không có dữ liệu bảng lương
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="pagination-section" v-if="total > 0">
            <div class="pagination-info">
              Hiển thị {{ (page - 1) * pageSize + 1 }} - 
              {{ Math.min(page * pageSize, total) }} của {{ total }} kết quả
            </div>
            <div class="pagination">
              <button class="pagination-btn" @click="prevPage" :disabled="page <= 1">‹</button>
              <button 
                class="pagination-btn" 
                v-for="p in visiblePages" 
                :key="p" 
                :class="{ active: p === page }" 
                @click="goPage(p)"
              >
                {{ p }}
              </button>
              <button 
                class="pagination-btn" 
                @click="nextPage" 
                :disabled="page >= totalPages"
              >›</button>
            </div>
          </div>
        </BaseCard>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import api from '@/services/api'

const payrollData = ref([])
const loading = ref(false)
const error = ref(null)
const allEmployees = ref([]) // Lưu danh sách tất cả nhân viên

const filters = ref({
  month: new Date().getMonth() + 1,
  year: new Date().getFullYear(),
  search: ''
})

const years = computed(() => {
  const currentYear = new Date().getFullYear()
  return [currentYear, currentYear - 1, currentYear - 2]
})

const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

const totalEmployees = computed(() => payrollData.value.length)

const totalPayroll = computed(() => {
  return payrollData.value.reduce((sum, item) => sum + (item.netPay || 0), 0)
})

const paidAmount = computed(() => {
  return payrollData.value
    .filter(item => item.status === 'Đã trả' || item.status === 'APPROVED')
    .reduce((sum, item) => sum + (item.netPay || 0), 0)
})

const pendingAmount = computed(() => totalPayroll.value - paidAmount.value)

const totalPages = computed(() => Math.ceil(total.value / pageSize.value))

const visiblePages = computed(() => {
  const maxVisible = 5
  if (totalPages.value <= maxVisible) {
    return Array.from({ length: totalPages.value }, (_, i) => i + 1)
  }
  return [1, 2, 3, 4, 5]
})

const pagedData = computed(() => {
  let list = [...payrollData.value]
  
  if (filters.value.search) {
    const searchLower = filters.value.search.toLowerCase()
    list = list.filter(item => 
      item.employeeName.toLowerCase().includes(searchLower)
    )
  }
  
  const start = (page.value - 1) * pageSize.value
  const end = start + pageSize.value
  return list.slice(start, end)
})

const formatMoney = (value) => {
  if (value === null || value === undefined) return '0 đ'
  return value.toLocaleString('vi-VN') + ' đ'
}

const statusClass = (status) => {
  const paidStatuses = ['Đã trả', 'Paid', 'paid', 'APPROVED', 'COMPLETED']
  return paidStatuses.includes(status) ? 'status-paid' : 'status-pending'
}

/**
 * Lấy danh sách tất cả employee IDs
 */
const fetchAllEmployeeIds = async () => {
  try {
    const response = await api.employees.list({ pageSize: 9999 })
    let employees = []
    
    if (Array.isArray(response)) {
      employees = response
    } else if (response?.data && Array.isArray(response.data)) {
      employees = response.data
    } else if (response?.items && Array.isArray(response.items)) {
      employees = response.items
    } else if (response?.result && Array.isArray(response.result)) {
      employees = response.result
    } else {
      employees = []
    }
    
    allEmployees.value = employees
    return employees.map(emp => emp.employeeId ?? emp.id ?? emp.employeeID).filter(id => id)
  } catch (err) {
    console.error('Lỗi lấy danh sách nhân viên:', err)
    return []
  }
}

const loadPayrolls = async () => {
  loading.value = true
  error.value = null
  
  try {
    // Bước 1: Lấy danh sách employee IDs
    const employeeIds = await fetchAllEmployeeIds()
    
    if (employeeIds.length === 0) {
      error.value = 'Không tìm thấy danh sách nhân viên'
      payrollData.value = []
      total.value = 0
      loading.value = false
      return
    }
    
    console.log(`📋 Lấy được ${employeeIds.length} nhân viên:`, employeeIds)
    
    // Bước 2: Tạo payPeriod
    const payPeriod = `${filters.value.year}-${String(filters.value.month).padStart(2, '0')}-01`
    
    console.log('📤 Gọi API calculate-bulk với:', { payPeriod, employeeIds })
    
    // Bước 3: Gọi API tính lương với danh sách employeeIds
    const response = await api.payroll.calculateBulk(payPeriod, employeeIds)
    
    console.log('📥 Response:', response)
    
    // Chuẩn hóa response
    let rawData = []
    if (Array.isArray(response)) {
      rawData = response
    } else if (response?.data && Array.isArray(response.data)) {
      rawData = response.data
    } else if (response?.items && Array.isArray(response.items)) {
      rawData = response.items
    } else if (response?.result && Array.isArray(response.result)) {
      rawData = response.result
    } else {
      rawData = []
    }
    
    // Transform dữ liệu
    payrollData.value = rawData.map((item, index) => ({
      id: index,
      employeeId: item.employeeId ?? item.employeeID ?? item.employee?.id ?? null,
      employeeName: item.employeeName ?? item.fullName ?? item.employee?.fullName ?? `Nhân viên ${index + 1}`,
      departmentName: item.departmentName ?? item.department?.name ?? item.department ?? '',
      basicSalary: item.basicSalary ?? item.basic ?? item.grossSalary ?? 0,
      allowance: item.allowance ?? item.allowanceAmount ?? item.phuCap ?? 0,
      bonus: item.bonus ?? item.thuong ?? 0,
      deduction: item.deduction ?? item.khauTru ?? item.totalDeductions ?? 0,
      netPay: item.netPay ?? item.net ?? item.netSalary ?? 0,
      status: item.status ?? item.payStatus ?? (item.statusId === 1 ? 'Đã duyệt' : 'Chờ duyệt'),
      payrollId: item.payrollId ?? item.payrollID ?? item.id ?? null
    }))
    
    total.value = response?.totalCount ?? response?.total ?? response?.count ?? payrollData.value.length
    
    if (payrollData.value.length === 0) {
      error.value = `Không tìm thấy dữ liệu lương cho tháng ${filters.value.month}/${filters.value.year}`
    }
    
  } catch (err) {
    console.error('❌ Load payrolls error:', err)
    
    if (err.response?.status === 400) {
      const data = err.response?.data
      let message = ''
      if (data?.errors) {
        const errors = Object.values(data.errors).flat()
        message = errors.join(', ')
      } else if (data?.message) {
        message = data.message
      } else if (data?.title) {
        message = data.title
      } else {
        message = 'Sai định dạng request'
      }
      error.value = `Lỗi 400: ${message}`
    } else if (err.response?.status === 401) {
      error.value = 'Lỗi 401: Vui lòng đăng nhập lại'
    } else if (err.response?.status === 403) {
      error.value = 'Lỗi 403: Bạn không có quyền truy cập (cần role ADMIN hoặc HR)'
    } else if (err.code === 'ERR_NETWORK') {
      error.value = 'Lỗi kết nối: Không thể kết nối đến server'
    } else {
      error.value = err.message || 'Đã xảy ra lỗi khi tải dữ liệu'
    }
    
    payrollData.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  filters.value = {
    month: new Date().getMonth() + 1,
    year: new Date().getFullYear(),
    search: ''
  }
  page.value = 1
  loadPayrolls()
}

const prevPage = () => {
  if (page.value > 1) {
    page.value--
  }
}

const nextPage = () => {
  if (page.value < totalPages.value) {
    page.value++
  }
}

const goPage = (p) => {
  page.value = p
}

onMounted(() => {
  loadPayrolls()
})
</script>

<style src="@/CSS/Payroll.css" scoped></style>