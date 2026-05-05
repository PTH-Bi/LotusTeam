// Services/MultiPositionCvFilterService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LotusTeam.Data;
using LotusTeam.Models;
using System.Text.RegularExpressions;

namespace LotusTeam.Services
{
    public class MultiPositionCvFilterService : IMultiPositionCvFilterService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MultiPositionCvFilterService> _logger;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_POSITIONS = "active_job_positions";
        private const int CACHE_DURATION_MINUTES = 60;

        // Dictionary lưu từ khóa mặc định cho từng vị trí (fallback khi chưa có trong DB)
        private readonly Dictionary<string, (List<string> keywords, int minScore)> _defaultPositions = new()
        {
            { "Frontend Developer", (new List<string> { "react", "angular", "vue", "html", "css", "javascript", "typescript", "frontend", "ui/ux", "responsive" }, 40) },
            { "Backend Developer", (new List<string> { "c#", ".net", "java", "python", "node.js", "api", "microservices", "sql", "mongodb", "backend" }, 40) },
            { "Fullstack Developer", (new List<string> { "react", "angular", "c#", ".net", "node.js", "sql", "mongodb", "fullstack", "frontend", "backend" }, 50) },
            { "DevOps Engineer", (new List<string> { "docker", "kubernetes", "jenkins", "ci/cd", "aws", "azure", "linux", "terraform", "gitlab", "devops" }, 45) },
            { "Tester/QA", (new List<string> { "selenium", "test", "qa", "quality", "automation", "manual testing", "jira", "bug", "regression", "testing" }, 35) },
            { "Business Analyst", (new List<string> { "requirement", "use case", "uml", "jira", "confluence", "agile", "scrum", "documentation", "ba", "business analysis" }, 45) },
            { "Project Manager", (new List<string> { "project management", "agile", "scrum", "jira", "leadership", "planning", "risk management", "stakeholder", "pmp" }, 50) },
            { "Data Analyst", (new List<string> { "sql", "python", "excel", "power bi", "tableau", "data visualization", "statistics", "pandas", "data analysis" }, 40) },
            { "Data Scientist", (new List<string> { "python", "machine learning", "tensorflow", "pytorch", "sql", "statistics", "deep learning", "ai", "data science" }, 50) },
            { "Mobile Developer", (new List<string> { "android", "ios", "swift", "kotlin", "react native", "flutter", "mobile", "xamarin" }, 40) },
            { "System Administrator", (new List<string> { "linux", "windows server", "network", "active directory", "vpn", "firewall", "backup", "monitoring" }, 35) },
            { "Security Engineer", (new List<string> { "cybersecurity", "penetration testing", "firewall", "encryption", "vulnerability", "security audit", "iso 27001" }, 45) },
            { "UI/UX Designer", (new List<string> { "figma", "adobe xd", "sketch", "photoshop", "illustrator", "wireframe", "prototype", "user experience", "ui/ux" }, 40) },
            { "Product Manager", (new List<string> { "product management", "roadmap", "agile", "scrum", "market research", "user story", "product owner" }, 50) },
            { "HR/Recruiter", (new List<string> { "recruitment", "sourcing", "interviewing", "onboarding", "hr", "human resources", "talent acquisition", "hiring" }, 40) },
            { "Marketing Specialist", (new List<string> { "seo", "social media", "content marketing", "google ads", "facebook ads", "email marketing", "analytics" }, 40) },
            { "Sales Executive", (new List<string> { "sales", "business development", "negotiation", "crm", "lead generation", "closing", "b2b", "b2c" }, 40) },
            { "Accountant", (new List<string> { "accounting", "quickbooks", "tax", "financial reporting", "audit", "balance sheet", "general ledger" }, 40) },
            { "Administrative Assistant", (new List<string> { "office administration", "scheduling", "microsoft office", "communication", "organization", "customer service" }, 35) }
        };

