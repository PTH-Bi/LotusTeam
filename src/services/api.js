// api.js - Hoàn chỉnh đồng bộ với tất cả Controllers C#

import axiosClient from './axiosClient'

// ==================== ANNOUNCEMENT ====================
const announcement = {
  list: (params) => axiosClient.get('/Announcements', { params }),
  create: (data) => axiosClient.post('/Announcements', data),
  get: (id) => axiosClient.get(`/Announcements/${id}`),
  update: (id, data) => axiosClient.put(`/Announcements/${id}`, data),
  remove: (id) => axiosClient.delete(`/Announcements/${id}`)
}

// ==================== ASSETS ====================
const assets = {
  list: (params) => axiosClient.get('/Assets', { params }),
  create: (data) => axiosClient.post('/Assets', data),
  get: (id) => axiosClient.get(`/Assets/${id}`),
  update: (id, data) => axiosClient.put(`/Assets/${id}`, data),
  remove: (id) => axiosClient.delete(`/Assets/${id}`),
  assign: (data) => axiosClient.post('/Assets/assign', data),
  revoke: (id) => axiosClient.post(`/Assets/revoke/${id}`),
  history: (assetId) => axiosClient.get(`/Assets/history/${assetId}`)
}

// ==================== ATTENDANCE ====================
const attendance = {
  my: (employeeId) => axiosClient.get(`/Attendance/my/${employeeId}`),
  department: (departmentId) => axiosClient.get(`/Attendance/department/${departmentId}`),
  manual: (data) => axiosClient.post('/Attendance/manual', data),
  raw: (employeeId) => axiosClient.get(`/Attendance/raw/${employeeId}`),
  adjust: (attendanceId, data) => axiosClient.put(`/Attendance/adjust/${attendanceId}`, data),
  overtimeList: (params) => axiosClient.get('/Attendance/overtime', { params }),
  createOvertime: (data) => axiosClient.post('/Attendance/overtime', data),
  approveOvertime: (id, data) => axiosClient.post(`/Attendance/overtime/approve/${id}`, data),
  generateQr: () => axiosClient.post('/Attendance/generate-qr'),
  activeQr: () => axiosClient.get('/Attendance/active-qr'),
  scan: (data) => axiosClient.post('/Attendance/scan', data),
  // Face Attendance
  faceRegister: (data) => axiosClient.post('/FaceAttendance/register', data),
  faceVerify: (data) => axiosClient.post('/FaceAttendance/verify', data),
  faceCheckin: (data) => axiosClient.post('/FaceAttendance/checkin', data)
}

// ==================== AUTH ====================
const auth = {
  login: (data) => axiosClient.post('/Auth/login', data),
  logout: () => axiosClient.post('/Auth/logout'),
  changePassword: (data) => axiosClient.post('/Auth/change-password', data),
  profile: () => axiosClient.get('/Auth/profile'),
  updateProfile: (data) => axiosClient.put('/Auth/profile', data)
}

// ==================== BANK PARTNERS ====================
const bankPartners = {
  list: (params) => axiosClient.get('/BankPartners', { params }),
  create: (data) => axiosClient.post('/BankPartners', data),
  get: (id) => axiosClient.get(`/BankPartners/${id}`),
  update: (id, data) => axiosClient.put(`/BankPartners/${id}`, data),
  remove: (id) => axiosClient.delete(`/BankPartners/${id}`)
}

// ==================== CHATBOT ====================
const chatbot = {
  send: (message) => axiosClient.post('/Chatbot/send', { message }),
  history: (params) => axiosClient.get('/Chatbot/history', { params }),
  getSuggestions: () => axiosClient.get('/Chatbot/suggestions'),
  clearHistory: () => axiosClient.delete('/Chatbot/history')
}

