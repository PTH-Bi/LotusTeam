<template>
  <div class="attendance-page">
    <Sidebar activeItem="Chấm công" />

    <main class="attendance-main">
      <header class="attendance-header">
        <div class="header-left">
          <h1 class="attendance-title">Chấm công hàng ngày</h1>
          <p class="page-subtitle">{{ currentDateFormatted }}</p>
        </div>
        <div class="header-right">
          <div class="date-display">
            Tháng {{ new Date().getMonth() + 1 }} / {{ new Date().getFullYear() }}
          </div>
          
          <button @click="refreshData" :disabled="loading">
            {{ loading ? 'Đang tải...' : 'Làm mới' }}
          </button>
        </div>
      </header>

      <div class="attendance-content">
        <div class="summary-cards">
          <div class="summary-card">
            <div class="summary-info">
              <span class="summary-label">Nhân viên có mặt</span>
              <div class="summary-value-row">
                <span class="summary-value success">{{ presentCount }}</span>
                <span class="summary-total">/ {{ attendanceRecords.length }}</span>
              </div>
            </div>
          </div>

          <div class="summary-card">
            <div class="summary-info">
              <span class="summary-label">Vắng mặt</span>
              <div class="summary-value-row">
                <span class="summary-value danger">{{ absentCount }}</span>
                <span class="summary-total">/ {{ attendanceRecords.length }}</span>
              </div>
            </div>
          </div>

          <div class="summary-card">
            <div class="summary-info">
              <span class="summary-label">Đúng giờ</span>
              <div class="summary-value-row">
                <span class="summary-value info">98%</span>
              </div>
            </div>
          </div>
        </div>

        <div class="attendance-table-card">
          <div class="table-header">
            <h2 class="section-title">Danh sách điểm danh hôm nay</h2>
            <div class="table-search">
              <input 
                v-model="searchQuery" 
                placeholder="Tìm tên nhân viên..."
                type="text"
              >
            </div>
          </div>

          <table>
            <thead>
              <tr>
                <th>Nhân viên</th>
                <th>Phòng ban</th>
                <th style="width: 120px; text-align: center;">Trạng thái</th>
                <th v-if="!isAdmin" style="width: 100px; text-align: center;">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in filteredRecords" :key="row.id">
                <td>
                  <div class="user-cell">
                    <div class="avatar-sm">{{ getInitials(row.name) }}</div>
                    <span class="user-name">{{ row.name }}</span>
                  </div>
                </td>
                <td>{{ row.department }}</td>
                <td style="text-align: center;">
                  <span 
                    class="status-badge" 
                    :class="{
                      'status-present': row.status === 'present',
                      'status-absent': row.status === 'absent',
                      'status-none': row.status === 'none'
                    }"
                  >
                    {{ getStatusText(row.status) }}
                  </span>
                </td>
                <td v-if="!isAdmin" style="text-align: center;">
                  <div class="action-buttons">
                    <button 
                      class="btn-present" 
                      :class="{ active: row.status === 'present' }"
                      @click="setAttendance(row.id, 'present')"
                      :disabled="loadingAttendance"
                    >
                      Có mặt
                    </button>
                    <button 
                      class="btn-absent" 
                      :class="{ active: row.status === 'absent' }"
                      @click="setAttendance(row.id, 'absent')"
                      :disabled="loadingAttendance"
                    >
                      Vắng
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="filteredRecords.length === 0">
                <td :colspan="isAdmin ? 3 : 4" style="text-align: center; padding: 40px; color: #999;">
                  Không tìm thấy nhân viên nào
                </td>
              </tr>
            </tbody>
          </table>
          

        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useEmployeeStore } from '../stores/employeeStore';
import { useAuthStore } from '../stores/authStore'
import Sidebar from './Sidebar.vue'
import '@/CSS/AttendanceDashboard.css'
import api from '@/services/api'

const employeeStore = useEmployeeStore();
const authStore = useAuthStore();
const loading = ref(false);
const searchQuery = ref('')
const attendanceStatus = ref({});
const loadingAttendance = ref(false)
const attendanceError = ref(null)

// Kiểm tra role admin
const isAdmin = computed(() => authStore.user?.role === 'admin');

