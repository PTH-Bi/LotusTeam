import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axiosClient from '@/services/axiosClient'

export const useEmployeeStore = defineStore('employee', () => {
  // State
  const employees = ref([])
  const loading = ref(false)
  const error = ref(null)
  const positionsCache = ref([]) // cached positions from API
  const departmentsCache = ref([])

  // Getters
  const totalEmployees = computed(() => employees.value.length)
  const activeEmployees = computed(() => employees.value.filter(e => e.status === 'Đang làm việc').length)
  const onLeaveEmployees = computed(() => employees.value.filter(e => e.status === 'Nghỉ phép').length)

  // Thống kê theo phòng ban
  const departmentStats = computed(() => {
    const stats = {}
    employees.value.forEach(emp => {
      if (!stats[emp.department]) {
        stats[emp.department] = { total: 0, active: 0 }
      }
      stats[emp.department].total++
      if (emp.status === 'Đang làm việc') {
        stats[emp.department].active++
      }
    })
    return stats
  })

  // Actions
  async function fetchEmployees() {
    loading.value = true
    error.value = null
    try {
      console.log('Fetching employees from API...')
      // Ensure positions cache is loaded to map names <-> ids
      await loadPositions()
      const response = await axiosClient.get('/Employees')
      console.log('API response received:', response)

      // Support several possible response formats:
      // 1) Direct array: [{...}, {...}]
      // 2) Wrapper: { success: true, data: [...] }
      // 3) Wrapper with items: { items: [...] }
      let list = []
      if (Array.isArray(response)) {
        list = response
      } else if (response && Array.isArray(response.data)) {
        list = response.data
      } else if (response && Array.isArray(response.items)) {
        list = response.items
      } else if (response && response.success && Array.isArray(response.data)) {
        list = response.data
      } else {
        // Unknown format — log and fallback
        console.warn('Unexpected /Employees response format, using empty list')
        list = []
      }

      const employeeData = list.map(emp => ({
        id: emp.employeeId ?? emp.id ?? emp.employeeCode ?? null,
        employeeCode: emp.employeeCode ?? emp.code ?? emp.employeeId ?? '',
        name: emp.fullName ?? emp.name ?? emp.displayName ?? '',
        email: emp.email ?? '',
        phone: emp.phone ?? emp.mobile ?? '',
        department: emp.departmentName ?? emp.department ?? 'Chưa phân loại',
        // keep raw position name and id if provided
        position: emp.positionName ?? emp.position ?? 'Chưa xác định',
        positionId: emp.positionId ?? emp.position_id ?? emp.positionId ?? null,
        status: getStatusText(emp.status ?? emp.statusValue ?? 1),
        startDate: emp.hireDate ?? emp.startDate ?? '',
        baseSalary: emp.baseSalary ?? 0,
        avatar: getInitials(emp.fullName ?? emp.name ?? ''),
        gender: (emp.genderId ?? (emp.gender === 'male' ? 1 : emp.gender === 'female' ? 2 : null)) === 1 ? 'male' : 'female',
        birthDate: emp.dateOfBirth ?? emp.birthDate ?? '',
        address: emp.address ?? ''
      }))

      employees.value = employeeData
      // If position names are missing but we have positionId and positionsCache, fill names
      if (positionsCache.value.length > 0 || departmentsCache.value.length > 0) {
        employees.value = employees.value.map(emp => {
          if ((!emp.position || emp.position === 'Chưa xác định' || emp.position === '') && emp.positionId) {
            const found = positionsCache.value.find(p => p.id == emp.positionId)
            if (found) emp.position = found.name
          }
          // fill department name from departmentsCache if available and department id present
          if ((!emp.department || emp.department === 'Chưa phân loại' || emp.department === '') && emp.departmentId) {
            const f = departmentsCache.value.find(d => d.id == emp.departmentId)
            if (f) emp.department = f.name
          }
          return emp
        })
      }

      console.log(`Loaded ${employees.value.length} employees`)
    } catch (err) {
      // If axios error object preserved, use its friendly message when available
      error.value = err.userMessage || err.message || 'Đã xảy ra lỗi khi lấy danh sách nhân viên'
      console.error('Error fetching employees:', {
        message: err.message,
        userMessage: err.userMessage,
        status: err.response?.status,
        responseData: err.response?.data,
      })
      // On fetch failure, do not populate with local hard-coded data. Keep list empty and surface error.
      employees.value = []
    } finally {
      loading.value = false
    }
  }

  // Load positions list from API into cache for name<->id resolution
  async function loadPositions() {
    try {
      if (positionsCache.value.length > 0) {
        console.log('📦 Positions already cached:', positionsCache.value)
        return positionsCache.value
      }
      console.log('🔄 Loading positions from /Positions...')
      const resp = await axiosClient.get('/Positions')
      console.log('📥 Positions API response:', resp)
      
      let list = Array.isArray(resp) ? resp : (resp?.data ?? resp?.items ?? [])
      
      if (list.length > 0) {
        positionsCache.value = list.map(p => ({ 
          id: p.id ?? p.positionId ?? p.positionId, 
          name: p.name ?? p.positionName ?? p.title ?? p.displayName ?? `Position ${p.id || '?'}`
        }))
        console.log('✅ Loaded positions:', positionsCache.value)
      } else {
        console.warn('⚠️ No positions returned from API, using fallback defaults')
        positionsCache.value = [
          { id: 1, name: 'Nhân viên' },
          { id: 2, name: 'Trưởng phòng' },
          { id: 3, name: 'Giám đốc' },
          { id: 4, name: 'Trưởng dự án' }
        ]
      }

      return positionsCache.value || []
    } catch (err) {
      console.warn('❌ Failed to load positions list:', err?.message || err)
      console.error('   Error details:', err)
      if (positionsCache.value.length === 0) {
        console.log('   Using fallback defaults')
        positionsCache.value = [
          { id: 1, name: 'Nhân viên' },
          { id: 2, name: 'Trưởng phòng' },
          { id: 3, name: 'Giám đốc' },
          { id: 4, name: 'Trưởng dự án' }
        ]
      }
      return positionsCache.value
    }
  }

  async function loadDepartments() {
    try {
      if (departmentsCache.value.length > 0) {
        console.log('📦 Departments already cached:', departmentsCache.value)
        return departmentsCache.value
      }
      console.log('🔄 Loading departments from /Departments...')
      const resp = await axiosClient.get('/Departments')
      console.log('📥 Departments API response:', resp)
      
      let list = Array.isArray(resp) ? resp : (resp?.data ?? resp?.items ?? [])
      
      if (list.length > 0) {
        departmentsCache.value = list.map(d => ({ 
          id: d.id ?? d.departmentId ?? d.departmentId, 
          name: d.name ?? d.departmentName ?? d.title ?? d.displayName ?? `Department ${d.id || '?'}`
        }))
        console.log('✅ Loaded departments:', departmentsCache.value)
      } else {
        console.warn('⚠️ No departments returned from API, using fallback defaults')
        departmentsCache.value = [
          { id: 1, name: 'Nhân sự' },
          { id: 2, name: 'Kế toán' },
          { id: 3, name: 'Kinh doanh' },
          { id: 4, name: 'Công nghệ' },
          { id: 5, name: 'Hỗ trợ' }
        ]
      }

      return departmentsCache.value || []
    } catch (err) {
      console.warn('❌ Failed to load departments list:', err?.message || err)
      console.error('   Error details:', err)
      if (departmentsCache.value.length === 0) {
        console.log('   Using fallback defaults')
        departmentsCache.value = [
          { id: 1, name: 'Nhân sự' },
          { id: 2, name: 'Kế toán' },
          { id: 3, name: 'Kinh doanh' },
          { id: 4, name: 'Công nghệ' },
          { id: 5, name: 'Hỗ trợ' }
        ]
      }
      return departmentsCache.value
    }
  }

  async function addEmployee(employeeData) {
    loading.value = true
    error.value = null
    try {
      console.log('Adding employee with data:', employeeData)

      // Generate employee code if not provided
      let employeeCode = employeeData.employeeCode?.trim()
      if (!employeeCode) {
        employeeCode = generateEmployeeCode()
      }

      // Validate required fields
      if (!employeeData.name?.trim()) {
        throw new Error('Họ và tên không được để trống')
      }
      if (!employeeData.email?.trim()) {
        throw new Error('Email không được để trống')
      }
      if (!employeeData.department?.trim()) {
        throw new Error('Phòng ban không được để trống')
      }
      if (!employeeData.position?.trim()) {
        throw new Error('Chức vụ không được để trống')
      }
      if (!employeeData.startDate) {
        throw new Error('Ngày vào làm không được để trống')
      }
      if (!employeeData.birthDate) {
        throw new Error('Ngày sinh không được để trống')
      }

      // Check for duplicate employee code (frontend check)
      const duplicateCode = employees.value.find(emp => emp.employeeCode === employeeCode)
      if (duplicateCode) {
        throw new Error(`Mã nhân viên "${employeeCode}" đã tồn tại`)
      }

      // Check for duplicate email (frontend check)
      const duplicateEmail = employees.value.find(emp => emp.email === employeeData.email)
      if (duplicateEmail) {
        throw new Error(`Email "${employeeData.email}" đã được sử dụng`)
      }

      // Định dạng ngày đúng chuẩn
      const formatDateForAPI = (dateString) => {
        if (!dateString) return null
        const date = new Date(dateString)
        if (isNaN(date.getTime())) return null
        return date.toISOString()
      }

      // Xử lý genderId: male -> 1, female -> 2
      let genderId = null
      if (employeeData.gender === 'male') {
        genderId = 1
      } else if (employeeData.gender === 'female') {
        genderId = 2
      }

      // Chuẩn bị payload cho backend
      const payload = {
        employeeCode: employeeCode,
        fullName: employeeData.name.trim(),
        email: employeeData.email.trim(),
        phone: employeeData.phone?.trim() || null,
        genderId,
        dateOfBirth: formatDateForAPI(employeeData.birthDate),
        departmentId: getDepartmentId(employeeData.department),
        positionId: getPositionId(employeeData.position),
        hireDate: formatDateForAPI(employeeData.startDate),
        baseSalary: employeeData.baseSalary ? parseFloat(employeeData.baseSalary) : 0,
        address: employeeData.address?.trim() || null,
        maritalStatus: employeeData.maritalStatus ?? null,
        identityNumber: employeeData.identityNumber ?? null,
        bankAccount: employeeData.bankAccount ?? null,
        taxCode: employeeData.taxCode ?? null,
        emergencyContactName: employeeData.emergencyContactName ?? null,
        emergencyContactPhone: employeeData.emergencyContactPhone ?? null,
        initialContract: employeeData.initialContract ? {
          contractTypeId: employeeData.initialContract.contractTypeId ?? null,
          startDate: formatDateForAPI(employeeData.initialContract.startDate),
          endDate: formatDateForAPI(employeeData.initialContract.endDate),
          salary: employeeData.initialContract.salary ?? null,
          signedDate: formatDateForAPI(employeeData.initialContract.signedDate)
        } : null,
        createUserAccount: !!employeeData.createUserAccount,
        username: employeeData.username ?? null,
        password: employeeData.password ?? null
      }

      console.log('Sending payload to API:', JSON.stringify(payload, null, 2))

      // Gọi API
      let response
      try {
        response = await axiosClient.post('/Employees', payload)
      } catch (apiError) {
        console.warn('API add employee failed:', apiError)
        // Do not create local fallback employee. Surface the network error to caller.
        throw new Error('Không thể kết nối đến server. Thao tác không thực hiện được.')
      }

      console.log('API Response:', response)

      let responseData
      if (response && response.success && response.data) {
        responseData = response.data
      } else if (response && response.employeeId) {
        responseData = response
      } else if (response && response.data && response.data.employeeId) {
        responseData = response.data
      }

      if (!responseData) {
        throw new Error(response?.message || 'Không thể thêm nhân viên')
      }

      console.log('Employee created successfully:', responseData)

      const newEmployee = {
        id: responseData.employeeId ?? responseData.id ?? `local-${Date.now()}`,
        employeeCode: responseData.employeeCode ?? employeeCode,
        name: responseData.fullName ?? employeeData.name,
        email: responseData.email ?? employeeData.email,
        phone: (responseData.phone ?? employeeData.phone) || '',
        department: responseData.departmentName ?? employeeData.department,
        position: responseData.positionName ?? employeeData.position,
        status: getStatusText(responseData.status ?? 1),
        startDate: responseData.hireDate ? formatDate(responseData.hireDate) : employeeData.startDate,
        baseSalary: responseData.baseSalary ?? payload.baseSalary,
        avatar: getInitials(responseData.fullName ?? employeeData.name),
        gender: (responseData.genderId === 1 ? 'male' : (responseData.genderId === 2 ? 'female' : employeeData.gender || 'male')),
        birthDate: responseData.dateOfBirth ? formatDate(responseData.dateOfBirth) : employeeData.birthDate,
        address: responseData.address ?? employeeData.address ?? ''
      }

      employees.value.push(newEmployee)
      return newEmployee

    } catch (err) {
      error.value = err.message
      console.error('Error adding employee:', err)

      if (err.response) {
        console.error('Response error details:', {
          status: err.response.status,
          data: err.response.data,
          headers: err.response.headers
        })

        let errorMessage = `Lỗi ${err.response.status}: `
        if (err.response.data) {
          if (typeof err.response.data === 'string') {
            errorMessage += err.response.data
          } else if (err.response.data.message) {
            errorMessage += err.response.data.message
          } else if (err.response.data.errors) {
            const errors = Object.values(err.response.data.errors).flat()
            errorMessage += errors.join(', ')
          } else if (err.response.data.error) {
            errorMessage += err.response.data.error
          } else {
            errorMessage += JSON.stringify(err.response.data)
          }
        } else {
          errorMessage += 'Không thể thêm nhân viên'
        }

        throw new Error(errorMessage)
      } else if (err.request) {
        console.error('No response received. Network error:', err.request)
        throw new Error('Không thể kết nối đến server. Vui lòng kiểm tra:\n1. Backend server đang chạy\n2. URL API đúng\n3. CORS configuration\n4. Network tab trong Developer Tools')
      } else {
        throw new Error('Lỗi khi xử lý yêu cầu: ' + err.message)
      }
    } finally {
      loading.value = false
    }
  }

// Local persistence and pending-sync removed. Store uses API-only data.

function formatDate(dateString) {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    if (isNaN(date.getTime())) {
      return ''
    }
    return date.toISOString().split('T')[0]
  } catch {
    return ''
  }
}

  async function updateEmployee(id, updatedData) {
    loading.value = true
    error.value = null
    try {
      // Check for duplicate email (excluding current employee)
      const duplicateEmail = employees.value.find(emp => 
        emp.email === updatedData.email && emp.id !== id
      )
      if (duplicateEmail) {
        throw new Error(`Email "${updatedData.email}" đã được sử dụng`)
      }

      // Prepare payload
      const payload = {
        fullName: updatedData.name.trim(),
        email: updatedData.email.trim(),
        phone: updatedData.phone?.trim() || null,
        genderId: updatedData.gender === 'male' ? 1 : 2,
        dateOfBirth: updatedData.birthDate ? formatDateForAPI(updatedData.birthDate) : null,
        address: updatedData.address || null,
        departmentId: getDepartmentId(updatedData.department),
        positionId: getPositionId(updatedData.position),
        baseSalary: updatedData.baseSalary ? Number(updatedData.baseSalary) : 0,
        status: getStatusValue(updatedData.status),
        maritalStatus: updatedData.maritalStatus ?? null,
        identityNumber: updatedData.identityNumber ?? null,
        bankAccount: updatedData.bankAccount ?? null,
        taxCode: updatedData.taxCode ?? null,
        emergencyContactName: updatedData.emergencyContactName ?? null,
        emergencyContactPhone: updatedData.emergencyContactPhone ?? null,
        initialContract: updatedData.initialContract ? {
          contractTypeId: updatedData.initialContract.contractTypeId ?? null,
          startDate: formatDateForAPI(updatedData.initialContract.startDate),
          endDate: formatDateForAPI(updatedData.initialContract.endDate),
          salary: updatedData.initialContract.salary ?? null,
          signedDate: formatDateForAPI(updatedData.initialContract.signedDate)
        } : null
      }

      console.log('Updating employee with payload:', payload)

      // Call API
      const response = await axiosClient.put(`/Employees/${id}`, payload)
      console.log('Update response:', response)

      if (response && response.success) {
        // Update local state
        const index = employees.value.findIndex(e => e.id === id)
        if (index !== -1) {
          employees.value[index] = { 
            ...employees.value[index], 
            ...updatedData,
            avatar: getInitials(updatedData.name)
          }
        }
        return true
      } else {
        throw new Error(response?.message || 'Không thể cập nhật nhân viên')
      }
    } catch (err) {
      error.value = err.message
      console.error('Error updating employee:', err)
      
      if (err.response?.status === 404) {
        throw new Error('Không tìm thấy nhân viên để cập nhật')
      }
      
      throw err
    } finally {
      loading.value = false
    }
  }

  async function removeEmployee(id) {
    loading.value = true
    error.value = null
    try {
      console.log('Deleting employee ID:', id)
      
      // Call API - Sử dụng endpoint terminate thay vì delete
      await axiosClient.put(`/Employees/${id}/terminate`, {
        terminationDate: new Date().toISOString(),
        terminationType: 'Resignation',
        reason: 'Xóa nhân viên',
        notes: 'Xóa từ hệ thống'
      })

      // Update local state - set status to terminated
      const index = employees.value.findIndex(e => e.id === id)
      if (index !== -1) {
        employees.value[index].status = 'Đã nghỉ việc'
        // Hoặc remove khỏi list nếu muốn
        // employees.value.splice(index, 1)
      }

      return true
    } catch (err) {
      error.value = err.message
      console.error('Error deleting employee:', err)
      
      if (err.response?.status === 404) {
        throw new Error('Không tìm thấy nhân viên để xóa')
      }
      
      return false
    } finally {
      loading.value = false
    }
  }

  function getEmployeeById(id) {
    return employees.value.find(e => e.id === id)
  }

  function filterEmployees(filters) {
    return employees.value.filter(emp => {
      if (filters.department && emp.department !== filters.department) return false
      if (filters.status && emp.status !== filters.status) return false
      if (filters.searchTerm && !emp.name.toLowerCase().includes(filters.searchTerm.toLowerCase())) return false
      return true
    })
  }

  // Helper functions
  function generateEmployeeCode() {
    const lastEmployee = employees.value.length > 0
      ? employees.value[employees.value.length - 1]
      : null

    let nextNumber = 1
    
    if (lastEmployee && lastEmployee.employeeCode) {
      const match = lastEmployee.employeeCode.match(/(\d+)$/)
      if (match) {
        nextNumber = parseInt(match[1]) + 1
      } else {
        nextNumber = employees.value.length + 1
      }
    }

    return `NV-${String(nextNumber).padStart(3, '0')}`
  }

  function getInitials(name) {
    if (!name) return 'NV'
    return name.split(' ')
      .map(n => n[0])
      .join('')
      .slice(0, 2)
      .toUpperCase()
  }

  function formatDate(dateString) {
    if (!dateString) return ''
    const date = new Date(dateString)
    return date.toISOString().split('T')[0]
  }

  function formatDateForAPI(dateString) {
    if (!dateString) return null
    const date = new Date(dateString)
    if (isNaN(date.getTime())) {
      return null
    }
    return date.toISOString()
  }

  function getStatusText(statusValue) {
    switch (statusValue) {
      case 0: return 'Đã nghỉ việc'
      case 1: return 'Đang làm việc'
      case 2: return 'Nghỉ phép'
      case 3: return 'Tạm ngừng'
      default: return 'Đang làm việc'
    }
  }

  function getStatusValue(statusText) {
    switch (statusText) {
      case 'Đang làm việc': return 1
      case 'Tạm dừng': return 3
      case 'Nghỉ việc': return 0
      case 'Nghỉ phép': return 2
      default: return 1
    }
  }

  function getDepartmentId(departmentName) {
    if (!departmentName) return 1
    const asNumber = Number(departmentName)
    if (!isNaN(asNumber) && asNumber > 0) return asNumber

    const name = String(departmentName).trim().toLowerCase()
    const found = departmentsCache.value.find(d => (d.name || '').toLowerCase() === name)
    if (found) return found.id

    const departmentMap = {
      'phòng kỹ thuật': 1,
      'phòng kế toán': 2,
      'phòng marketing': 3,
      'phòng nhân sự': 4,
      'ban giám đốc': 5
    }

    const deptId = departmentMap[name]
    if (!deptId) {
      console.warn(`Không tìm thấy ID cho phòng ban: ${departmentName}, sử dụng mặc định 1`)
    }
    return deptId || 1
}

function getDepartmentNameById(departmentId) {
  const found = departmentsCache.value.find(d => d.id == departmentId)
  return found ? found.name : null
}

function getPositionNameById(positionId) {
  const found = positionsCache.value.find(p => p.id == positionId)
  return found ? found.name : null
}

function getPositionId(positionName) {
  // If already numeric id, return it
  if (!positionName) return 9
  const asNumber = Number(positionName)
  if (!isNaN(asNumber) && asNumber > 0) return asNumber

  // Try positions cache (case-insensitive match)
  const name = String(positionName).trim().toLowerCase()
  const found = positionsCache.value.find(p => (p.name || '').toLowerCase() === name)
  if (found) return found.id

  // Fallback: try some common mappings
  const positionMap = {
    'nhân viên backend': 1,
    'backend developer': 1,
    'nhân viên frontend': 2,
    'frontend developer': 2,
    'full stack': 3,
    'fullstack developer': 3,
    'trưởng phòng': 4,
    'kế toán viên': 9,
    'marketing executive': 5,
    'hr specialist': 6,
    'giám đốc': 7,
    'nhân viên': 9,
    'hr executive': 6
  }
  const mapped = positionMap[name]
  if (!mapped) {
    console.warn(`Không tìm thấy ID cho chức vụ: ${positionName}, sử dụng mặc định 9`)
  }
  return mapped || 9
}

function getStatusText(statusValue) {
  // Map theo backend: 0: đã nghỉ, 1: đang làm, 2: nghỉ phép, 3: tạm ngừng
  switch (statusValue) {
    case 0: return 'Đã nghỉ việc'
    case 1: return 'Đang làm việc'
    case 2: return 'Nghỉ phép'
    case 3: return 'Tạm ngừng'
    default: return 'Đang làm việc'
  }
}

// Cập nhật mapping ngược lại
function getStatusValue(statusText) {
  switch (statusText) {
    case 'Đang làm việc': return 1
    case 'Tạm ngừng': return 3
    case 'Tạm dừng': return 3 // Giữ tương thích với frontend cũ
    case 'Nghỉ việc': return 0
    case 'Đã nghỉ việc': return 0
    case 'Nghỉ phép': return 2
    default: return 1
  }
}

  return {
    employees,
    loading,
    error,
    totalEmployees,
    activeEmployees,
    onLeaveEmployees,
    departmentStats,
    fetchEmployees,
    addEmployee,
    updateEmployee,
    removeEmployee,
    getEmployeeById,
    filterEmployees,
    // expose lists + loaders
    positionsCache,
    departmentsCache,
    loadPositions,
    loadDepartments
  }
})