import axiosClient from './axiosClient'

type Params = Record<string, any>
type ApiResponse<T = any> = Promise<T>

const announcement = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/announcements', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/announcements', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/announcements/${id}`)
}

const assets = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/assets', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/assets', data),
  assign: (data: any): ApiResponse<any> => axiosClient.post('/assets/assign', data),
  revoke: (id: string | number): ApiResponse<any> => axiosClient.post(`/assets/revoke/${id}`),
  history: (assetId: string | number): ApiResponse<any[]> => axiosClient.get(`/assets/history/${assetId}`)
}

const attendance = {
  my: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/attendance/my/${employeeId}`),
  department: (departmentId: string | number): ApiResponse<any[]> => axiosClient.get(`/attendance/department/${departmentId}`),
  manual: (data: any): ApiResponse<any> => axiosClient.post('/attendance/manual', data),
  raw: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/attendance/raw/${employeeId}`),
  adjust: (attendanceId: string | number, data: any): ApiResponse<any> => axiosClient.put(`/attendance/adjust/${attendanceId}`, data),
  overtimeList: (params?: Params): ApiResponse<any[]> => axiosClient.get('/attendance/overtime', { params }),
  createOvertime: (data: any): ApiResponse<any> => axiosClient.post('/attendance/overtime', data),
  approveOvertime: (id: string | number, data?: any): ApiResponse<any> => axiosClient.post(`/attendance/overtime/approve/${id}`, data),
  generateQr: (): ApiResponse<any> => axiosClient.post('/attendance/generate-qr'),
  activeQr: (): ApiResponse<any> => axiosClient.get('/attendance/active-qr'),
  scan: (data: any): ApiResponse<any> => axiosClient.post('/attendance/scan', data)
}

const auth = {
  login: (data: any): ApiResponse<any> => axiosClient.post('/auth/login', data),
  logout: (): ApiResponse<any> => axiosClient.post('/auth/logout'),
  changePassword: (data: any): ApiResponse<any> => axiosClient.post('/auth/change-password', data),
  profile: (): ApiResponse<any> => axiosClient.get('/auth/profile'),
  updateProfile: (data: any): ApiResponse<any> => axiosClient.put('/auth/profile', data)
}

const companyInfo = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/CompanyInfo', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/CompanyInfo', data),
  update: (data: any): ApiResponse<any> => axiosClient.put('/CompanyInfo', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/CompanyInfo/${id}`),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/CompanyInfo/${id}`)
}

const contracts = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Contracts', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Contracts', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Contracts/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Contracts/${id}`, data),
  extend: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Contracts/${id}/extend`, data),
  employeeBenefits: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/Contracts/employee/${employeeId}/benefits`),
  addEmployeeBenefit: (employeeId: string | number, data: any): ApiResponse<any> => axiosClient.post(`/Contracts/employee/${employeeId}/benefits`, data),
  updateBenefit: (benefitId: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Contracts/benefits/${benefitId}`, data),
  deleteBenefit: (benefitId: string | number): ApiResponse<any> => axiosClient.delete(`/Contracts/benefits/${benefitId}`)
}

const dashboard = {
  overview: (): ApiResponse<any> => axiosClient.get('/Dashboard/overview'),
  hr: (): ApiResponse<any> => axiosClient.get('/Dashboard/hr'),
  attendance: (): ApiResponse<any> => axiosClient.get('/Dashboard/attendance'),
  personal: (): ApiResponse<any> => axiosClient.get('/Dashboard/personal')
}

const departments = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Departments', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Departments', data),
  tree: (): ApiResponse<any[]> => axiosClient.get('/Departments/tree'),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Departments/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Departments/${id}`, data),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/Departments/${id}`),
  export: (): ApiResponse<any> => axiosClient.get('/Departments/export')
}

const employees = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Employees', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Employees', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Employees/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Employees/${id}`, data),
  terminate: (id: string | number, data?: any): ApiResponse<any> => axiosClient.put(`/Employees/${id}/terminate`, data),
  uploadAvatar: (id: string | number, formData: FormData): ApiResponse<any> => axiosClient.post(`/Employees/${id}/upload-avatar`, formData),
  deleteAvatar: (id: string | number): ApiResponse<any> => axiosClient.delete(`/Employees/${id}/avatar`)
}