// ==================== COMPANY BANK ACCOUNTS ====================
const companyBankAccounts = {
  list: (params) => axiosClient.get('/CompanyBankAccounts', { params }),
  create: (data) => axiosClient.post('/CompanyBankAccounts', data),
  get: (id) => axiosClient.get(`/CompanyBankAccounts/${id}`),
  update: (id, data) => axiosClient.put(`/CompanyBankAccounts/${id}`, data),
  remove: (id) => axiosClient.delete(`/CompanyBankAccounts/${id}`),
  setDefault: (id) => axiosClient.post(`/CompanyBankAccounts/${id}/set-default`)
}

// ==================== COMPANY INFO ====================
const companyInfo = {
  list: (params) => axiosClient.get('/CompanyInfo', { params }),
  create: (data) => axiosClient.post('/CompanyInfo', data),
  update: (data) => axiosClient.put('/CompanyInfo', data),
  get: (id) => axiosClient.get(`/CompanyInfo/${id}`),
  remove: (id) => axiosClient.delete(`/CompanyInfo/${id}`)
}

// ==================== CONTRACTS ====================
const contracts = {
  list: (params) => axiosClient.get('/Contracts', { params }),
  create: (data) => axiosClient.post('/Contracts', data),
  get: (id) => axiosClient.get(`/Contracts/${id}`),
  update: (id, data) => axiosClient.put(`/Contracts/${id}`, data),
  extend: (id, data) => axiosClient.put(`/Contracts/${id}/extend`, data),
  remove: (id) => axiosClient.delete(`/Contracts/${id}`),
  employeeBenefits: (employeeId) => axiosClient.get(`/Contracts/employee/${employeeId}/benefits`),
  addEmployeeBenefit: (employeeId, data) => axiosClient.post(`/Contracts/employee/${employeeId}/benefits`, data),
  updateBenefit: (benefitId, data) => axiosClient.put(`/Contracts/benefits/${benefitId}`, data),
  deleteBenefit: (benefitId) => axiosClient.delete(`/Contracts/benefits/${benefitId}`)
}

// ==================== CONTRACT TYPES ====================
const contractTypes = {
  list: (params) => axiosClient.get('/ContractTypes', { params }),
  create: (data) => axiosClient.post('/ContractTypes', data),
  get: (id) => axiosClient.get(`/ContractTypes/${id}`),
  update: (id, data) => axiosClient.put(`/ContractTypes/${id}`, data),
  remove: (id) => axiosClient.delete(`/ContractTypes/${id}`)
}

// ==================== DASHBOARD ====================
const dashboard = {
  overview: () => axiosClient.get('/Dashboard/overview'),
  hr: () => axiosClient.get('/Dashboard/hr'),
  attendance: (params) => axiosClient.get('/Dashboard/attendance', { params }),
  personal: () => axiosClient.get('/Dashboard/personal'),
  leave: () => axiosClient.get('/Dashboard/leave'),
  getAll: async () => {
    try {
      const [overview, hr, attendance, personal] = await Promise.all([
        axiosClient.get('/Dashboard/overview'),
        axiosClient.get('/Dashboard/hr'),
        axiosClient.get('/Dashboard/attendance'),
        axiosClient.get('/Dashboard/personal')
      ])
      return {
        overview: overview.data,
        hr: hr.data,
        attendance: attendance.data,
        personal: personal.data
      }
    } catch (error) {
      console.error('Failed to fetch all dashboard data:', error)
      throw error
    }
  }
}

// ==================== DEPARTMENTS ====================
const departments = {
  list: (params) => axiosClient.get('/Departments', { params }),
  create: (data) => axiosClient.post('/Departments', data),
  tree: () => axiosClient.get('/Departments/tree'),
  get: (id) => axiosClient.get(`/Departments/${id}`),
  update: (id, data) => axiosClient.put(`/Departments/${id}`, data),
  remove: (id) => axiosClient.delete(`/Departments/${id}`),
  export: () => axiosClient.get('/Departments/export')
}

