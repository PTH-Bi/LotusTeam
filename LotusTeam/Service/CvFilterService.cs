namespace LotusTeam.Services
{
    public class CvFilterService
    {
        private readonly string[] skills =
        {
            "c#",
            ".net",
            "sql",
            "react",
            "angular"
        };

        public int CalculateScore(string cvText)
        {
            if (string.IsNullOrEmpty(cvText)) return 0;

            cvText = cvText.ToLower();

            int score = 0;

            foreach (var skill in skills)
            {
                if (cvText.Contains(skill))
                {
                    score += 20;
                }
            }

            return score;
        }

        public bool IsSuitable(int score)
        {
            return score >= 40;
        }
    }
}