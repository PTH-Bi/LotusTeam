using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig.Content;

namespace LotusTeam.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        #region DbSets - Employees Module
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Gender> Genders => Set<Gender>();
        public DbSet<Employees> Employees => Set<Employees>();
        public DbSet<JobHistory> JobHistories => Set<JobHistory>();
        public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
        public DbSet<Skill> Skills => Set<Skill>();
        #endregion

       
        public DbSet<CompanyInfo> CompanyInfos { get; set; }

        public DbSet<GoogleTokens> GoogleTokens { get; set; }

        public DbSet<ProcessedEmails> ProcessedEmails { get; set; }

        public DbSet<CandidateCVs> CandidateCVs { get; set; }

        public DbSet<RefreshTokens> RefreshTokens { get; set; }

        public DbSet<ChatLogs> ChatLogs { get; set; }
        public DbSet<FaceAttendances> FaceAttendances { get; set; }

        public DbSet<EmployeeFaces> EmployeeFaces { get; set; }

        public DbSet<Allowances> Allowances { get; set; }

        public DbSet<Bonus> Bonuses { get; set; }

        public DbSet<Deduction> Deductions { get; set; }

        public DbSet<Dependent> Dependents { get; set; }

        public DbSet<DependentAllowance> DependentAllowances { get; set; }

        public DbSet<RemoteAttendances> RemoteAttendances { get; set; }

        public DbSet<JobPosition> JobPositions { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<CandidatePositionMatch> CandidatePositionMatches { get; set; }


        #region DbSets - Bank
        public DbSet<BankPartner> BankPartners { get; set; }
        public DbSet<CompanyBankAccounts> CompanyBankAccounts { get; set; }
        public DbSet<PayrollBankTransfers> PayrollBankTransfers { get; set; }
        public DbSet<PayrollTransferDetails> PayrollTransferDetails { get; set; }
        #endregion

        #region DbSets - Contracts Module
        public DbSet<ContractType> ContractTypes => Set<ContractType>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<ContractTemplates> ContractTemplates => Set<ContractTemplates>();
        #endregion

        #region DbSets - Leave & Attendance Module
        public DbSet<LeaveBalances> LeaveBalances { get; set; }
        public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<WorkSchedules> WorkSchedules => Set<WorkSchedules>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<WorkType> WorkTypes => Set<WorkType>();
        public DbSet<Attendances> Attendances => Set<Attendances>();
        public DbSet<AttendanceOvertime> AttendanceOvertimes => Set<AttendanceOvertime>();
        public DbSet<OvertimeRule> OvertimeRules => Set<OvertimeRule>();
        public DbSet<OvertimeRequests> OvertimeRequests => Set<OvertimeRequests>();
        public DbSet<Holiday> Holidays => Set<Holiday>();
        public DbSet<ShiftTask> ShiftTasks => Set<ShiftTask>();
        public DbSet<MealRequests> MealRequests => Set<MealRequests>();
        #endregion

        #region DbSets - Payroll Module
        public DbSet<Payrolls> Payrolls => Set<Payrolls>();
        public DbSet<PayrollComponents> PayrollComponents => Set<PayrollComponents>();
        public DbSet<PayrollDetails> PayrollDetails => Set<PayrollDetails>();
        public DbSet<PayrollTaxSnapshot> PayrollTaxSnapshots => Set<PayrollTaxSnapshot>();
        public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();
        public DbSet<SalaryPromotionRequests> SalaryPromotionRequests => Set<SalaryPromotionRequests>();
        #endregion

        #region DbSets - Performance & Development
        public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
        public DbSet<Courses> Courses => Set<Courses>();
        public DbSet<EmployeeCourses> EmployeeCourses => Set<EmployeeCourses>();
        public DbSet<OKRs> OKRs => Set<OKRs>();
        public DbSet<KeyResults> KeyResults => Set<KeyResults>();
        public DbSet<KPIs> KPIs => Set<KPIs>();
        public DbSet<EmployeeKPIs> EmployeeKPIs => Set<EmployeeKPIs>();
        #endregion

        #region DbSets - Authentication & Authorization
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRoles> UserRoles => Set<UserRoles>();
        public DbSet<RolePermissions> RolePermissions => Set<RolePermissions>();
        #endregion

        #region DbSets - Assets & Benefits
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<AssetStatus> AssetStatuses => Set<AssetStatus>();
        public DbSet<EmployeeAsset> EmployeeAssets => Set<EmployeeAsset>();
        public DbSet<AssetIncidents> AssetIncidents => Set<AssetIncidents>();
        public DbSet<Benefit> Benefits => Set<Benefit>();
        #endregion

        #region DbSets - Projects & Workflow
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
        public DbSet<Workflows> Workflows => Set<Workflows>();
        public DbSet<WorkflowSteps> WorkflowSteps => Set<WorkflowSteps>();
        public DbSet<WorkReport> WorkReports { get; set; }
        #endregion

        #region DbSets - Recruitment
        public DbSet<Candidates> Candidates => Set<Candidates>();
        #endregion

        #region DbSets - System & Management
        public DbSet<StatusMasters> StatusMasters => Set<StatusMasters>();
        public DbSet<Requests> Requests => Set<Requests>();
        public DbSet<RewardsDisciplines> RewardsDisciplines => Set<RewardsDisciplines>();
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AuditSoftDelete> AuditSoftDeletes => Set<AuditSoftDelete>();
        public DbSet<InternalAnnouncement> InternalAnnouncements => Set<InternalAnnouncement>();
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
        public DbSet<Document> Documents => Set<Document>();
        
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CandidatePositionMatch>()
                .HasIndex(c => new { c.CandidateId, c.JobPositionId, c.CandidateCVID })
                .IsUnique();

            modelBuilder.Entity<JobSkill>()
                .HasIndex(j => new { j.JobPositionId, j.SkillName })
                .IsUnique();

            modelBuilder.Entity<Dependent>(entity =>
            {
                entity.HasKey(e => e.DependentID);
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Relationship).HasMaxLength(50);
                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DependentAllowance>(entity =>
            {
                entity.HasKey(e => e.DependentAllowanceID);
                entity.Property(e => e.AmountPerDependent).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeID);
                entity.HasOne(e => e.Payroll)
                      .WithMany(p => p.DependentAllowances)
                      .HasForeignKey(e => e.PayrollID);
            });

            modelBuilder.Entity<Allowances>(entity =>
            {
                entity.HasKey(e => e.AllowanceID);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeID);
                entity.HasOne(e => e.Payroll).WithMany(p => p.Allowances).HasForeignKey(e => e.PayrollID);
            });

            modelBuilder.Entity<Bonus>(entity =>
            {
                entity.HasKey(e => e.BonusID);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeID);
                entity.HasOne(e => e.Payroll).WithMany(p => p.Bonuses).HasForeignKey(e => e.PayrollID);
            });

            modelBuilder.Entity<Deduction>(entity =>
            {
                entity.HasKey(e => e.DeductionID);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeID);
                entity.HasOne(e => e.Payroll).WithMany(p => p.Deductions).HasForeignKey(e => e.PayrollID);
            });

            /* ================== PRIMARY KEY CONFIGURATIONS ================== */
            // Employees Module
            modelBuilder.Entity<Department>()
                .HasKey(x => x.DepartmentID);

            modelBuilder.Entity<Position>()
                .HasKey(x => x.PositionID);

            modelBuilder.Entity<Gender>()
                .HasKey(x => x.GenderID);

            modelBuilder.Entity<Employees>()
                .HasKey(x => x.EmployeeID);

            modelBuilder.Entity<JobHistory>()
                .HasKey(x => x.HistoryID);

            modelBuilder.Entity<EmployeeSkill>()
                .HasKey(x => x.EmployeeSkillID);

            modelBuilder.Entity<Skill>()
                .HasKey(x => x.SkillID);

            modelBuilder.Entity<EmployeeFaces>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .HasPrincipalKey(e => e.EmployeeID);

            // Contracts Module
            modelBuilder.Entity<ContractType>()
                .HasKey(x => x.ContractTypeID);

            modelBuilder.Entity<Contract>()
                .HasKey(x => x.ContractID);

            modelBuilder.Entity<ContractTemplates>()
                .HasKey(x => x.TemplateId);

            // Leave & Attendance Module
            modelBuilder.Entity<LeaveType>()
                .HasKey(x => x.LeaveTypeID);

            modelBuilder.Entity<LeaveRequest>()
                .HasKey(x => x.LeaveID);

            modelBuilder.Entity<WorkSchedules>()
                .HasKey(x => x.ScheduleId);

            modelBuilder.Entity<Shift>()
                .HasKey(x => x.ShiftID);

            modelBuilder.Entity<WorkType>()
                .HasKey(x => x.WorkTypeID);

            modelBuilder.Entity<Attendances>()
                .HasKey(x => x.AttendanceID);

            modelBuilder.Entity<AttendanceOvertime>()
                .HasKey(x => x.AttendanceOvertimeID);

            modelBuilder.Entity<OvertimeRule>()
                .HasKey(x => x.RuleID);

            modelBuilder.Entity<OvertimeRequests>()
                .HasKey(x => x.OvertimeRequestId);

            modelBuilder.Entity<Holiday>()
                .HasKey(x => x.HolidayID);

            modelBuilder.Entity<ShiftTask>()
                .HasKey(x => x.TaskId);

            modelBuilder.Entity<MealRequests>()
                .HasKey(x => x.MealRequestId);

            // Payroll Module
            modelBuilder.Entity<Payrolls>()
                .HasKey(x => x.PayrollID);

            modelBuilder.Entity<PayrollComponents>()
                .HasKey(x => x.PayrollComponentID);

            modelBuilder.Entity<PayrollDetails>()
                .HasKey(x => x.PayrollDetailID);

            modelBuilder.Entity<PayrollTaxSnapshot>()
                .HasKey(x => x.SnapshotID);

            modelBuilder.Entity<TaxBracket>()
                .HasKey(x => x.TaxBracketID);

            modelBuilder.Entity<SalaryPromotionRequests>()
                .HasKey(x => x.PromotionRequestId);

            // Performance & Development
            modelBuilder.Entity<PerformanceReview>()
                .HasKey(x => x.ReviewId);

            modelBuilder.Entity<Training>()
                .HasKey(x => x.TrainingID);

            modelBuilder.Entity<EmployeeTraining>()
                .HasKey(x => x.EmployeeTrainingID);

            modelBuilder.Entity<Courses>()
                .HasKey(x => x.CourseId);

            modelBuilder.Entity<EmployeeCourses>()
                .HasKey(x => x.EnrollmentId);

            modelBuilder.Entity<OKRs>()
                .HasKey(x => x.OkrId);

            modelBuilder.Entity<KeyResults>()
                .HasKey(x => x.KrId);

            modelBuilder.Entity<KPIs>()
                .HasKey(x => x.KpiId);

            modelBuilder.Entity<EmployeeKPIs>()
                .HasKey(x => x.EmployeeKpiId);

            // Authentication & Authorization
            modelBuilder.Entity<User>()
                .HasKey(x => x.UserID);

            modelBuilder.Entity<Role>()
                .HasKey(x => x.RoleID);

            modelBuilder.Entity<Permission>()
                .HasKey(x => x.PermissionID);

            modelBuilder.Entity<UserRoles>()
                .HasKey(x => x.UserRoleID);

            modelBuilder.Entity<RolePermissions>()
                .HasKey(x => x.RolePermissionID);

            // Assets & Benefits
            modelBuilder.Entity<Asset>()
                .HasKey(x => x.AssetId);

            modelBuilder.Entity<AssetStatus>()
                .HasKey(x => x.StatusId);

            modelBuilder.Entity<EmployeeAsset>()
                .HasKey(x => x.AssignmentID); // Dựa trên EmployeeAsset có AssignmentID

            modelBuilder.Entity<AssetIncidents>()
                .HasKey(x => x.IncidentId);

            modelBuilder.Entity<Benefit>()
                .HasKey(x => x.BenefitID);

            // Projects & Workflow
            modelBuilder.Entity<Project>()
                .HasKey(x => x.ProjectID);

            modelBuilder.Entity<ProjectAssignment>()
                .HasKey(x => x.AssignmentID);

            modelBuilder.Entity<Workflows>()
                .HasKey(x => x.WorkflowId);

            modelBuilder.Entity<WorkflowSteps>()
                .HasKey(x => x.StepId);

            // Recruitment
            modelBuilder.Entity<Candidates>()
                .HasKey(x => x.CandidateId);

            // System & Management
            modelBuilder.Entity<StatusMasters>()
                .HasKey(x => x.StatusID);

            modelBuilder.Entity<Requests>()
                .HasKey(x => x.RequestId);

            modelBuilder.Entity<RewardsDisciplines>()
                .HasKey(x => x.RDID);

            modelBuilder.Entity<SystemConfig>()
                .HasKey(x => x.ConfigID);

            modelBuilder.Entity<AuditLog>()
                .HasKey(x => x.LogID);

            modelBuilder.Entity<AuditSoftDelete>()
                .HasKey(x => x.AuditId);

            modelBuilder.Entity<InternalAnnouncement>()
                .HasKey(x => x.AnnouncementId);

            modelBuilder.Entity<Survey>()
                .HasKey(x => x.SurveyID);

            modelBuilder.Entity<SurveyResponse>()
                .HasKey(x => x.ResponseID);

            modelBuilder.Entity<Document>()
                .HasKey(x => x.DocumentId);

            /* ================== UNIQUE CONSTRAINTS ================== */

            modelBuilder.Entity<Department>()
                .HasIndex(x => x.DepartmentCode)
                .IsUnique();

            modelBuilder.Entity<Position>()
                .HasIndex(x => x.PositionCode)
                .IsUnique();

            modelBuilder.Entity<Gender>()
                .HasIndex(x => x.GenderCode)
                .IsUnique();

            modelBuilder.Entity<Employees>()
                .HasIndex(x => x.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Employees>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<ContractType>()
                .HasIndex(x => x.ContractTypeCode)
                .IsUnique();

            modelBuilder.Entity<Contract>()
                .HasIndex(x => x.ContractCode)
                .IsUnique();

            modelBuilder.Entity<LeaveType>()
                .HasIndex(x => x.LeaveTypeCode)
                .IsUnique();

            modelBuilder.Entity<StatusMasters>()
                .HasIndex(x => new { x.StatusCode, x.Module })
                .IsUnique();

            modelBuilder.Entity<Shift>()
                .HasIndex(x => x.ShiftCode)
                .IsUnique();

            modelBuilder.Entity<WorkType>()
                .HasIndex(x => x.WorkTypeCode)
                .IsUnique();

            modelBuilder.Entity<OvertimeRule>()
                .HasIndex(x => x.RuleCode)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(x => x.RoleCode)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(x => x.PermissionCode)
                .IsUnique();

            modelBuilder.Entity<PayrollComponents>()
                .HasIndex(x => x.ComponentCode)
                .IsUnique();

            modelBuilder.Entity<Training>()
                .HasIndex(x => x.TrainingName)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(x => x.AssetCode)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(x => x.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<AssetStatus>()
                .HasIndex(x => x.StatusCode)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasIndex(x => x.ProjectCode)
                .IsUnique();

            modelBuilder.Entity<SystemConfig>()
                .HasIndex(x => x.ConfigKey)
                .IsUnique();

            modelBuilder.Entity<Skill>()
                .HasIndex(x => x.SkillName)
                .IsUnique();

            // New entities
            modelBuilder.Entity<ContractTemplates>()
                .HasIndex(x => x.TemplateCode)
                .IsUnique();

            modelBuilder.Entity<Workflows>()
                .HasIndex(x => x.WorkflowCode)
                .IsUnique();

            modelBuilder.Entity<KPIs>()
                .HasIndex(x => x.KpiCode)
                .IsUnique();

            modelBuilder.Entity<Courses>()
                .HasIndex(x => x.CourseCode)
                .IsUnique();

            modelBuilder.Entity<Candidates>()
                .HasIndex(x => x.Email)
                .IsUnique();

            /* ================== COMPOSITE UNIQUE CONSTRAINTS ================== */

            modelBuilder.Entity<Attendances>()
                .HasIndex(x => new { x.EmployeeID, x.WorkDate })
                .IsUnique();

            modelBuilder.Entity<UserRoles>()
                .HasIndex(x => new { x.UserID, x.RoleID })
                .IsUnique();

            modelBuilder.Entity<RolePermissions>()
                .HasIndex(x => new { x.RoleID, x.PermissionID })
                .IsUnique();

            modelBuilder.Entity<EmployeeTraining>()
                .HasIndex(x => new { x.EmployeeID, x.TrainingID })
                .IsUnique();

            modelBuilder.Entity<Holiday>()
                .HasIndex(x => x.HolidayDate)
                .IsUnique();

            modelBuilder.Entity<Payrolls>()
                .HasIndex(x => new { x.EmployeeID, x.PayPeriod })
                .IsUnique();

            modelBuilder.Entity<EmployeeAsset>()
                .HasIndex(x => new { x.EmployeeID, x.AssetID })
                .IsUnique();

            modelBuilder.Entity<ProjectAssignment>()
                .HasIndex(x => new { x.EmployeeID, x.ProjectID })
                .IsUnique();

            modelBuilder.Entity<EmployeeCourses>()
                .HasIndex(x => new { x.EmployeeId, x.CourseId })
                .IsUnique();

            modelBuilder.Entity<EmployeeKPIs>()
                .HasIndex(x => new { x.EmployeeId, x.KpiId, x.Period })
                .IsUnique();

            modelBuilder.Entity<WorkSchedules>()
                .HasIndex(x => new { x.EmployeeId, x.WorkDate })
                .IsUnique();

            modelBuilder.Entity<MealRequests>()
                .HasIndex(x => new { x.EmployeeId, x.RequestDate, x.MealType })
                .IsUnique();

            /* ================== EMPLOYEE SKILL CONFIGURATION ================== */

            modelBuilder.Entity<EmployeeSkill>(entity =>
            {
                // EmployeeSkill -> Employee (nhân viên sở hữu kỹ năng)
                entity.HasOne(es => es.Employee)
                      .WithMany() // Employee không cần navigation ngược
                      .HasForeignKey(es => es.EmployeeID)
                      .OnDelete(DeleteBehavior.Restrict);

                // EmployeeSkill -> Skill
                entity.HasOne(es => es.Skill)
                      .WithMany()
                      .HasForeignKey(es => es.SkillID)
                      .OnDelete(DeleteBehavior.Cascade);

                // EmployeeSkill -> Employee (người xác nhận)
                entity.HasOne(es => es.Verifier)
                      .WithMany()
                      .HasForeignKey(es => es.VerifiedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            /* ================== SIMPLIFIED RELATIONSHIPS ================== */

            // Employee relationships
            modelBuilder.Entity<Employees>()
                .HasMany(e => e.Contracts)
                .WithOne(c => c.Employee)
                .HasForeignKey(c => c.EmployeeID);

            modelBuilder.Entity<Employees>()
                .HasMany(e => e.Attendances)
                .WithOne(a => a.Employee)
                .HasForeignKey(a => a.EmployeeID);

            modelBuilder.Entity<Employees>()
                .HasMany(e => e.LeaveRequests)
                .WithOne(l => l.Employee)
                .HasForeignKey(l => l.EmployeeID);

            // Attendance relationships
            modelBuilder.Entity<Attendances>()
                .HasMany(a => a.AttendanceOvertimes)
                .WithOne(o => o.Attendance)
                .HasForeignKey(o => o.AttendanceID);

            // Payroll relationships
            modelBuilder.Entity<Payrolls>()
                .HasMany(p => p.PayrollDetails)
                .WithOne(d => d.Payroll)
                .HasForeignKey(d => d.PayrollID);

            // Project relationships
            modelBuilder.Entity<Project>()
                .HasMany(p => p.ProjectAssignments)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectID);

            /* ================== PRECISION CONFIGURATIONS ================== */

            // Employee
            modelBuilder.Entity<Employees>()
                .Property(e => e.BaseSalary)
                .HasPrecision(18, 2);

            // Contract
            modelBuilder.Entity<Contract>()
                .Property(c => c.Salary)
                .HasPrecision(18, 2);

            // Attendance
            modelBuilder.Entity<Attendances>()
                .Property(a => a.WorkingHours)
                .HasPrecision(5, 2);

            modelBuilder.Entity<AttendanceOvertime>()
                .Property(a => a.OvertimeHours)
                .HasPrecision(4, 2);

            modelBuilder.Entity<AttendanceOvertime>()
                .Property(a => a.CalculatedAmount)
                .HasPrecision(18, 2);

            // Payroll
            modelBuilder.Entity<Payrolls>()
                .Property(p => p.GrossSalary)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payrolls>()
                .Property(p => p.TotalDeductions)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payrolls>()
                .Property(p => p.NetSalary)
                .HasPrecision(18, 2);

            // Payroll Details
            modelBuilder.Entity<PayrollDetails>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // Asset
            modelBuilder.Entity<Asset>()
                .Property(a => a.Cost)
                .HasPrecision(18, 2);

            /* ================== DEFAULT VALUES ================== */

            modelBuilder.Entity<Employees>()
                .Property(e => e.Status)
                .HasDefaultValue((short)1);

            modelBuilder.Entity<Employees>()
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Department>()
                .Property(d => d.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Attendances>()
                .Property(a => a.LateMinutes)
                .HasDefaultValue(0);

            modelBuilder.Entity<Attendances>()
                .Property(a => a.EarlyLeaveMinutes)
                .HasDefaultValue(0);

            modelBuilder.Entity<Attendances>()
                .Property(a => a.IsHoliday)
                .HasDefaultValue(false);

            modelBuilder.Entity<Attendances>()
                .Property(a => a.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Asset>()
                .Property(a => a.Status)
                .HasDefaultValue((short)1);

            /* ================== AUDIT LOG CONFIGURATION ================== */

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(x => x.LogID);
                entity.ToTable("AuditLog");
            });

            /* ================== FIX STATUS ID RELATIONSHIPS ================== */

            // Attendance -> StatusMaster
            modelBuilder.Entity<Attendances>()
                .HasOne(a => a.Status)
                .WithMany()
                .HasForeignKey(a => a.StatusID)
                .HasPrincipalKey(s => s.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest -> StatusMaster  
            modelBuilder.Entity<LeaveRequest>()
                .HasOne<StatusMasters>()
                .WithMany()
                .HasForeignKey(l => l.StatusID)
                .HasPrincipalKey(s => s.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // SalaryPromotionRequest -> StatusMaster
            modelBuilder.Entity<SalaryPromotionRequests>()
                .HasOne(s => s.Status)
                .WithMany()
                .HasForeignKey(s => s.StatusId)
                .HasPrincipalKey(s => s.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee -> StatusMaster (nếu cần)
            modelBuilder.Entity<Employees>()
                .HasOne<StatusMasters>()
                .WithMany()
                .HasForeignKey(e => e.Status)
                .HasPrincipalKey(s => s.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // Payroll -> StatusMaster
            modelBuilder.Entity<Payrolls>()
                .HasOne(p => p.Status)
                .WithMany()
                .HasForeignKey(p => p.StatusID)
                .HasPrincipalKey(s => s.StatusID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}