// ==================== EMPLOYEES ====================
const employees = {
  list: (params) => axiosClient.get('/Employees', { params }),
  create: (data) => axiosClient.post('/Employees', { createDto: data }),
  get: (id) => axiosClient.get(`/Employees/${id}`),
  update: (id, data) => axiosClient.put(`/Employees/${id}`, data),
  terminate: (id, data) => axiosClient.put(`/Employees/${id}/terminate`, data),
  uploadAvatar: (id, formData) => axiosClient.post(`/Employees/${id}/upload-avatar`, formData),
  deleteAvatar: (id) => axiosClient.delete(`/Employees/${id}/avatar`),
  export: (params) => axiosClient.get('/Employees/export', { params }),
  search: (keyword) => axiosClient.get('/Employees/search', { params: { keyword } })
}

// ==================== FACE ATTENDANCE ====================
const faceAttendance = {
  checkIn: (employeeId, imageBase64) => axiosClient.post('/FaceAttendance/checkin', {
    employeeId,
    imageBase64
  }),
  checkOut: (employeeId, imageBase64) => axiosClient.post('/FaceAttendance/checkout', {
    employeeId,
    imageBase64
  }),
  getHistory: (employeeId, params) => axiosClient.get(`/FaceAttendance/history/${employeeId}`, { params }),
  registerFace: (employeeId, imageBase64) => axiosClient.post('/FaceAttendance/register-face', {
    employeeId,
    imageBase64
  }),
  getToday: (employeeId) => axiosClient.get(`/FaceAttendance/today/${employeeId}`),
  getRegistrationStatus: (employeeId) => axiosClient.get(`/FaceAttendance/registration-status/${employeeId}`)
}

// ==================== FEEDBACK ====================
const feedback = {
  list: (params) => axiosClient.get('/Feedback', { params }),
  create: (data) => axiosClient.post('/Feedback', data),
  get: (id) => axiosClient.get(`/Feedback/${id}`),
  update: (id, data) => axiosClient.put(`/Feedback/${id}`, data),
  remove: (id) => axiosClient.delete(`/Feedback/${id}`),
  respond: (id, data) => axiosClient.post(`/Feedback/${id}/respond`, data),
  my: () => axiosClient.get('/Feedback/my')
}

// ==================== GENDERS ====================
const genders = {
  list: (params) => axiosClient.get('/Genders', { params }),
  get: (id) => axiosClient.get(`/Genders/${id}`),
  create: (data) => axiosClient.post('/Genders', data),
  update: (id, data) => axiosClient.put(`/Genders/${id}`, data),
  remove: (id) => axiosClient.delete(`/Genders/${id}`)
}

// ==================== GOOGLE AUTH ====================
const googleAuth = {
  oauthCallback: () => axiosClient.get('/oauth2callback'),
  login: () => axiosClient.get('/Auth/google-login'),
  redirect: () => window.location.href = '/api/Auth/google-login'
}

// ==================== JOB POSITIONS ====================
const jobPositions = {
  list: (params) => axiosClient.get('/JobPositions', { params }),
  create: (data) => axiosClient.post('/JobPositions', data),
  get: (id) => axiosClient.get(`/JobPositions/${id}`),
  update: (id, data) => axiosClient.put(`/JobPositions/${id}`, data),
  remove: (id) => axiosClient.delete(`/JobPositions/${id}`),
  tree: () => axiosClient.get('/JobPositions/tree')
}

// ==================== LEAVE ====================
const leave = {
  types: () => axiosClient.get('/Leave/types'),
  createType: (data) => axiosClient.post('/Leave/types', data),
  updateType: (id, data) => axiosClient.put(`/Leave/types/${id}`, data),
  deleteType: (id) => axiosClient.delete(`/Leave/types/${id}`),
  request: (data) => axiosClient.post('/Leave/request', data),
  approve: (id, data) => axiosClient.post(`/Leave/approve/${id}`, data),
  reject: (id, data) => axiosClient.post(`/Leave/reject/${id}`, data),
  cancel: (id) => axiosClient.post(`/Leave/cancel/${id}`),
  my: (employeeId) => axiosClient.get(`/Leave/my/${employeeId}`),
  history: (employeeId) => axiosClient.get(`/Leave/history/${employeeId}`),
  calendar: (params) => axiosClient.get('/Leave/calendar', { params }),
  balance: (params) => axiosClient.get('/Leave/balance', { params }),
  pending: () => axiosClient.get('/Leave/pending'),
  stats: (params) => axiosClient.get('/Leave/stats', { params })
}

