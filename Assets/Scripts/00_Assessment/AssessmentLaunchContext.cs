// AssessmentLaunchContext.cs
// ✅ NEW helper (one-time overrides) so different buttons can launch different assessment JSON + rewards + hub spawn override
// Put this anywhere in your project (ex: Assets/Scripts/Assessment/AssessmentLaunchContext.cs)

using System;

public static class AssessmentLaunchContext
{
    public struct LaunchData
    {
        public string jsonFileName;            // ex: "Assessment2_questions.json"
        public string completionRewardId;      // ex: "CompleteAssessment2"
        public string perfectRewardId;         // ex: "PerfectAssessment2"
        public string chapterCompletionRewardId; // ex: "CH2_COMPLETE"

        public bool useHubSpawnOverrideOnReturn;
        public string hubSpawnPointNameOnReturn; // ex: "Assessment_Return"
    }

    private static bool _hasData;
    private static LaunchData _data;

    public static bool hasData => _hasData;

    public static void Set(
        string jsonFileName,
        string completionRewardId,
        string perfectRewardId,
        string chapterCompletionRewardId = "",
        bool useHubSpawnOverrideOnReturn = false,
        string hubSpawnPointNameOnReturn = ""
    )
    {
        _data = new LaunchData
        {
            jsonFileName = jsonFileName ?? "",
            completionRewardId = completionRewardId ?? "",
            perfectRewardId = perfectRewardId ?? "",
            chapterCompletionRewardId = chapterCompletionRewardId ?? "",
            useHubSpawnOverrideOnReturn = useHubSpawnOverrideOnReturn,
            hubSpawnPointNameOnReturn = hubSpawnPointNameOnReturn ?? ""
        };

        _hasData = true;
    }

    public static bool TryConsume(out LaunchData data)
    {
        if (!_hasData)
        {
            data = default;
            return false;
        }

        data = _data;

        // one-time consume
        _hasData = false;
        _data = default;

        return true;
    }

    public static void Clear()
    {
        _hasData = false;
        _data = default;
    }
}