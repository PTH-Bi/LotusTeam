const ROLE_CODES = {
  SUPER_ADMIN: 'SUPER_ADMIN',
  ADMIN: 'ADMIN',
  HR_MANAGER: 'HR_MANAGER',
  HR_STAFF: 'HR_STAFF',
  RECRUITER: 'RECRUITER',
  INTERVIEWER: 'INTERVIEWER',
  TRAINER: 'TRAINER',
  ACCOUNTANT: 'ACCOUNTANT',
  FINANCE_MANAGER: 'FINANCE_MANAGER',
  DIRECTOR: 'DIRECTOR',
  MANAGER: 'MANAGER',
  TEAM_LEADER: 'TEAM_LEADER',
  EMPLOYEE: 'EMPLOYEE',
  PROBATION_STAFF: 'PROBATION_STAFF'
}

const ROLE_ALIASES = {
  SUPERADMIN: ROLE_CODES.SUPER_ADMIN,
  SUPER_ADMIN: ROLE_CODES.SUPER_ADMIN,
  ADMIN: ROLE_CODES.ADMIN,
  HRMANAGER: ROLE_CODES.HR_MANAGER,
  HR_MANAGER: ROLE_CODES.HR_MANAGER,
  HRSTAFF: ROLE_CODES.HR_STAFF,
  HR_STAFF: ROLE_CODES.HR_STAFF,
  RECRUITER: ROLE_CODES.RECRUITER,
  INTERVIEWER: ROLE_CODES.INTERVIEWER,
  TRAINER: ROLE_CODES.TRAINER,
  ACCOUNTANT: ROLE_CODES.ACCOUNTANT,
  FINANCEMANAGER: ROLE_CODES.FINANCE_MANAGER,
  FINANCE_MANAGER: ROLE_CODES.FINANCE_MANAGER,
  DIRECTOR: ROLE_CODES.DIRECTOR,
  MANAGER: ROLE_CODES.MANAGER,
  TEAMLEADER: ROLE_CODES.TEAM_LEADER,
  TEAM_LEADER: ROLE_CODES.TEAM_LEADER,
  EMPLOYEE: ROLE_CODES.EMPLOYEE,
  PROBATIONSTAFF: ROLE_CODES.PROBATION_STAFF,
  PROBATION_STAFF: ROLE_CODES.PROBATION_STAFF,
  USER: ROLE_CODES.EMPLOYEE,
  STAFF: ROLE_CODES.EMPLOYEE
}

const ROLE_GROUPS = {
  director: [
    ROLE_CODES.SUPER_ADMIN,
    ROLE_CODES.ADMIN,
    ROLE_CODES.DIRECTOR,
    ROLE_CODES.MANAGER
  ],
  hr: [
    ROLE_CODES.HR_MANAGER,
    ROLE_CODES.HR_STAFF,
    ROLE_CODES.RECRUITER,
    ROLE_CODES.INTERVIEWER,
    ROLE_CODES.TRAINER
  ],
  finance: [
    ROLE_CODES.ACCOUNTANT,
    ROLE_CODES.FINANCE_MANAGER
  ],
  tech: [
    ROLE_CODES.TEAM_LEADER,
    ROLE_CODES.EMPLOYEE,
    ROLE_CODES.PROBATION_STAFF
  ],
  marketing: [
    ROLE_CODES.TEAM_LEADER,
    ROLE_CODES.EMPLOYEE
  ]
}

const normalizeRoleCode = (role) => {
  if (!role) return null
  const plain = String(role).trim().replace(/\s+/g, '_').replace(/-/g, '_').replace(/\./g, '').toUpperCase()
  return ROLE_ALIASES[plain] || plain
}

const normalizeDepartment = (department) => {
  if (!department) return ''
  return String(department).trim().toLowerCase()
}

const normalizePosition = (position) => {
  if (!position) return ''
  return String(position).trim().toLowerCase()
}

