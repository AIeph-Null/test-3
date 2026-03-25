using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterController : MonoBehaviour
{
    [SerializeField] private Transform xrCamera;
    [SerializeField] private float waterLevelY = 0f;
    [SerializeField] private Volume underwaterVolume;
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Audio")]
    [SerializeField] private AudioLowPassFilter lowPass;
    [SerializeField] private float underwaterCutoff = 900f;
    [SerializeField] private float aboveWaterCutoff = 22000f;

    [Header("Stability")]
    [SerializeField] private float hysteresis = 0.05f;

    private bool isUnderwater;

    private void Reset()
    {
        if (Camera.main != null) xrCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (xrCamera == null || underwaterVolume == null) return;

        float y = xrCamera.position.y;

        bool targetState = isUnderwater
            ? y < waterLevelY + hysteresis
            : y < waterLevelY - hysteresis;

        isUnderwater = targetState;

        float targetWeight = isUnderwater ? 1f : 0f;
        underwaterVolume.weight = Mathf.MoveTowards(
            underwaterVolume.weight,
            targetWeight,
            fadeSpeed * Time.deltaTime
        );

        if (lowPass != null)
        {
            float targetCutoff = isUnderwater ? underwaterCutoff : aboveWaterCutoff;
            lowPass.cutoffFrequency = Mathf.Lerp(
                lowPass.cutoffFrequency,
                targetCutoff,
                fadeSpeed * Time.deltaTime
            );
        }

        // Optional: drive your own shader effects
        Shader.SetGlobalFloat("_UnderwaterAmount", underwaterVolume.weight);
    }
}