// ==================== LEAVE BALANCE ====================
const leaveBalance = {
  getByEmployee: (employeeId) => axiosClient.get(`/LeaveBalance/employee/${employeeId}`),
  create: (data) => axiosClient.post('/LeaveBalance', data),
  update: (id, data) => axiosClient.put(`/LeaveBalance/${id}`, data),
  remove: (id) => axiosClient.delete(`/LeaveBalance/${id}`),
  bulkUpdate: (data) => axiosClient.post('/LeaveBalance/bulk', data),
  yearly: (year) => axiosClient.get('/LeaveBalance/yearly', { params: { year } })
}

// ==================== PAYROLL ====================
const payroll = {
  calculate: (payPeriod) => axiosClient.post('/Payroll/calculate', null, { params: { payPeriod } }),
  calculateBulk: (payPeriod, employeeIds = []) => axiosClient.post('/Payroll/calculate-bulk', employeeIds, { params: { payPeriod } }),
  byEmployee: (employeeId) => axiosClient.get(`/Payroll/employee/${employeeId}`),
  get: (payrollId) => axiosClient.get(`/Payroll/${payrollId}`),
  approve: (payPeriod) => axiosClient.post('/Payroll/approve', null, { params: { payPeriod } }),
  taxSnapshot: (payrollId) => axiosClient.post(`/Payroll/tax-snapshot/${payrollId}`),
  history: (employeeId) => axiosClient.get(`/Payroll/history/${employeeId}`),
  list: (params) => axiosClient.get('/Payroll', { params }),
  export: (payPeriod) => axiosClient.get('/Payroll/export', { params: { payPeriod }, responseType: 'blob' })
}

// ==================== PAYROLL BANK TRANSFERS ====================
const payrollBankTransfers = {
  list: (params) => axiosClient.get('/PayrollBankTransfers', { params }),
  create: (data) => axiosClient.post('/PayrollBankTransfers', data),
  get: (id) => axiosClient.get(`/PayrollBankTransfers/${id}`),
  process: (id) => axiosClient.post(`/PayrollBankTransfers/${id}/process`),
  confirm: (id) => axiosClient.post(`/PayrollBankTransfers/${id}/confirm`),
  export: (id) => axiosClient.get(`/PayrollBankTransfers/${id}/export`, { responseType: 'blob' })
}

// ==================== PERFORMANCE ====================
const performance = {
  review: (data) => axiosClient.post('/Performance/review', data),
  reviewHistory: (employeeId) => axiosClient.get(`/Performance/review-history/${employeeId}`),
  skills: (employeeId) => axiosClient.get(`/Performance/skills/${employeeId}`),
  addSkill: (data) => axiosClient.post('/Performance/skills', data),
  updateSkill: (id, data) => axiosClient.put(`/Performance/skills/${id}`, data),
  removeSkill: (id) => axiosClient.delete(`/Performance/skills/${id}`),
  trainingRecommend: (employeeId) => axiosClient.get(`/Performance/training-recommend/${employeeId}`),
  capability: (employeeId) => axiosClient.get(`/Performance/capability/${employeeId}`),
  kpis: (employeeId) => axiosClient.get(`/Performance/kpis/${employeeId}`),
  addKpi: (data) => axiosClient.post('/Performance/kpis', data),
  goals: (employeeId) => axiosClient.get(`/Performance/goals/${employeeId}`),
  addGoal: (data) => axiosClient.post('/Performance/goals', data)
}