        public MultiPositionCvFilterService(
            AppDbContext context,
            ILogger<MultiPositionCvFilterService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // =====================================================
        // TÍNH ĐIỂM KINH NGHIỆM
        // =====================================================

        private int CalculateExperienceScore(string cvText)
        {
            int experienceScore = 0;
            int yearsOfExperience = GetYearsOfExperience(cvText);

            // 1. Tính điểm dựa trên số năm kinh nghiệm
            if (yearsOfExperience >= 5)
                experienceScore = 25;  // 5+ năm: 25 điểm
            else if (yearsOfExperience >= 3)
                experienceScore = 20;  // 3-4 năm: 20 điểm
            else if (yearsOfExperience >= 1)
                experienceScore = 10;  // 1-2 năm: 10 điểm
            else if (yearsOfExperience > 0)
                experienceScore = 5;   // Dưới 1 năm: 5 điểm

            // 2. Tìm từ khóa cấp bậc senior
            if (Regex.IsMatch(cvText, @"\b(senior|lead|principal|expert|chuyên viên cao cấp|trưởng nhóm)\b", RegexOptions.IgnoreCase))
                experienceScore += 15;

            // 3. Trừ điểm nếu là junior/fresher
            if (Regex.IsMatch(cvText, @"\b(junior|fresher|intern|thực tập|mới ra trường)\b", RegexOptions.IgnoreCase))
                experienceScore -= 10;

            // 4. Bonus điểm cho số lượng dự án
            int projectCount = Regex.Matches(cvText, @"\b(project|dự án|developed|built|implemented|xây dựng|phát triển)\b", RegexOptions.IgnoreCase).Count;
            int projectBonus = Math.Min(projectCount * 2, 10);
            experienceScore += projectBonus;

            // 5. Bonus điểm cho công ty lớn
            if (Regex.IsMatch(cvText, @"\b(FPT|Viettel|VNG|Tiki|Shopee|Lazada|Google|Microsoft|Amazon|Meta)\b", RegexOptions.IgnoreCase))
                experienceScore += 10;

            return Math.Max(0, Math.Min(experienceScore, 40));
        }

        private int GetYearsOfExperience(string cvText)
        {
            var patterns = new[]
            {
                @"(\d+)\+?\s*năm\s+kinh\s+nghiệm",
                @"(\d+)\+?\s*years?\s+experience",
                @"experience\s+of\s+(\d+)\+?\s*years?",
                @"(\d+)\+?\s*năm\s+kinh\s+nghi?ệm",
                @"(\d+)\+?\s*years?"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(cvText.ToLower(), pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int years))
                {
                    return years;
                }
            }

            return 0;
        }

        // =====================================================
        // TÍNH ĐIỂM CHỨNG CHỈ & BẰNG CẤP
        // =====================================================

        private int CalculateCertificateScore(string cvText)
        {
            int score = 0;
            var certificates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // Cloud Certificates
                { "azure", 10 }, { "aws", 10 }, { "gcp", 8 }, { "cloud", 5 },
                // DevOps
                { "docker", 8 }, { "kubernetes", 8 }, { "terraform", 8 }, { "jenkins", 8 }, { "ci/cd", 8 },
                // Management
                { "pmp", 10 }, { "scrum master", 8 }, { "agile", 5 }, { "itil", 8 },
                // Security
                { "cissp", 10 }, { "ceh", 10 }, { "oscp", 10 }, { "security", 5 },
                // Language
                { "toeic", 5 }, { "ielts", 5 }, { "toefl", 5 }, { "chứng chỉ tiếng anh", 5 }
            };

            foreach (var cert in certificates)
            {
                if (cvText.Contains(cert.Key, StringComparison.OrdinalIgnoreCase))
                {
                    score += cert.Value;
                }
            }

