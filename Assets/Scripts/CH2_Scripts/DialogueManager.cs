using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string portraitName;
    public string text;

    public DialogueLine(string speaker, string portraitName, string text)
    {
        this.speaker = speaker;
        this.portraitName = portraitName;
        this.text = text;
    }
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public Button dialogueButton;
    public GameObject gameplayRoot;

    [Header("All Available Portrait Sprites")]
    public Sprite[] allPortraitSprites;

    [Header("Portrait Lookup Behavior")]
    public bool allowContainsFallback = true;
    public bool verboseLogs = true;

    [Header("Portrait Aliases")]
    public bool supportFlippedUnderscoreFormat = true;
    public bool supportAllCapsCharacterFormat = true;

    [Header("Portrait Typo Fixes")]
    public bool autoFixCommonTypos = true;

    [Header("Advance Input (New Input System)")]
    public bool useEToAdvance = true;
    public bool allowButtonClick = true;

    [Header("Advance SFX")]
    [Tooltip("Optional: sound played every time you advance a line.")]
    public AudioSource sfxSource;

    public AudioClip advanceSfx;

    [Range(0f, 1f)]
    public float advanceSfxVolume = 1f;

    [Tooltip("If true, plays the SFX when StartDialogue shows the first line.")]
    public bool playSfxOnFirstLine = false;

    private readonly Dictionary<string, Sprite> portraitDictionary = new Dictionary<string, Sprite>(1024);
    private readonly Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private bool dialogueActive = false;

    private int _lastAdvanceFrame = -999;

    private bool _warnedMissingSfxOnce = false;

    private static readonly Regex _parenSuffix = new Regex(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);
    private static readonly Regex _cloneSuffix = new Regex(@"\s*\(clone\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _trailingDigits = new Regex(@"\d+$", RegexOptions.Compiled);

    private void Awake()
    {
        RefreshPortraitDictionary();
        EnsureSfxSourceReady();
    }

    private void OnEnable()
    {
        RefreshPortraitDictionary();
        EnsureSfxSourceReady();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        RefreshPortraitDictionary();
        EnsureSfxSourceReady();
    }
#endif

    private void Update()
    {
        if (!dialogueActive) return;

        if (useEToAdvance && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    public bool IsDialogueActive() => dialogueActive;

    public void StartDialogue(DialogueLine[] dialogueLines)
    {
        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        dialogueQueue.Clear();
        if (dialogueLines != null)
        {
            foreach (DialogueLine line in dialogueLines)
                dialogueQueue.Enqueue(line);
        }

        dialogueActive = true;

        if (dialogueButton != null)
        {
            dialogueButton.onClick.RemoveAllListeners();
            if (allowButtonClick)
                dialogueButton.onClick.AddListener(Advance);
        }

        ShowNextLine();

        if (playSfxOnFirstLine)
            PlayAdvanceSfx();
    }

    private void Advance()
    {
        if (_lastAdvanceFrame == Time.frameCount) return;
        _lastAdvanceFrame = Time.frameCount;

        PlayAdvanceSfx();
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = dialogueQueue.Dequeue();

        if (characterNameText != null) characterNameText.text = currentLine.speaker;
        if (dialogueText != null) dialogueText.text = currentLine.text;

        UpdatePortrait(currentLine.portraitName);
    }

    private void UpdatePortrait(string portraitName)
    {
        if (portraitImage == null) return;

        if (string.IsNullOrWhiteSpace(portraitName))
        {
            portraitImage.gameObject.SetActive(false);
            return;
        }

        if (TryGetPortrait(portraitName, out Sprite sprite))
        {
            portraitImage.sprite = sprite;
            portraitImage.gameObject.SetActive(true);

            if (verboseLogs)
                Debug.Log($"[DialogueManager] Portrait FOUND: request='{portraitName}' -> sprite='{sprite.name}'");
        }
        else
        {
            if (verboseLogs)
                Debug.LogWarning($"[DialogueManager] Portrait NOT found for request='{portraitName}'. Check typos or naming mismatch.");

            portraitImage.gameObject.SetActive(false);
        }
    }

    private void EnsureSfxSourceReady()
    {
        // Don’t create an AudioSource if they don't want SFX at all
        // BUT: if they assigned a clip, we ensure there is a source.
        if (sfxSource == null && advanceSfx != null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource != null)
        {
            sfxSource.enabled = true;
            sfxSource.playOnAwake = false;

            // Force 2D so it's always audible regardless of listener position
            sfxSource.spatialBlend = 0f;
        }
    }

    private void PlayAdvanceSfx()
    {
        if (advanceSfx == null)
        {
            if (verboseLogs && !_warnedMissingSfxOnce)
            {
                _warnedMissingSfxOnce = true;
                Debug.LogWarning("[DialogueManager] advanceSfx is NOT assigned, so no SFX will play.");
            }
            return;
        }

        EnsureSfxSourceReady();

        if (sfxSource == null)
        {
            if (verboseLogs)
                Debug.LogWarning("[DialogueManager] No AudioSource available for SFX. Assign sfxSource or keep advanceSfx assigned so it can auto-create one.");
            return;
        }

        // If AudioListener is missing, you won’t hear anything.
        // (We don't spam this log; only when SFX tries to play.)
        if (AudioListener.pause)
        {
            if (verboseLogs)
                Debug.LogWarning("[DialogueManager] AudioListener.pause is TRUE (audio paused). SFX won't be audible.");
        }

        sfxSource.PlayOneShot(advanceSfx, advanceSfxVolume);
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        dialogueActive = false;

        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);
    }

    // =========================
    // Portrait Dictionary
    // =========================

    private void RefreshPortraitDictionary()
    {
        portraitDictionary.Clear();
        if (allPortraitSprites == null) return;

        foreach (Sprite s in allPortraitSprites)
        {
            if (s == null) continue;

            foreach (string key in BuildKeysForSpriteName(s.name))
            {
                if (string.IsNullOrEmpty(key)) continue;

                if (!portraitDictionary.ContainsKey(key))
                    portraitDictionary.Add(key, s);
            }
        }
    }

    private bool TryGetPortrait(string requestName, out Sprite sprite)
    {
        sprite = null;

        string fixedRequest = autoFixCommonTypos ? ApplyCommonTypoFixes(requestName) : requestName;

        if (portraitDictionary.TryGetValue(fixedRequest, out sprite))
            return true;

        string norm = NormalizeKey(fixedRequest);
        if (!string.IsNullOrEmpty(norm) && portraitDictionary.TryGetValue(norm, out sprite))
            return true;

        if (supportFlippedUnderscoreFormat)
        {
            string flipped = FlipUnderscoreOrder(fixedRequest);
            if (!string.Equals(flipped, fixedRequest))
            {
                if (portraitDictionary.TryGetValue(flipped, out sprite))
                    return true;

                string normFlipped = NormalizeKey(flipped);
                if (!string.IsNullOrEmpty(normFlipped) && portraitDictionary.TryGetValue(normFlipped, out sprite))
                    return true;
            }
        }

        if (supportAllCapsCharacterFormat)
        {
            string capsRemap = RemapAllCapsCharacter(fixedRequest);
            if (!string.Equals(capsRemap, fixedRequest))
            {
                if (portraitDictionary.TryGetValue(capsRemap, out sprite))
                    return true;

                string normCaps = NormalizeKey(capsRemap);
                if (!string.IsNullOrEmpty(normCaps) && portraitDictionary.TryGetValue(normCaps, out sprite))
                    return true;
            }
        }

        string stripped = StripSuffixes(fixedRequest);
        string normStripped = NormalizeKey(stripped);
        if (!string.IsNullOrEmpty(normStripped) && portraitDictionary.TryGetValue(normStripped, out sprite))
            return true;

        if (allowContainsFallback && !string.IsNullOrEmpty(normStripped))
        {
            foreach (var kvp in portraitDictionary)
            {
                if (kvp.Key.Contains(normStripped))
                {
                    sprite = kvp.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerable<string> BuildKeysForSpriteName(string spriteName)
    {
        string fixedName = autoFixCommonTypos ? ApplyCommonTypoFixes(spriteName) : spriteName;

        yield return fixedName;

        string stripped = StripSuffixes(fixedName);
        yield return stripped;

        string norm = NormalizeKey(fixedName);
        if (!string.IsNullOrEmpty(norm)) yield return norm;

        string normStripped = NormalizeKey(stripped);
        if (!string.IsNullOrEmpty(normStripped)) yield return normStripped;

        string noDigits = _trailingDigits.Replace(stripped, "");
        string normNoDigits = NormalizeKey(noDigits);
        if (!string.IsNullOrEmpty(normNoDigits)) yield return normNoDigits;

        string aliasFlip = FlipUnderscoreOrder(fixedName);
        if (!string.Equals(aliasFlip, fixedName))
        {
            yield return aliasFlip;
            string normAliasFlip = NormalizeKey(aliasFlip);
            if (!string.IsNullOrEmpty(normAliasFlip)) yield return normAliasFlip;
        }

        string aliasCaps = RemapAllCapsCharacter(fixedName);
        if (!string.Equals(aliasCaps, fixedName))
        {
            yield return aliasCaps;
            string normAliasCaps = NormalizeKey(aliasCaps);
            if (!string.IsNullOrEmpty(normAliasCaps)) yield return normAliasCaps;
        }
    }

    // =========================
    // Typo Fixes
    // =========================

    private string ApplyCommonTypoFixes(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        // "neautral" -> "neutral"
        s = Regex.Replace(s, "neautral", "neutral", RegexOptions.IgnoreCase);

        return s;
    }

    // =========================
    // Alias Helpers
    // =========================

    private string FlipUnderscoreOrder(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        var parts = s.Split('_');
        if (parts.Length != 2) return s;

        string a = parts[0].Trim();
        string b = parts[1].Trim();

        if (IsKnownCharacterToken(b))
        {
            string charName = ToTitleCaseToken(b);
            return $"{charName}_{a}";
        }

        return s;
    }

    private string RemapAllCapsCharacter(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        var parts = s.Split('_');
        if (parts.Length != 2) return s;

        string a = parts[0].Trim();
        string b = parts[1].Trim();

        if (IsKnownCharacterToken(a))
        {
            string charName = ToTitleCaseToken(a);
            string mood = b.Length > 0 ? char.ToUpper(b[0]) + b.Substring(1).ToLowerInvariant() : b;
            return $"{charName}_{mood}";
        }

        return s;
    }

    private bool IsKnownCharacterToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        return token.Equals("mouse", System.StringComparison.OrdinalIgnoreCase) ||
               token.Equals("faith", System.StringComparison.OrdinalIgnoreCase);
    }

    private string ToTitleCaseToken(string token)
    {
        token = token.Trim();
        if (token.Length == 0) return token;
        return char.ToUpper(token[0]) + token.Substring(1).ToLowerInvariant();
    }

    // =========================
    // Normalization
    // =========================

    private string StripSuffixes(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        s = _cloneSuffix.Replace(s, "");
        s = _parenSuffix.Replace(s, "");
        return s.Trim();
    }

    private string NormalizeKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        if (autoFixCommonTypos)
            s = ApplyCommonTypoFixes(s);

        s = StripSuffixes(s);
        s = s.ToLowerInvariant();

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }
}