// ==================== POSITION MANAGEMENT ====================
const positionManagement = {
  list: (params) => axiosClient.get('/PositionManagement', { params }),
  create: (data) => axiosClient.post('/PositionManagement', data),
  get: (id) => axiosClient.get(`/PositionManagement/${id}`),
  update: (id, data) => axiosClient.put(`/PositionManagement/${id}`, data),
  remove: (id) => axiosClient.delete(`/PositionManagement/${id}`),
  approve: (id, data) => axiosClient.post(`/PositionManagement/${id}/approve`, data),
  reject: (id, data) => axiosClient.post(`/PositionManagement/${id}/reject`, data)
}

// ==================== POSITIONS ====================
const positions = {
  list: (params) => axiosClient.get('/Positions', { params }),
  create: (data) => axiosClient.post('/Positions', data),
  get: (id) => axiosClient.get(`/Positions/${id}`),
  update: (id, data) => axiosClient.put(`/Positions/${id}`, data),
  remove: (id) => axiosClient.delete(`/Positions/${id}`),
  tree: () => axiosClient.get('/Positions/tree')
}

// ==================== PROJECTS ====================
const projects = {
  list: (params) => axiosClient.get('/Projects', { params }),
  create: (data) => axiosClient.post('/Projects', data),
  get: (id) => axiosClient.get(`/Projects/${id}`),
  update: (id, data) => axiosClient.put(`/Projects/${id}`, data),
  remove: (id) => axiosClient.delete(`/Projects/${id}`),
  updateStatus: (projectId, data) => axiosClient.post(`/Projects/${projectId}/status`, data),
  assign: (data) => axiosClient.post('/Projects/assign', data),
  unassign: (assignmentId) => axiosClient.delete(`/Projects/assign/${assignmentId}`),
  assignments: (projectId) => axiosClient.get(`/Projects/${projectId}/assignments`),
  history: (employeeId) => axiosClient.get(`/Projects/history/${employeeId}`),
  myProjects: () => axiosClient.get('/Projects/my')
}

// ==================== RECRUITMENT ====================
const recruitment = {
  candidates: (params) => axiosClient.get('/Recruitment/candidates', { params }),
  createCandidate: (data) => axiosClient.post('/Recruitment/candidates', data),
  getCandidate: (id) => axiosClient.get(`/Recruitment/candidates/${id}`),
  updateCandidate: (id, data) => axiosClient.put(`/Recruitment/candidates/${id}`, data),
  deleteCandidate: (id) => axiosClient.delete(`/Recruitment/candidates/${id}`),
  workflowTemplates: () => axiosClient.get('/Recruitment/workflow/template'),
  advanceCandidate: (candidateId, data) => axiosClient.post(`/Recruitment/candidates/${candidateId}/advance`, data),
  review: (data) => axiosClient.post('/Recruitment/review', data),
  hire: (candidateId, data) => axiosClient.post(`/Recruitment/hire/${candidateId}`, data),
  positions: () => axiosClient.get('/Recruitment/positions'),
  createPosition: (data) => axiosClient.post('/Recruitment/positions', data),
  updatePosition: (id, data) => axiosClient.put(`/Recruitment/positions/${id}`, data),
  deletePosition: (id) => axiosClient.delete(`/Recruitment/positions/${id}`)
}

// ==================== REPORTS ====================
const reports = {
  employees: (params) => axiosClient.get('/Reports/employees', { params }),
  attendance: (params) => axiosClient.get('/Reports/attendance', { params }),
  payroll: (params) => axiosClient.get('/Reports/payroll', { params }),
  leave: (params) => axiosClient.get('/Reports/leave', { params }),
  recruitment: (params) => axiosClient.get('/Reports/recruitment', { params }),
  performance: (params) => axiosClient.get('/Reports/performance', { params }),
  export: (type, params) => axiosClient.get(`/Reports/export/${type}`, { params, responseType: 'blob' })
}

