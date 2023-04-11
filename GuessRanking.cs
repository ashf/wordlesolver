namespace WordleSolver;

public class GuessRanking
{
    public int MaxScore = int.MaxValue;
    public int AverageScore = int.MaxValue;
    public int BestScore = int.MaxValue;
    public int Index = 0;

    public bool IsLessThan(GuessRanking rhs)
    {
        if (MaxScore < rhs.MaxScore) return true;
        if (MaxScore > rhs.MaxScore) return false;

        if (AverageScore < rhs.AverageScore) return true;
        if (AverageScore > rhs.AverageScore) return false;

        if (BestScore < rhs.BestScore) return true;
        if (BestScore > rhs.BestScore) return false;

        return Index < rhs.Index;
    }
};