const googleAuth = {
  oauthCallback: (): ApiResponse<any> => axiosClient.get('/oauth2callback')
}

const leave = {
  types: (): ApiResponse<any[]> => axiosClient.get('/leave/types'),
  createType: (data: any): ApiResponse<any> => axiosClient.post('/leave/types', data),
  request: (data: any): ApiResponse<any> => axiosClient.post('/leave/request', data),
  approve: (id: string | number, data?: any): ApiResponse<any> => axiosClient.post(`/leave/approve/${id}`, data),
  reject: (id: string | number, data?: any): ApiResponse<any> => axiosClient.post(`/leave/reject/${id}`, data),
  my: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/leave/my/${employeeId}`),
  history: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/leave/history/${employeeId}`),
  calendar: (params?: Params): ApiResponse<any[]> => axiosClient.get('/leave/calendar', { params }),
  balance: (params?: Params): ApiResponse<any> => axiosClient.get('/leave/balance', { params })
}

const leaveBalance = {
  getByEmployee: (employeeId: string | number): ApiResponse<any> => axiosClient.get(`/LeaveBalance/employee/${employeeId}`),
  create: (data: any): ApiResponse<any> => axiosClient.post('/LeaveBalance', data),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/LeaveBalance/${id}`)
}

const payroll = {
  calculate: (data: any): ApiResponse<any> => axiosClient.post('/payroll/calculate', data),
  calculateBulk: (data: any): ApiResponse<any> => axiosClient.post('/payroll/calculate-bulk', data),
  byEmployee: (employeeId: string | number): ApiResponse<any> => axiosClient.get(`/payroll/employee/${employeeId}`),
  get: (payrollId: string | number): ApiResponse<any> => axiosClient.get(`/payroll/${payrollId}`),
  approve: (data: any): ApiResponse<any> => axiosClient.post('/payroll/approve', data),
  taxSnapshot: (payrollId: string | number, data?: any): ApiResponse<any> => axiosClient.post(`/payroll/tax-snapshot/${payrollId}`, data),
  history: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/payroll/history/${employeeId}`)
}

const performance = {
  review: (data: any): ApiResponse<any> => axiosClient.post('/performance/review', data),
  reviewHistory: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/performance/review-history/${employeeId}`),
  skills: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/performance/skills/${employeeId}`),
  addSkill: (data: any): ApiResponse<any> => axiosClient.post('/performance/skills', data),
  trainingRecommend: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/performance/training-recommend/${employeeId}`),
  capability: (employeeId: string | number): ApiResponse<any> => axiosClient.get(`/performance/capability/${employeeId}`)
}

const positions = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Positions', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Positions', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Positions/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Positions/${id}`, data),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/Positions/${id}`)
}

const projects = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/projects', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/projects', data),
  updateStatus: (projectId: string | number, data: any): ApiResponse<any> => axiosClient.post(`/projects/${projectId}/status`, data),
  assign: (data: any): ApiResponse<any> => axiosClient.post('/projects/assign', data),
  assignments: (projectId: string | number): ApiResponse<any[]> => axiosClient.get(`/projects/${projectId}/assignments`),
  history: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/projects/history/${employeeId}`)
}

const recruitment = {
  candidates: (params?: Params): ApiResponse<any[]> => axiosClient.get('/recruitment/candidates', { params }),
  createCandidate: (data: any): ApiResponse<any> => axiosClient.post('/recruitment/candidates', data),
  workflowTemplates: (): ApiResponse<any[]> => axiosClient.get('/recruitment/workflow/template'),
  advanceCandidate: (candidateId: string | number, data: any): ApiResponse<any> => axiosClient.post(`/recruitment/candidates/${candidateId}/advance`, data),
  review: (data: any): ApiResponse<any> => axiosClient.post('/recruitment/review', data),
  hire: (candidateId: string | number, data: any): ApiResponse<any> => axiosClient.post(`/recruitment/hire/${candidateId}`, data)
}