// ==================== REWARD DISCIPLINE ====================
const rewardDiscipline = {
  rewards: (employeeId) => axiosClient.get(`/RewardDiscipline/reward/${employeeId}`),
  disciplines: (employeeId) => axiosClient.get(`/RewardDiscipline/discipline/${employeeId}`),
  addReward: (data) => axiosClient.post('/RewardDiscipline/reward', data),
  addDiscipline: (data) => axiosClient.post('/RewardDiscipline/discipline', data),
  updateReward: (id, data) => axiosClient.put(`/RewardDiscipline/reward/${id}`, data),
  updateDiscipline: (id, data) => axiosClient.put(`/RewardDiscipline/discipline/${id}`, data),
  deleteReward: (id) => axiosClient.delete(`/RewardDiscipline/reward/${id}`),
  deleteDiscipline: (id) => axiosClient.delete(`/RewardDiscipline/discipline/${id}`),
  summary: (employeeId) => axiosClient.get(`/RewardDiscipline/summary/${employeeId}`)
}

// ==================== ROLES ====================
const roles = {
  list: (params) => axiosClient.get('/Roles', { params }),
  create: (data) => axiosClient.post('/Roles', data),
  get: (id) => axiosClient.get(`/Roles/${id}`),
  update: (id, data) => axiosClient.put(`/Roles/${id}`, data),
  remove: (id) => axiosClient.delete(`/Roles/${id}`),
  permissions: () => axiosClient.get('/Roles/permissions'),
  assignPermission: (roleId, permissionId) => axiosClient.post(`/Roles/${roleId}/permissions/${permissionId}`),
  removePermission: (roleId, permissionId) => axiosClient.delete(`/Roles/${roleId}/permissions/${permissionId}`)
}

// ==================== STATUS MASTER ====================
const statusMaster = {
  list: (params) => axiosClient.get('/StatusMaster', { params }),
  get: (id) => axiosClient.get(`/StatusMaster/${id}`),
  create: (data) => axiosClient.post('/StatusMaster', data),
  update: (id, data) => axiosClient.put(`/StatusMaster/${id}`, data),
  remove: (id) => axiosClient.delete(`/StatusMaster/${id}`),
  byType: (type) => axiosClient.get('/StatusMaster/by-type', { params: { type } })
}

// ==================== SURVEYS ====================
const surveys = {
  list: (params) => axiosClient.get('/Surveys', { params }),
  create: (data) => axiosClient.post('/Surveys', data),
  get: (id) => axiosClient.get(`/Surveys/${id}`),
  update: (id, data) => axiosClient.put(`/Surveys/${id}`, data),
  remove: (id) => axiosClient.delete(`/Surveys/${id}`),
  respond: (data) => axiosClient.post('/Surveys/response', data),
  results: (surveyId) => axiosClient.get(`/Surveys/${surveyId}/results`),
  myResponses: () => axiosClient.get('/Surveys/my-responses'),
  publish: (id) => axiosClient.post(`/Surveys/${id}/publish`),
  close: (id) => axiosClient.post(`/Surveys/${id}/close`)
}

// ==================== TRAINING ====================
const training = {
  // Training Programs
  programs: (params) => axiosClient.get('/Training/programs', { params }),
  getProgram: (id) => axiosClient.get(`/Training/programs/${id}`),
  createProgram: (data) => axiosClient.post('/Training/programs', data),
  updateProgram: (id, data) => axiosClient.put(`/Training/programs/${id}`, data),
  deleteProgram: (id) => axiosClient.delete(`/Training/programs/${id}`),
  
  // Courses
  courses: (params) => axiosClient.get('/Training/courses', { params }),
  getCourse: (id) => axiosClient.get(`/Training/courses/${id}`),
  createCourse: (data) => axiosClient.post('/Training/courses', data),
  updateCourse: (id, data) => axiosClient.put(`/Training/courses/${id}`, data),
  deleteCourse: (id) => axiosClient.delete(`/Training/courses/${id}`),
  
  // Enrollments
  enroll: (data) => axiosClient.post('/Training/enroll', data),
  unenroll: (enrollmentId) => axiosClient.delete(`/Training/enroll/${enrollmentId}`),
  employeeCourses: (employeeId) => axiosClient.get(`/Training/employee/${employeeId}/courses`),
  employeePrograms: (employeeId) => axiosClient.get(`/Training/employee/${employeeId}/programs`),
  
  // Results & Certificates
  result: (data) => axiosClient.post('/Training/result', data),
  certificates: (employeeId) => axiosClient.get(`/Training/certificates/${employeeId}`),
  employeeCertificates: (employeeId) => axiosClient.get(`/Training/employee/${employeeId}/certificates`),
  
  // Training Sessions
  sessions: (params) => axiosClient.get('/Training/sessions', { params }),
  createSession: (data) => axiosClient.post('/Training/sessions', data),
  updateSession: (id, data) => axiosClient.put(`/Training/sessions/${id}`, data),
  deleteSession: (id) => axiosClient.delete(`/Training/sessions/${id}`),
  
  // Attendance
  sessionAttendance: (sessionId) => axiosClient.get(`/Training/sessions/${sessionId}/attendance`),
  markAttendance: (sessionId, data) => axiosClient.post(`/Training/sessions/${sessionId}/attendance`, data)
}

