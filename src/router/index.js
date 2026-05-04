import { createRouter, createWebHistory } from 'vue-router'
import Login from '../components/Login.vue'
import Dashboard from '../components/Dashboard.vue'
import AttendanceDashboard from '../components/AttendanceDashboard.vue'
import Employees from '../components/Employees.vue'
import Recruitment from '../components/Recruitment.vue'
import Setting from '../components/Setting.vue'
import Leave from '../components/Leave.vue'
import Training from '../components/Training.vue'
import YearlyAttendance from '../components/YearlyAttendance.vue'
import WorkReports from '../components/WorkReports.vue'
import Requests from '../components/Requests.vue'
import Feedback from '../components/Feedback.vue'
import Payroll from '../components/Payroll.vue'
import WorkReportsAdmin from '../components/WorkReportsAdmin.vue'
import FeedBackAdmin from '../components/FeedBackAdmin.vue'
import TrainingAdmin from '../components/TrainingAdmin.vue'
import CV from '../components/CV.vue'
import EmployeeLeave from '../components/EmployeeLeave.vue'

// ==================== CÁC COMPONENT MỚI ====================
import BankPartners from '../components/BankPartners.vue'
import CompanyBankAccounts from '../components/CompanyBankAccounts.vue'
import ChatbotWidget from '../components/ChatbotWidget.vue'
import JobPositions from '../components/JobPositions.vue'
import PositionManagement from '../components/PositionManagement.vue'
import PayrollBankTransfers from '../components/PayrollBankTransfers.vue'
import StatusMaster from '../components/StatusMaster.vue'
import FaceAttendance from '../components/FaceAttendance.vue'

const routes = [
  {
    path: '/',
    redirect: '/dashboard'
  },

  {
    path: '/face-attendance',
    name: 'FaceAttendance',
    component: FaceAttendance,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },

  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: { requiresGuest: true }
  },
  // ==================== DASHBOARD & ATTENDANCE ====================
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: Dashboard,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  {
    path: '/attendanceDashboard',
    name: 'AttendanceDashboard',
    component: AttendanceDashboard,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  {
    path: '/attendance-yearly',
    name: 'YearlyAttendance',
    component: YearlyAttendance,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  
  // ==================== EMPLOYEES ====================
  {
    path: '/employees',
    name: 'Employees',
    component: Employees,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  {
    path: '/cv',
    name: 'CV',
    component: CV,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== LEAVE ====================
  {
    path: '/leave',
    name: 'Leave',
    component: Leave,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  {
    path: '/leave-request',
    name: 'EmployeeLeave',
    component: EmployeeLeave,
    meta: { requiresAuth: true, roles: ['employee', 'admin'] }
  },
  
  // ==================== TRAINING ====================
  {
    path: '/training',
    name: 'Training',
    component: Training,
    meta: { requiresAuth: true, roles: ['employee', 'admin'] }
  },
  {
    path: '/training-admin',
    name: 'TrainingAdmin',
    component: TrainingAdmin,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== FEEDBACK ====================
  {
    path: '/feedback',
    name: 'Feedback',
    component: Feedback,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  {
    path: '/feedback-admin',
    name: 'FeedbackAdmin',
    component: FeedBackAdmin,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== RECRUITMENT ====================
  {
    path: '/recruitment',
    name: 'Recruitment',
    component: Recruitment,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== WORK REPORTS ====================
  {
    path: '/work-reports',
    name: 'WorkReports',
    component: WorkReports,
    meta: { requiresAuth: true, roles: ['employee', 'admin'] }
  },
  {
    path: '/work-reports-admin',
    name: 'WorkReportsAdmin',
    component: WorkReportsAdmin,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== REQUESTS ====================
  {
    path: '/requests',
    name: 'Requests',
    component: Requests,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  
  // ==================== PAYROLL ====================
  {
    path: '/payroll',
    name: 'Payroll',
    component: Payroll,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  
  // ==================== SETTINGS ====================
  {
    path: '/setting',
    name: 'Setting',
    component: Setting,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  
  // ==================== BANK PARTNERS (MỚI) ====================
  {
    path: '/bank-partners',
    name: 'BankPartners',
    component: BankPartners,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== COMPANY BANK ACCOUNTS (MỚI) ====================
  {
    path: '/company-bank-accounts',
    name: 'CompanyBankAccounts',
    component: CompanyBankAccounts,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== CHATBOT (MỚI) ====================
  {
    path: '/chatbotwidget',
    name: 'ChatbotWidget',
    component: ChatbotWidget,
    meta: { requiresAuth: true, roles: ['admin', 'employee'] }
  },
  
  // ==================== JOB POSITIONS (MỚI) ====================
  {
    path: '/job-positions',
    name: 'JobPositions',
    component: JobPositions,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== POSITION MANAGEMENT (MỚI) ====================
  {
    path: '/position-management',
    name: 'PositionManagement',
    component: PositionManagement,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== PAYROLL BANK TRANSFERS (MỚI) ====================
  {
    path: '/payroll-bank-transfers',
    name: 'PayrollBankTransfers',
    component: PayrollBankTransfers,
    meta: { requiresAuth: true, roles: ['admin'] }
  },
  
  // ==================== STATUS MASTER (MỚI) ====================
  {
    path: '/status-master',
    name: 'StatusMaster',
    component: StatusMaster,
    meta: { requiresAuth: true, roles: ['admin'] }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Route guard
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const userStr = localStorage.getItem('user')
  const isAuthenticated = !!token
  
  // Xác định role dựa trên username hoặc role field
  let role = 'employee'
  if (userStr) {
    try {
      const user = JSON.parse(userStr)
      // Ưu tiên lấy role từ user object trước
      if (user.role) {
        role = user.role.toLowerCase()
      } else if (user.username === 'admin') {
        role = 'admin'
      } else {
        role = 'employee'
      }
      console.log('Route guard - User:', user.username, 'Role:', role)
    } catch (e) {
      console.error('Error parsing user:', e)
      role = 'employee'
    }
  }

  // Log để debug
  console.log('Navigation:', {
    from: from.path,
    to: to.path,
    role: role,
    isAuthenticated: isAuthenticated,
    requiresAuth: to.meta.requiresAuth,
    requiredRoles: to.meta.roles
  })

  // Case 1: Cần đăng nhập nhưng chưa đăng nhập
  if (to.meta.requiresAuth && !isAuthenticated) {
    console.log('Redirect to login: Requires auth but not authenticated')
    next('/login')
    return
  }

  // Case 2: Trang login nhưng đã đăng nhập
  if (to.meta.requiresGuest && isAuthenticated) {
    const target = role === 'admin' ? '/dashboard' : '/setting'
    console.log('Already logged in, redirect to:', target)
    next(target)
    return
  }

  // Case 3: Kiểm tra role
  if (to.meta.roles && !to.meta.roles.includes(role)) {
    console.log('Access denied - role:', role, 'required:', to.meta.roles)
    
    // Xác định trang đích dựa trên role
    let target = '/setting'
    if (role === 'admin') {
      target = '/dashboard'
    } else if (role === 'employee') {
      target = '/setting'
    }
    
    // Tránh redirect vòng lặp
    if (to.path !== target) {
      console.log('Redirecting to:', target)
      next(target)
    } else {
      console.log('Already on target page, proceeding')
      next()
    }
    return
  }

  // Case 4: Cho phép đi tiếp
  console.log('Access granted to:', to.path)
  next()
})

export default router