const guessRoleCode = ({ role, department, position, username }) => {
  const normalizedRole = normalizeRoleCode(role)
  if (normalizedRole && ROLE_ALIASES[normalizedRole]) {
    return normalizedRole
  }

  const dept = normalizeDepartment(department)
  const pos = normalizePosition(position)
  const name = String(username || '').trim().toLowerCase()

  if (normalizedRole) {
    if (normalizedRole === 'ADMIN') return ROLE_CODES.ADMIN
    if (normalizedRole === 'EMPLOYEE') return ROLE_CODES.EMPLOYEE
    if (ROLE_ALIASES[normalizedRole]) return ROLE_ALIASES[normalizedRole]
    return normalizedRole
  }

  if (name === 'admin') {
    return ROLE_CODES.ADMIN
  }

  if (dept.includes('ban giam doc') || dept.includes('ban giám đốc')) {
    if (pos.includes('giám đốc') || pos.includes('director')) return ROLE_CODES.DIRECTOR
    if (pos.includes('trưởng') || pos.includes('leader') || pos.includes('manager')) return ROLE_CODES.MANAGER
    return ROLE_CODES.MANAGER
  }

  if (dept.includes('phòng nhân sự') || dept.includes('nhân sự')) {
    if (pos.includes('manager') || pos.includes('quản lý')) return ROLE_CODES.HR_MANAGER
    if (pos.includes('recruit') || pos.includes('tuyển dụng')) return ROLE_CODES.RECRUITER
    if (pos.includes('interview') || pos.includes('phỏng vấn')) return ROLE_CODES.INTERVIEWER
    if (pos.includes('trainer') || pos.includes('đào tạo')) return ROLE_CODES.TRAINER
    return ROLE_CODES.HR_STAFF
  }

  if (dept.includes('phòng kế toán') || dept.includes('kế toán') || dept.includes('tài chính')) {
    if (pos.includes('manager') || pos.includes('quản lý') || pos.includes('finance')) return ROLE_CODES.FINANCE_MANAGER
    return ROLE_CODES.ACCOUNTANT
  }

  if (dept.includes('phòng kỹ thuật') || dept.includes('kỹ thuật')) {
    if (pos.includes('trưởng') || pos.includes('leader')) return ROLE_CODES.TEAM_LEADER
    if (pos.includes('probation') || pos.includes('thử việc') || pos.includes('probation')) return ROLE_CODES.PROBATION_STAFF
    return ROLE_CODES.EMPLOYEE
  }

  if (dept.includes('phòng marketing') || dept.includes('marketing')) {
    if (pos.includes('trưởng') || pos.includes('leader')) return ROLE_CODES.TEAM_LEADER
    return ROLE_CODES.EMPLOYEE
  }

  return ROLE_CODES.EMPLOYEE
}

const getRoleGroupNames = (role, department) => {
  const roleCode = normalizeRoleCode(role)
  if (!roleCode) return []

  if (ROLE_GROUPS.director.includes(roleCode)) return ['director']
  if (ROLE_GROUPS.hr.includes(roleCode)) return ['hr']
  if (ROLE_GROUPS.finance.includes(roleCode)) return ['finance']
  if (ROLE_GROUPS.tech.includes(roleCode) && normalizeDepartment(department).includes('kỹ thuật')) return ['tech']
  if (ROLE_GROUPS.marketing.includes(roleCode) && normalizeDepartment(department).includes('marketing')) return ['marketing']
  if (ROLE_GROUPS.tech.includes(roleCode) && !normalizeDepartment(department)) return ['tech']

  // Fallback: include role based groups if direct
  const matched = Object.entries(ROLE_GROUPS).find(([, roles]) => roles.includes(roleCode))
  return matched ? [matched[0]] : []
}

const getDefaultRouteForRole = (role, department) => {
  const roleCode = normalizeRoleCode(role)
  const groups = getRoleGroupNames(roleCode, department)
  if (groups.includes('director')) return '/dashboard'
  if (groups.includes('hr')) return '/employees'
  if (groups.includes('finance')) return '/payroll'
  if (groups.includes('tech')) return '/work-reports'
  if (groups.includes('marketing')) return '/work-reports'
  return '/setting'
}

export {
  ROLE_CODES,
  ROLE_GROUPS,
  normalizeRoleCode,
  guessRoleCode,
  getRoleGroupNames,
  getDefaultRouteForRole
}