// ==================== USERS ====================
const users = {
  list: (params) => axiosClient.get('/Users', { params }),
  create: (data) => axiosClient.post('/Users', data),
  get: (id) => axiosClient.get(`/Users/${id}`),
  update: (id, data) => axiosClient.put(`/Users/${id}`, data),
  remove: (id) => axiosClient.delete(`/Users/${id}`),
  toggleActive: (id, data) => axiosClient.put(`/Users/${id}/toggle-active`, data),
  resetPassword: (id, data) => axiosClient.put(`/Users/${id}/reset-password`, data),
  changeRole: (id, roleId) => axiosClient.put(`/Users/${id}/role`, { roleId })
}

// ==================== WORK REPORTS ====================
const workReports = {
  list: (params) => axiosClient.get('/WorkReports', { params }),
  create: (data) => axiosClient.post('/WorkReports', data),
  get: (id) => axiosClient.get(`/WorkReports/${id}`),
  update: (id, data) => axiosClient.put(`/WorkReports/${id}`, data),
  remove: (id) => axiosClient.delete(`/WorkReports/${id}`),
  my: (params) => axiosClient.get('/WorkReports/my', { params }),
  pending: () => axiosClient.get('/WorkReports/pending'),
  approve: (id, data) => axiosClient.post(`/WorkReports/${id}/approve`, data),
  reject: (id, data) => axiosClient.post(`/WorkReports/${id}/reject`, data),
  comment: (id, data) => axiosClient.post(`/WorkReports/${id}/comment`, data),
  export: (params) => axiosClient.get('/WorkReports/export', { params, responseType: 'blob' }),
  stats: (params) => axiosClient.get('/WorkReports/stats', { params })
}

// ==================== WORK TYPES ====================
const workTypes = {
  list: (params) => axiosClient.get('/WorkTypes', { params }),
  create: (data) => axiosClient.post('/WorkTypes', data),
  get: (id) => axiosClient.get(`/WorkTypes/${id}`),
  update: (id, data) => axiosClient.put(`/WorkTypes/${id}`, data),
  remove: (id) => axiosClient.delete(`/WorkTypes/${id}`),
  toggleActive: (id, data) => axiosClient.put(`/WorkTypes/${id}/toggle-active`, data)
}

// ==================== EXPORT ALL ====================
const api = {
  announcement,
  assets,
  attendance,
  auth,
  bankPartners,
  chatbot,
  companyBankAccounts,
  companyInfo,
  contracts,
  contractTypes,
  dashboard,
  departments,
  employees,
  faceAttendance,
  feedback,
  genders,
  googleAuth,
  jobPositions,
  leave,
  leaveBalance,
  payroll,
  payrollBankTransfers,
  performance,
  positionManagement,
  positions,
  projects,
  recruitment,
  reports,
  rewardDiscipline,
  roles,
  statusMaster,
  surveys,
  training,
  users,
  workReports,
  workTypes
}

export default api