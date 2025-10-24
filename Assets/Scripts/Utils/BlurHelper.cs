using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Simple helper to animate a Volume's BlurSettings.strength parameter.
// Usage (baby steps):
// 1. Put a Global Volume in the scene with a profile that includes BlurSettings (strength default 0).
// 2. Put this BlurHelper on a GameObject in the scene (or let it auto-find the Volume).
// 3. Call BlurHelper.Instance.PlayBlur(0.7f, 0.35f) to blur in, and PlayBlur(0f,0.25f) to blur out.

public class BlurHelper : MonoBehaviour
{
    public static BlurHelper Instance { get; private set; }

    [Tooltip("Optional: assign the Global Volume that contains the BlurSettings. If empty the helper will try to find any Volume in the scene.")]
    public Volume targetVolume;

    // The BlurSettings type exists in the project (added previously as part of the URP Renderer Feature).
    // We keep a reference so we can change its strength.value.
    private object blurSettingsObj = null; // kept as object in case BlurSettings is declared without a namespace

    // Cached reflection info to avoid repeated lookups.
    private System.Reflection.PropertyInfo strengthProperty = null;
    private System.Reflection.FieldInfo strengthField = null;

    private Coroutine runningCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (targetVolume == null)
        {
            // try find a global volume
            var vol = FindObjectOfType<Volume>();
            if (vol != null) targetVolume = vol;
        }

        if (targetVolume == null)
        {
            Debug.LogWarning("BlurHelper: No Volume found in scene. Please add a Global Volume with BlurSettings.");
            return;
        }

        // Try to find the BlurSettings component in the profile.
        if (targetVolume.profile == null)
        {
            Debug.LogWarning("BlurHelper: Volume has no profile assigned.");
            return;
        }

        // Use the non-generic TryGet to avoid compile-time dependency on a namespace if it differs.
        // We'll iterate components and find one named 'BlurSettings'.
        foreach (var comp in targetVolume.profile.components)
        {
            if (comp == null) continue;
            var compType = comp.GetType();
            if (compType.Name == "BlurSettings")
            {
                blurSettingsObj = comp;
                // find the 'strength' field or property (ClampedFloatParameter strength)
                strengthProperty = compType.GetProperty("strength");
                if (strengthProperty == null)
                {
                    strengthField = compType.GetField("strength");
                    if (strengthField == null)
                        Debug.LogWarning("BlurHelper: Found BlurSettings but it has no 'strength' field or property.");
                }
                break;
            }
        }

        if (blurSettingsObj == null)
        {
            Debug.LogWarning("BlurHelper: BlurSettings not found in Volume profile. Make sure the URP Blur Renderer Feature's Volume component class is present in the Volume profile.");
        }
    }

    // Public API: animate the blur strength to targetStrength over duration seconds.
    public void PlayBlur(float targetStrength, float duration)
    {
        Debug.Log($"BlurHelper: PlayBlur called with target={targetStrength}, duration={duration}");
        if (blurSettingsObj == null || strengthProperty == null)
        {
            Debug.LogWarning("BlurHelper: blurSettingsObj or strengthProperty is null. Setup failed?");
            return;
        }

        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(AnimateStrength(targetStrength, duration));
    }

    public void ResetBlur()
    {
        if (blurSettingsObj == null || (strengthProperty == null && strengthField == null)) return;
        object strengthParam = null;
        if (strengthProperty != null) strengthParam = strengthProperty.GetValue(blurSettingsObj, null);
        else if (strengthField != null) strengthParam = strengthField.GetValue(blurSettingsObj);
        if (strengthParam == null) return;
        // ClampedFloatParameter has a 'value' field/property — try property first, fallback to field
        var valProp = strengthParam.GetType().GetProperty("value");
        if (valProp != null) valProp.SetValue(strengthParam, 0f, null);
        else
        {
            var valField = strengthParam.GetType().GetField("value");
            if (valField != null) valField.SetValue(strengthParam, 0f);
        }
    }

    private IEnumerator AnimateStrength(float target, float duration)
    {
        // read current value
        object strengthParam = null;
        if (strengthProperty != null)
            strengthParam = strengthProperty.GetValue(blurSettingsObj, null);
        else if (strengthField != null)
            strengthParam = strengthField.GetValue(blurSettingsObj);
        if (strengthParam == null) yield break;

    var valProp = strengthParam.GetType().GetProperty("value");
    var valField = strengthParam.GetType().GetField("value");
        if (valProp == null && valField == null)
        {
            Debug.LogWarning("BlurHelper: Can't find .value on BlurSettings.strength parameter.");
            yield break;
        }

        float start = (valProp != null) ? (float)valProp.GetValue(strengthParam, null) : (float)valField.GetValue(strengthParam);
        float elapsed = 0f;

        // avoid division by zero
        if (duration <= 0f)
        {
            // snap
            if (valProp != null) valProp.SetValue(strengthParam, target, null);
            else valField.SetValue(strengthParam, target);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // use unscaled so UI animations are consistent when timeScale changes
            float t = Mathf.Clamp01(elapsed / duration);
            float v = Mathf.Lerp(start, target, t);
            if (valProp != null) valProp.SetValue(strengthParam, v, null);
            else valField.SetValue(strengthParam, v);
            yield return null;
        }

        // ensure final value
        if (valProp != null) valProp.SetValue(strengthParam, target, null);
        else valField.SetValue(strengthParam, target);
        runningCoroutine = null;
    }
}