            return Math.Min(score, 15);
        }

        private int CalculateEducationScore(string cvText)
        {
            int score = 0;

            // Tiến sĩ
            if (Regex.IsMatch(cvText, @"\b(doctor|tiến sĩ|phd|doctorate)\b", RegexOptions.IgnoreCase))
                score += 15;
            // Thạc sĩ
            else if (Regex.IsMatch(cvText, @"\b(master|thạc sĩ|msc|ma)\b", RegexOptions.IgnoreCase))
                score += 10;
            // Đại học
            else if (Regex.IsMatch(cvText, @"\b(bachelor|cử nhân|đại học|university)\b", RegexOptions.IgnoreCase))
                score += 8;
            // Cao đẳng
            else if (Regex.IsMatch(cvText, @"\b(college|cao đẳng)\b", RegexOptions.IgnoreCase))
                score += 5;

            // Tốt nghiệp loại giỏi
            if (Regex.IsMatch(cvText, @"\b(good|giỏi|distinction|honors)\b", RegexOptions.IgnoreCase))
                score += 5;

            // Tốt nghiệp xuất sắc
            if (Regex.IsMatch(cvText, @"\b(excellent|xuất sắc|very good)\b", RegexOptions.IgnoreCase))
                score += 8;

            return Math.Min(score, 20);
        }

        // =====================================================
        // TÍNH ĐIỂM CHO POSITION
        // =====================================================

        public async Task<PositionMatchResult?> CalculateScoreForPosition(int positionId, string cvText)
        {
            if (string.IsNullOrEmpty(cvText))
                return null;

            cvText = cvText.ToLower();

            var position = await _context.JobPositions
                .Include(p => p.JobSkills)
                .FirstOrDefaultAsync(p => p.JobPositionId == positionId && p.IsActive);

            if (position == null)
                return null;

            return await CalculateScoreForPositionFromDb(cvText, position);
        }

        public async Task<List<PositionMatchResult>> CalculateScoresForAllPositions(string cvText)
        {
            var results = new List<PositionMatchResult>();

            if (string.IsNullOrEmpty(cvText))
                return results;

            cvText = cvText.ToLower();

            // Lấy danh sách vị trí từ cache hoặc database
            var positions = await GetActivePositionsWithCacheAsync();

            if (positions.Any())
            {
                foreach (var position in positions)
                {
                    var result = await CalculateScoreForPositionFromDb(cvText, position);
                    results.Add(result);
                }
            }
            else
            {
                // Fallback: sử dụng default positions
                _logger.LogWarning("No positions found in database, using default positions as fallback");
                foreach (var defaultPos in _defaultPositions)
                {
                    var result = CalculateScoreForPositionFromDefault(cvText, defaultPos.Key, defaultPos.Value.keywords, defaultPos.Value.minScore);
                    results.Add(result);
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        private async Task<List<JobPosition>> GetActivePositionsWithCacheAsync()
        {
            if (_cache.TryGetValue(CACHE_KEY_POSITIONS, out List<JobPosition>? cachedPositions) && cachedPositions != null)
            {
                return cachedPositions;
            }

            var positions = await _context.JobPositions
                .Where(p => p.IsActive)
                .Include(p => p.JobSkills.Where(s => s.IsActive))
                .ToListAsync();

            if (positions.Any())
            {
                _cache.Set(CACHE_KEY_POSITIONS, positions, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }

            return positions;
        }

        public void InvalidatePositionCache()
        {
            _cache.Remove(CACHE_KEY_POSITIONS);
        }

        private async Task<PositionMatchResult> CalculateScoreForPositionFromDb(string cvText, JobPosition position)
        {
            int skillScore = 0;
            var matchedSkills = new List<string>();
            var missingRequiredSkills = new List<string>();
            int requiredSkillCount = position.JobSkills.Count(s => s.IsRequired && s.IsActive);
            int matchedRequiredCount = 0;

            // 1. Điểm kỹ năng (tối đa 60 điểm)
            foreach (var skill in position.JobSkills.Where(s => s.IsActive))
            {
                if (cvText.Contains(skill.SkillName.ToLower()))
                {
                    skillScore += skill.Weight;
                    matchedSkills.Add(skill.SkillName);
                    if (skill.IsRequired) matchedRequiredCount++;
                }
                else if (skill.IsRequired)
                {
                    missingRequiredSkills.Add(skill.SkillName);
                }
            }

            // 2. Điểm kinh nghiệm (tối đa 40 điểm)
            int experienceScore = CalculateExperienceScore(cvText);
            int experienceYears = GetYearsOfExperience(cvText);

            // 3. Điểm chứng chỉ (tối đa 15 điểm)
            int certificateScore = CalculateCertificateScore(cvText);

            // 4. Điểm bằng cấp (tối đa 20 điểm)
            int educationScore = CalculateEducationScore(cvText);

            // 5. Bonus điểm nếu có tên vị trí trong CV (5 điểm)
            int positionNameBonus = cvText.Contains(position.PositionName.ToLower()) ? 5 : 0;

            // Tổng điểm
            int totalScore = skillScore + experienceScore + certificateScore + educationScore + positionNameBonus;

            // 6. Điều chỉnh dựa trên tỷ lệ kỹ năng bắt buộc
            if (requiredSkillCount > 0)
            {
                double requiredRatio = (double)matchedRequiredCount / requiredSkillCount;
                if (requiredRatio >= 0.8)
                {
                    totalScore += 10; // Bonus nếu đạt 80% kỹ năng bắt buộc
                }
                else if (requiredRatio < 0.5)
                {
                    totalScore -= 15; // Phạt nếu thiếu quá 50% kỹ năng bắt buộc
                }
            }

            totalScore = Math.Max(0, Math.Min(totalScore, 100));

            // Log chi tiết
            _logger.LogDebug(@"
Position: {PositionName}
  - Skill Score: {SkillScore}
  - Experience: {ExpYears} năm = {ExpScore} điểm
  - Certificate: {CertScore} điểm
  - Education: {EduScore} điểm
  - Bonus: {Bonus} điểm
  - TOTAL: {TotalScore} điểm",
                position.PositionName, skillScore, experienceYears, experienceScore,
                certificateScore, educationScore, positionNameBonus, totalScore);

            return new PositionMatchResult
            {
                PositionId = position.JobPositionId,
                PositionName = position.PositionName,
                Description = position.Description,
                Score = totalScore,
                IsSuitable = totalScore >= position.MinScoreRequired && matchedRequiredCount == requiredSkillCount,
                MinScoreRequired = position.MinScoreRequired,
                MatchedSkills = matchedSkills,
                MissingRequiredSkills = missingRequiredSkills,
                ExperienceYears = experienceYears,
                ExperienceScore = experienceScore,
                CertificateScore = certificateScore,
                EducationScore = educationScore
            };
        }

        private PositionMatchResult CalculateScoreForPositionFromDefault(string cvText, string positionName, List<string> keywords, int minScore)
        {
            int skillScore = 0;
            var matchedSkills = new List<string>();

            foreach (var keyword in keywords)
            {
                if (cvText.Contains(keyword.ToLower()))
                {
                    skillScore += 20;
                    matchedSkills.Add(keyword);
                }
            }

            int experienceScore = CalculateExperienceScore(cvText);
            int experienceYears = GetYearsOfExperience(cvText);
            int certificateScore = CalculateCertificateScore(cvText);
            int educationScore = CalculateEducationScore(cvText);
            int positionNameBonus = cvText.Contains(positionName.ToLower()) ? 5 : 0;

            int totalScore = skillScore + experienceScore + certificateScore + educationScore + positionNameBonus;
            totalScore = Math.Min(totalScore, 100);

            return new PositionMatchResult
            {
                PositionId = null,
                PositionName = positionName,
                Score = totalScore,
                IsSuitable = totalScore >= minScore,
                MinScoreRequired = minScore,
                MatchedSkills = matchedSkills,
                MissingRequiredSkills = new List<string>(),
                ExperienceYears = experienceYears,
                ExperienceScore = experienceScore,
                CertificateScore = certificateScore,
                EducationScore = educationScore
            };
        }

        // =====================================================
        // CÁC METHOD KHÁC
        // =====================================================

        public async Task<PositionMatchResult?> GetBestMatchPosition(string cvText)
        {
            var allScores = await CalculateScoresForAllPositions(cvText);
            return allScores.FirstOrDefault();
        }

        public async Task<List<CandidateWithPositionMatch>> GetCandidatesByPosition(string positionName, int minScore = 40)
        {
            var matches = await _context.CandidatePositionMatches
                .Include(m => m.Candidate)
                .Include(m => m.JobPosition)
                .Where(m => m.JobPosition.PositionName == positionName && m.TotalScore >= minScore)
                .OrderByDescending(m => m.TotalScore)
                .Select(m => new CandidateWithPositionMatch
                {
                    CandidateId = m.CandidateId,
                    FullName = m.Candidate.FullName,
                    Email = m.Candidate.Email,
                    Phone = m.Candidate.Phone,
                    PositionName = m.JobPosition.PositionName,
                    Score = m.TotalScore,
                    MatchedSkills = m.MatchedSkills,
                    AppliedDate = m.Candidate.AppliedDate
                })
                .ToListAsync();

            return matches;
        }

        public async Task<JobPosition?> CreateJobPosition(JobPositionDto dto)
        {
            try
            {
                var position = new JobPosition
                {
                    PositionName = dto.PositionName,
                    Description = dto.Description,
                    MinScoreRequired = dto.MinScoreRequired,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.JobPositions.Add(position);
                await _context.SaveChangesAsync();

                foreach (var skillDto in dto.Skills)
                {
                    var skill = new JobSkill
                    {
                        JobPositionId = position.JobPositionId,
                        SkillName = skillDto.SkillName,
                        Weight = skillDto.Weight,
                        IsRequired = skillDto.IsRequired,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.JobSkills.Add(skill);
                }

                await _context.SaveChangesAsync();
                InvalidatePositionCache();
                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job position");
                return null;
            }
        }

        public async Task<JobSkill?> AddSkillToPosition(int positionId, JobSkillDto dto)
        {
            try
            {
                var position = await _context.JobPositions.FindAsync(positionId);
                if (position == null) return null;

                var skill = new JobSkill
                {
                    JobPositionId = positionId,
                    SkillName = dto.SkillName,
                    Weight = dto.Weight,
                    IsRequired = dto.IsRequired,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobSkills.Add(skill);
                await _context.SaveChangesAsync();
                InvalidatePositionCache();
                return skill;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skill to position");
                return null;
            }
        }

        public async Task<List<JobPositionDto>> GetAllActivePositions()
        {
            var positions = await _context.JobPositions
                .Where(p => p.IsActive)
                .Include(p => p.JobSkills)
                .Select(p => new JobPositionDto
                {
                    JobPositionId = p.JobPositionId,
                    PositionName = p.PositionName,
                    Description = p.Description,
                    MinScoreRequired = p.MinScoreRequired,
                    IsActive = p.IsActive,
                    Skills = p.JobSkills.Where(s => s.IsActive).Select(s => new JobSkillDto
                    {
                        JobSkillId = s.JobSkillId,
                        SkillName = s.SkillName,
                        Weight = s.Weight,
                        IsRequired = s.IsRequired
                    }).ToList()
                })
                .ToListAsync();

            return positions;
        }

        public async Task<PositionStatisticsDto> GetPositionStatistics()
        {
            var stats = new PositionStatisticsDto();

            var positions = await _context.JobPositions
                .Where(p => p.IsActive)
                .ToListAsync();

            stats.TotalPositions = positions.Count;
            stats.TotalCandidates = await _context.Candidates.CountAsync();

            foreach (var position in positions)
            {
                var matches = await _context.CandidatePositionMatches
                    .Where(m => m.JobPositionId == position.JobPositionId)
                    .ToListAsync();

                stats.PositionStats.Add(new PositionStats
                {
                    PositionName = position.PositionName,
                    TotalCandidates = matches.Count,
                    SuitableCandidates = matches.Count(m => m.IsSuitable),
                    AverageScore = matches.Any() ? matches.Average(m => m.TotalScore) : 0
                });
            }

            return stats;
        }

        public async Task InitializeDefaultPositionsAsync()
        {
            if (await _context.JobPositions.AnyAsync())
                return;

            foreach (var defaultPos in _defaultPositions)
            {
                var position = new JobPosition
                {
                    PositionName = defaultPos.Key,
                    Description = $"Position for {defaultPos.Key}",
                    MinScoreRequired = defaultPos.Value.minScore,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobPositions.Add(position);
                await _context.SaveChangesAsync();

                foreach (var keyword in defaultPos.Value.keywords)
                {
                    var skill = new JobSkill
                    {
                        JobPositionId = position.JobPositionId,
                        SkillName = keyword,
                        Weight = 20,
                        IsRequired = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.JobSkills.Add(skill);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Default job positions initialized");
        }
    }
}