const currentDateFormatted = computed(() => {
  const options = { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' };
  return new Date().toLocaleDateString('vi-VN', options);
});

const attendanceRecords = computed(() => {
  return employeeStore.employees.map(emp => ({
    id: emp.id,
    name: emp.name || emp.fullName || 'N/A',
    department: emp.department || 'N/A',
    status: attendanceStatus.value[emp.id] || 'none'
  }));
});

const filteredRecords = computed(() => {
  if (!searchQuery.value) return attendanceRecords.value;
  const q = searchQuery.value.toLowerCase();
  return attendanceRecords.value.filter(r => r.name.toLowerCase().includes(q));
});

const presentCount = computed(() => attendanceRecords.value.filter(r => r.status === 'present').length);
const absentCount = computed(() => attendanceRecords.value.filter(r => r.status === 'absent').length);

// Hàm lấy text trạng thái
const getStatusText = (status) => {
  switch(status) {
    case 'present': return 'Có mặt';
    case 'absent': return 'Vắng mặt';
    default: return 'Chưa điểm danh';
  }
};

const setAttendance = async (id, status) => {
  // Chỉ cho phép nếu không phải admin
  if (isAdmin.value) {
    console.warn('Admin không được phép chỉnh sửa điểm danh');
    return;
  }
  
  const newStatus = attendanceStatus.value[id] === status ? 'none' : status
  attendanceStatus.value = { ...attendanceStatus.value, [id]: newStatus }
  try {
    await api.attendance.manual({ employeeId: id, status: newStatus })
  } catch (e) {
    attendanceStatus.value = { ...attendanceStatus.value, [id]: attendanceStatus.value[id] === newStatus ? 'none' : attendanceStatus.value[id] }
    console.warn('Failed to update attendance', e)
  }
};

function normalizeAttendanceResponse(resp) {
  if (!resp) return []
  if (Array.isArray(resp)) return resp
  if (resp.data && Array.isArray(resp.data)) return resp.data
  if (resp.items && Array.isArray(resp.items)) return resp.items
  if (resp.success && Array.isArray(resp.data)) return resp.data
  return resp?.data?.data ?? []
}

async function loadAttendance() {
  loadingAttendance.value = true
  attendanceError.value = null
  try {
    const today = new Date().toISOString().split('T')[0]
    if (authStore.user?.role === 'admin' && authStore.user?.departmentId) {
      const resp = await api.attendance.department(authStore.user.departmentId)
      const list = normalizeAttendanceResponse(resp)
      const map = {}
      list.forEach(item => {
        const id = item.employeeId ?? item.id ?? item.employee ?? item.employeeCode ?? null
        const status = (item.status ?? item.state ?? item.attendanceStatus ?? '').toString().toLowerCase()
        if (id) map[id] = status
      })
      attendanceStatus.value = { ...attendanceStatus.value, ...map }
    } else if (authStore.user?.id) {
      const resp = await api.attendance.my(authStore.user.id)
      const list = normalizeAttendanceResponse(resp)
      list.forEach(it => {
        const id = it.id ?? it.employeeId ?? authStore.user.id
        attendanceStatus.value[id] = (it.status ?? it.state ?? 'present')
      })
    } else {
      const map = {}
      await Promise.all(employeeStore.employees.map(async emp => {
        try {
          const r = await api.attendance.raw(emp.id)
          const arr = normalizeAttendanceResponse(r)
          if (arr && arr.length) {
            const latest = arr[0]
            map[emp.id] = (latest.status ?? latest.state ?? 'present').toString().toLowerCase()
          }
        } catch (e) {}
      }))
      attendanceStatus.value = { ...attendanceStatus.value, ...map }
    }
  } catch (err) {
    attendanceError.value = err.message || String(err)
    console.warn('Could not load attendance list:', err)
  } finally {
    loadingAttendance.value = false
  }
}

const refreshData = async () => {
  loading.value = true;
  try {
    await employeeStore.fetchEmployees()
    await loadAttendance()
  } finally {
    loading.value = false
  }
};

const getInitials = (name) => {
  if (!name) return 'NV';
  return name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();
};

onMounted(async () => {
  if (authStore.user?.role === 'admin') {
    await employeeStore.fetchEmployees();
    await loadAttendance()
    console.log('Admin mode: View only');
  } else {
    console.log('Employee mode: Can edit attendance');
    try {
      const me = authStore.user?.id
      if (me) {
        const resp = await api.attendance.my(me)
        const list = normalizeAttendanceResponse(resp)
        if (Array.isArray(list)) {
          list.forEach(it => {
            attendanceStatus.value[it.id ?? it.employeeId ?? me] = (it.status ?? it.state ?? 'present')
          })
        }
      }
    } catch (err) {
      console.warn('Could not load personal attendance:', err)
    }
  }
});
</script>