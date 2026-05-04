type Params = Record<string, any>
type ApiResponse<T = any> = Promise<T>

declare const api: {
  announcement: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
  }

  assets: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    assign: (data: any) => ApiResponse<any>
    revoke: (id: string | number) => ApiResponse<any>
    history: (assetId: string | number) => ApiResponse<any[]>
  }

  attendance: {
    my: (employeeId: string | number) => ApiResponse<any[]>
    department: (departmentId: string | number) => ApiResponse<any[]>
    manual: (data: any) => ApiResponse<any>
    raw: (employeeId: string | number) => ApiResponse<any[]>
    adjust: (attendanceId: string | number, data: any) => ApiResponse<any>
    overtimeList: (params?: Params) => ApiResponse<any[]>
    createOvertime: (data: any) => ApiResponse<any>
    approveOvertime: (id: string | number, data?: any) => ApiResponse<any>
    generateQr: () => ApiResponse<any>
    activeQr: () => ApiResponse<any>
    scan: (data: any) => ApiResponse<any>
  }

  auth: {
    login: (data: any) => ApiResponse<any>
    logout: () => ApiResponse<any>
    changePassword: (data: any) => ApiResponse<any>
    profile: () => ApiResponse<any>
    updateProfile: (data: any) => ApiResponse<any>
  }

  companyInfo: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    update: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
  }

  contracts: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    extend: (id: string | number, data: any) => ApiResponse<any>
    employeeBenefits: (employeeId: string | number) => ApiResponse<any[]>
    addEmployeeBenefit: (employeeId: string | number, data: any) => ApiResponse<any>
    updateBenefit: (benefitId: string | number, data: any) => ApiResponse<any>
    deleteBenefit: (benefitId: string | number) => ApiResponse<any>
  }

  dashboard: {
    overview: () => ApiResponse<any>
    hr: () => ApiResponse<any>
    attendance: () => ApiResponse<any>
    personal: () => ApiResponse<any>
  }

  departments: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    tree: () => ApiResponse<any[]>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
    export: () => ApiResponse<any>
  }

  employees: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    terminate: (id: string | number, data?: any) => ApiResponse<any>
    uploadAvatar: (id: string | number, formData: FormData) => ApiResponse<any>
    deleteAvatar: (id: string | number) => ApiResponse<any>
  }

  googleAuth: {
    oauthCallback: () => ApiResponse<any>
  }

  leave: {
    types: () => ApiResponse<any[]>
    createType: (data: any) => ApiResponse<any>
    request: (data: any) => ApiResponse<any>
    approve: (id: string | number, data?: any) => ApiResponse<any>
    reject: (id: string | number, data?: any) => ApiResponse<any>
    my: (employeeId: string | number) => ApiResponse<any[]>
    history: (employeeId: string | number) => ApiResponse<any[]>
    calendar: (params?: Params) => ApiResponse<any[]>
    balance: (params?: Params) => ApiResponse<any>
  }

  leaveBalance: {
    getByEmployee: (employeeId: string | number) => ApiResponse<any>
    create: (data: any) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
  }

  payroll: {
    calculate: (data: any) => ApiResponse<any>
    calculateBulk: (data: any) => ApiResponse<any>
    byEmployee: (employeeId: string | number) => ApiResponse<any>
    get: (payrollId: string | number) => ApiResponse<any>
    approve: (data: any) => ApiResponse<any>
    taxSnapshot: (payrollId: string | number, data?: any) => ApiResponse<any>
    history: (employeeId: string | number) => ApiResponse<any[]>
  }

  performance: {
    review: (data: any) => ApiResponse<any>
    reviewHistory: (employeeId: string | number) => ApiResponse<any[]>
    skills: (employeeId: string | number) => ApiResponse<any[]>
    addSkill: (data: any) => ApiResponse<any>
    trainingRecommend: (employeeId: string | number) => ApiResponse<any[]>
    capability: (employeeId: string | number) => ApiResponse<any>
  }

  positions: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
  }

  projects: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    updateStatus: (projectId: string | number, data: any) => ApiResponse<any>
    assign: (data: any) => ApiResponse<any>
    assignments: (projectId: string | number) => ApiResponse<any[]>
    history: (employeeId: string | number) => ApiResponse<any[]>
  }

  recruitment: {
    candidates: (params?: Params) => ApiResponse<any[]>
    createCandidate: (data: any) => ApiResponse<any>
    workflowTemplates: () => ApiResponse<any[]>
    advanceCandidate: (candidateId: string | number, data: any) => ApiResponse<any>
    review: (data: any) => ApiResponse<any>
    hire: (candidateId: string | number, data: any) => ApiResponse<any>
  }

  reports: {
    employees: (params?: Params) => ApiResponse<any>
    attendance: (params?: Params) => ApiResponse<any>
    payroll: (params?: Params) => ApiResponse<any>
    leave: (params?: Params) => ApiResponse<any>
  }

  rewardDiscipline: {
    rewards: (employeeId: string | number) => ApiResponse<any[]>
    disciplines: (employeeId: string | number) => ApiResponse<any[]>
    addReward: (data: any) => ApiResponse<any>
    addDiscipline: (data: any) => ApiResponse<any>
  }

  roles: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
  }

  surveys: {
    create: (data: any) => ApiResponse<any>
    respond: (data: any) => ApiResponse<any>
    results: (surveyId: string | number) => ApiResponse<any>
  }

  training: {
    // Các methods có sẵn
    trainings: (params?: Params) => Promise<any[]>
    courses: (params?: Params) => Promise<any[]>
    enroll: (data: any) => Promise<any>
    employeeCourses: (employeeId: string | number) => Promise<any[]>
    employeeCertificates: (employeeId: string | number) => Promise<any[]>
    programs: () => Promise<any[]>
    createProgram: (data: any) => Promise<any>
    enrollProgram: (data: any) => Promise<any>
    employeeProgram: (employeeId: string | number) => Promise<any[]>
    result: (data: any) => Promise<any>
    certificates: (employeeId: string | number) => Promise<any[]>
    // CRUD methods bổ sung
    getAll: (params?: Params) => Promise<any[]>
    getById: (id: string | number) => Promise<any>
    create: (data: any) => Promise<any>
    update: (id: string | number, data: any) => Promise<any>
    delete: (id: string | number) => Promise<any>
  }

  users: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    get: (id: string | number) => ApiResponse<any>
    update: (id: string | number, data: any) => ApiResponse<any>
    remove: (id: string | number) => ApiResponse<any>
    toggleActive: (id: string | number, data?: any) => ApiResponse<any>
    resetPassword: (id: string | number, data?: any) => ApiResponse<any>
  }

  workTypes: {
    list: (params?: Params) => ApiResponse<any[]>
    create: (data: any) => ApiResponse<any>
    toggleActive: (id: string | number, data?: any) => ApiResponse<any>
  }
}

export default api