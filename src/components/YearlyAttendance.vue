<template>
  <div class="attendance-page">
    <Sidebar activeItem="Chấm theo năm" />

    <main class="attendance-main">
      <header class="attendance-header">
        <div class="header-left">
          <h2 class="yearly-title">Chấm công theo năm</h2>
          <p class="page-subtitle">Bảng tổng hợp công và giờ làm thêm {{ selectedYear }}</p>
        </div>
        <div class="header-right">
          <select v-model="selectedYear" class="year-select">
            <option v-for="year in years" :key="year" :value="year">Năm {{ year }}</option>
          </select>
          <BaseButton variant="primary" @click="exportCSV" size="medium">
            <template #icon-left>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="7 10 12 15 17 10"></polyline>
                <line x1="12" y1="15" x2="12" y2="3"></line>
              </svg>
            </template>
            Xuất dữ liệu
          </BaseButton>
        </div>
      </header>

      <div class="attendance-content">
        <!-- Thanh tìm kiếm và thống kê nhanh -->
        <div class="toolbar">
          <div class="search-box">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8" />
              <path d="M21 21l-4.35-4.35" />
            </svg>
            <input 
              type="text" 
              v-model="searchQuery" 
              placeholder="Tìm tên nhân viên..." 
              class="search-input"
            />
          </div>
          <div class="stats-badge">
            <span>{{ filteredEmployees.length }} nhân viên</span>
          </div>
        </div>

        <!-- Bảng tổng hợp năm (dạng lịch) -->
        <div class="table-container">
          <table class="yearly-table">
            <thead>
              <tr>
                <th rowspan="2" class="employee-col">Nhân viên</th>
                <th colspan="12" class="text-center">Các tháng trong năm</th>
                <th rowspan="2" class="total-col">Tổng công</th>
                <th rowspan="2" class="ot-col">Tổng OT (giờ)</th>
              </tr>
              <tr>
                <th v-for="m in 12" :key="m" class="month-col">T{{ m }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="emp in filteredEmployees" :key="emp.id">
                <td class="employee-cell">
                  <div class="employee-info">
                    <span class="employee-name">{{ emp.name }}</span>
                    <span class="employee-code">{{ emp.code }}</span>
                  </div>
                </td>
                <td v-for="(stat, idx) in emp.monthlyStats" :key="idx" class="stat-cell">
                  <div class="stat-display">
                    <span class="work-days">{{ stat.work }}</span>
                    <span v-if="stat.ot > 0" class="ot-hours">(+{{ stat.ot }}h)</span>
                  </div>
                </td>
                <td class="total-cell">{{ emp.totalWork }}</td>
                <td class="ot-cell">{{ emp.totalOT }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Ghi chú nhẹ nhàng -->
        <div class="table-note">
          <span>Công: số ngày làm việc / OT: giờ tăng ca</span>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import Sidebar from './Sidebar.vue';
import BaseButton from './base/BaseButton.vue';
import '@/CSS/YearlyAttendance.css';
import api from '@/services/api'

const currentYear = new Date().getFullYear();
const selectedYear = ref(currentYear);
const years = [currentYear, currentYear - 1, currentYear - 2];
const searchQuery = ref('');

const loading = ref(false)
const error = ref(null)

const exportCSV = () => {
  console.log('Exporting yearly attendance for', selectedYear.value);
};

const employees = ref([])

function normalizeAttendanceReport(resp) {
  if (!resp) return []
  if (Array.isArray(resp)) return resp
  if (resp.data && Array.isArray(resp.data)) return resp.data
  if (resp.items && Array.isArray(resp.items)) return resp.items
  if (resp.success && Array.isArray(resp.data)) return resp.data
  return resp?.data?.data ?? []
}

async function loadYearly() {
  loading.value = true
  error.value = null
  try {
    // Prefer server-side report endpoint when available
    try {
      const resp = await api.reports.attendance({ year: selectedYear.value })
      const list = normalizeAttendanceReport(resp)
      if (Array.isArray(list) && list.length) {
        employees.value = list.map(item => ({
          id: item.employeeId ?? item.id ?? item.employee ?? null,
          name: item.employeeName ?? item.fullName ?? item.name ?? item.employee ?? 'N/A',
          code: item.employeeCode ?? item.code ?? '',
          monthlyStats: (item.months && item.months.length === 12) ? item.months.map(m => ({ work: m.work ?? 0, ot: m.ot ?? 0 })) : (item.monthlyStats ?? Array.from({length:12}, () => ({ work:0, ot:0 }))),
          totalWork: item.totalWork ?? 0,
          totalOT: item.totalOT ?? 0
        }))
        loading.value = false
        return
      }
    } catch (e) {
      // fall through to per-employee history
    }

    // Fallback: build yearly summary per employee by calling attendance.history per employee
    const empsResp = await api.employees.list()
    const emps = Array.isArray(empsResp) ? empsResp : (empsResp?.data ?? empsResp?.items ?? [])
    const rows = []
    for (const emp of emps) {
      const id = emp.id ?? emp.employeeId
      let months = Array.from({ length: 12 }, () => ({ work: 0, ot: 0 }))
      try {
        const hist = await api.attendance.history(id)
        const records = Array.isArray(hist) ? hist : (hist?.data ?? hist?.items ?? [])
        records.forEach(rec => {
          const d = new Date(rec.date || rec.attendanceDate || rec.checkIn || rec.timestamp)
          if (isNaN(d)) return
          const m = d.getMonth()
          const status = (rec.status ?? rec.state ?? '').toString().toLowerCase()
          if (['present', 'p', 'in', 'checked-in'].includes(status) || status === '') months[m].work += 1
          const ot = Number(rec.otHours ?? rec.ot ?? rec.overtime ?? 0) || 0
          months[m].ot += ot
        })
      } catch (e) {
        // ignore per-employee history errors
      }
      const totalWork = months.reduce((s, x) => s + (x.work || 0), 0)
      const totalOT = months.reduce((s, x) => s + (x.ot || 0), 0)
      rows.push({
        id,
        name: emp.fullName ?? emp.name ?? emp.employee ?? 'N/A',
        code: emp.employeeCode ?? emp.code ?? '',
        monthlyStats: months,
        totalWork,
        totalOT
      })
    }
    employees.value = rows
  } catch (err) {
    error.value = err.message || String(err)
    console.error('Failed loading yearly attendance:', err)
    employees.value = []
  } finally {
    loading.value = false
  }
}

watch(selectedYear, () => loadYearly())

onMounted(() => {
  loadYearly()
})

// Lọc theo tên
const filteredEmployees = computed(() => {
  if (!searchQuery.value) return employees.value;
  const q = searchQuery.value.toLowerCase();
  return employees.value.filter(emp => emp.name.toLowerCase().includes(q));
});
</script>