const reports = {
  employees: (params?: Params): ApiResponse<any> => axiosClient.get('/reports/employees', { params }),
  attendance: (params?: Params): ApiResponse<any> => axiosClient.get('/reports/attendance', { params }),
  payroll: (params?: Params): ApiResponse<any> => axiosClient.get('/reports/payroll', { params }),
  leave: (params?: Params): ApiResponse<any> => axiosClient.get('/reports/leave', { params })
}

const rewardDiscipline = {
  rewards: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/reward-discipline/reward/${employeeId}`),
  disciplines: (employeeId: string | number): ApiResponse<any[]> => axiosClient.get(`/reward-discipline/discipline/${employeeId}`),
  addReward: (data: any): ApiResponse<any> => axiosClient.post('/reward-discipline/reward', data),
  addDiscipline: (data: any): ApiResponse<any> => axiosClient.post('/reward-discipline/discipline', data)
}

const roles = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Roles', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Roles', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Roles/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Roles/${id}`, data),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/Roles/${id}`)
}

const surveys = {
  create: (data: any): ApiResponse<any> => axiosClient.post('/surveys', data),
  respond: (data: any): ApiResponse<any> => axiosClient.post('/surveys/response', data),
  results: (surveyId: string | number): ApiResponse<any> => axiosClient.get(`/surveys/${surveyId}/results`)
}

// Helper function for training endpoints
const tryTrainingPaths = async (method: 'get'|'post'|'put'|'delete', paths: string[], requestData?: any): Promise<any> => {
  let lastError: any = null

  for (const path of paths) {
    try {
      if (method === 'get') return await axiosClient.get(path, { params: requestData })
      if (method === 'post') return await axiosClient.post(path, requestData)
      if (method === 'put') return await axiosClient.put(path, requestData)
      if (method === 'delete') return await axiosClient.delete(path)
    } catch (err: any) {
      lastError = err
      console.warn(`Training path failed ${method} ${path}:`, err?.message || err)
      continue
    }
  }

  throw lastError || new Error('Không tìm thấy endpoint Training phù hợp')
}

// Training CRUD functions with async fixed
const trainingGetAll = async (params?: Params): Promise<any[]> => {
  const paths = [
    '/Training/trainings',
    '/training/trainings',
    '/Training/courses',
    '/training/courses',
    '/api/Training/trainings',
    '/api/training/trainings',
    '/api/Training/courses',
    '/api/training/courses'
  ]
  return await tryTrainingPaths('get', paths, params)
}

const trainingGetById = async (id: string | number): Promise<any> => {
  const paths = [
    `/Training/trainings/${id}`,
    `/training/trainings/${id}`,
    `/Training/courses/${id}`,
    `/training/courses/${id}`,
    `/api/Training/trainings/${id}`,
    `/api/training/trainings/${id}`,
    `/api/Training/courses/${id}`,
    `/api/training/courses/${id}`
  ]
  return await tryTrainingPaths('get', paths)
}

const trainingCreate = async (data: any): Promise<any> => {
  const requestData = {
    trainingName: data.trainingName || data.name,
    description: data.description || '',
    startDate: data.startDate || data.date,
    endDate: data.endDate || data.date,
    trainer: data.trainer || data.instructor,
    location: data.location || 'Phòng họp',
    format: data.format || 'Online',
    department: data.department || 'Công nghệ',
    status: data.status || 'open'
  }

  const paths = [
    '/Training/trainings',
    '/training/trainings',
    '/Training/courses',
    '/training/courses',
    '/api/Training/trainings',
    '/api/training/trainings',
    '/api/Training/courses',
    '/api/training/courses'
  ]
  return await tryTrainingPaths('post', paths, requestData)
}

const trainingUpdate = async (id: string | number, data: any): Promise<any> => {
  const requestData = {
    trainingName: data.trainingName || data.name,
    description: data.description || '',
    startDate: data.startDate || data.date,
    endDate: data.endDate || data.date,
    trainer: data.trainer || data.instructor,
    location: data.location || 'Phòng họp',
    format: data.format || 'Online',
    department: data.department || 'Công nghệ',
    status: data.status || 'open'
  }

  const paths = [
    `/Training/trainings/${id}`,
    `/training/trainings/${id}`,
    `/Training/courses/${id}`,
    `/training/courses/${id}`,
    `/api/Training/trainings/${id}`,
    `/api/training/trainings/${id}`,
    `/api/Training/courses/${id}`,
    `/api/training/courses/${id}`
  ]
  return await tryTrainingPaths('put', paths, requestData)
}

const trainingDelete = async (id: string | number): Promise<any> => {
  const paths = [
    `/Training/trainings/${id}`,
    `/training/trainings/${id}`,
    `/Training/courses/${id}`,
    `/training/courses/${id}`,
    `/api/Training/trainings/${id}`,
    `/api/training/trainings/${id}`,
    `/api/Training/courses/${id}`,
    `/api/training/courses/${id}`
  ]
  return await tryTrainingPaths('delete', paths)
}

const training = {
  trainings: (params?: Params): Promise<any[]> => axiosClient.get('/Training/trainings', { params }),
  courses: (params?: Params): Promise<any[]> => axiosClient.get('/Training/courses', { params }),
  enroll: (data: any): Promise<any> => axiosClient.post('/Training/enroll', data),
  employeeCourses: (employeeId: string | number): Promise<any[]> => axiosClient.get(`/Training/employee/${employeeId}/courses`),
  employeeCertificates: (employeeId: string | number): Promise<any[]> => axiosClient.get(`/Training/employee/${employeeId}/certificates`),
  programs: (): Promise<any[]> => axiosClient.get('/training/programs'),
  createProgram: (data: any): Promise<any> => axiosClient.post('/training/programs', data),
  enrollProgram: (data: any): Promise<any> => axiosClient.post('/training/enroll', data),
  employeeProgram: (employeeId: string | number): Promise<any[]> => axiosClient.get(`/training/employee/${employeeId}`),
  result: (data: any): Promise<any> => axiosClient.post('/training/result', data),
  certificates: (employeeId: string | number): Promise<any[]> => axiosClient.get(`/training/certificates/${employeeId}`),

  getAll: trainingGetAll,
  getById: trainingGetById,
  create: trainingCreate,
  update: trainingUpdate,
  delete: trainingDelete
}

const users = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/Users', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/Users', data),
  get: (id: string | number): ApiResponse<any> => axiosClient.get(`/Users/${id}`),
  update: (id: string | number, data: any): ApiResponse<any> => axiosClient.put(`/Users/${id}`, data),
  remove: (id: string | number): ApiResponse<any> => axiosClient.delete(`/Users/${id}`),
  toggleActive: (id: string | number, data?: any): ApiResponse<any> => axiosClient.put(`/Users/${id}/toggle-active`, data),
  resetPassword: (id: string | number, data?: any): ApiResponse<any> => axiosClient.put(`/Users/${id}/reset-password`, data)
}

const workTypes = {
  list: (params?: Params): ApiResponse<any[]> => axiosClient.get('/WorkTypes', { params }),
  create: (data: any): ApiResponse<any> => axiosClient.post('/WorkTypes', data),
  toggleActive: (id: string | number, data?: any): ApiResponse<any> => axiosClient.put(`/WorkTypes/${id}/toggle-active`, data)
}

const api = {
  announcement,
  assets,
  attendance,
  auth,
  companyInfo,
  contracts,
  dashboard,
  departments,
  employees,
  googleAuth,
  leave,
  leaveBalance,
  payroll,
  performance,
  positions,
  projects,
  recruitment,
  reports,
  rewardDiscipline,
  roles,
  surveys,
  training,
  users,
  workTypes
}

export default api