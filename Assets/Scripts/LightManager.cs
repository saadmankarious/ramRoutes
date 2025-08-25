using UnityEngine;
using UnityEngine.Rendering.Universal;
using RamRoutes.Services;
using RamRoutes.Model;
using System.Collections;

public class LightManager : MonoBehaviour
{
    [Header("Stage Lights")]
    [Tooltip("Light to activate when in Town Center (TC) stage")]
    [SerializeField] private Light2D tcStageLight;
    
    [Tooltip("Light to activate when in Eastern Campus stage")]
    [SerializeField] private Light2D easternCampusLight;
    
    [Tooltip("Light to activate when in First Street stage")]
    [SerializeField] private Light2D firstStreetLight;

    [Tooltip("Light to activate when in Pedmall stage")]
    [SerializeField] private Light2D pedmallLight;

    [Tooltip("Light to activate when in Terminal stage")]
    [SerializeField] private Light2D terminalLight;
    
    [Header("Settings")]
    [Tooltip("Duration for smooth light transitions between stages")]
    [SerializeField] private float transitionDuration = 1f;
    
    private Stage currentStage;
    private Coroutine transitionCoroutine;
    
    private void Start()
    {
        // Initialize lighting based on current game stage
        InitializeLighting();
    }
    
    private void InitializeLighting()
    {
        // First, ensure all lights are turned off
        TurnOffAllLights();
        
        // Get current stage from GameStageService
        var gameStage = GameStageService.LoadStageFromPrefs();
        if (gameStage != null)
        {
            SetLightingForStage(gameStage.area);
        }
        else
        {
            // Default to TC stage if no stage is set
            SetLightingForStage(Stage.TC);
        }
    }
    
    /// <summary>
    /// Sets the lighting for the specified game stage
    /// </summary>
    /// <param name="stage">The game stage to set lighting for</param>
    public void SetLightingForStage(Stage stage)
    {
        currentStage = stage;
        
        // Stop any ongoing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        if (stage == Stage.Terminal)
        {
            // For Terminal stage, turn on all lights
            transitionCoroutine = StartCoroutine(TransitionAllLights());
        }
        else
        {
            // For other stages, turn on specific light
            Light2D targetLight = GetLightForStage(stage);
            if (targetLight != null)
            {
                transitionCoroutine = StartCoroutine(TransitionToLight(targetLight));
            }
        }
        
        Debug.Log($"LightManager: Set lighting for stage {stage}");
    }
    
    /// <summary>
    /// Gets the light component assigned to the specified stage
    /// </summary>
    /// <param name="stage">The game stage</param>
    /// <returns>The Light2D component for the stage, or null if not assigned</returns>
    private Light2D GetLightForStage(Stage stage)
    {
        return stage switch
        {
            Stage.TC => tcStageLight,
            Stage.EasternCampus => easternCampusLight,
            Stage.FirstStreet => firstStreetLight,
            Stage.Pedmall => pedmallLight,
            Stage.Terminal => null, // Terminal stage uses all lights, not a specific one
            _ => null
        };
    }
    
    /// <summary>
    /// Smoothly transitions a specific light to full intensity
    /// </summary>
    /// <param name="targetLight">The light to turn on</param>
    private IEnumerator TransitionToLight(Light2D targetLight)
    {
        if (targetLight == null) yield break;
        
        float elapsed = 0f;
        float startIntensity = targetLight.intensity;
        float targetIntensity = 1f; // Always transition to full intensity
        
        // Make sure the light is enabled
        targetLight.enabled = true;
        
        // Transition phase: fade in the target light
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / transitionDuration;
            
            // Fade in target light
            targetLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
            
            yield return null;
        }
        
        // Ensure final state
        targetLight.intensity = targetIntensity;
        targetLight.enabled = true;
        
        transitionCoroutine = null;
        
        Debug.Log($"LightManager: Light transition complete for {targetLight.name}");
    }
    
    /// <summary>
    /// Smoothly transitions all lights to full intensity (used for Terminal stage)
    /// </summary>
    private IEnumerator TransitionAllLights()
    {
        Light2D[] allLights = { tcStageLight, easternCampusLight, firstStreetLight, pedmallLight, terminalLight };
        
        // Store starting intensities for all lights
        float[] startIntensities = new float[allLights.Length];
        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] != null)
            {
                startIntensities[i] = allLights[i].intensity;
                allLights[i].enabled = true;
            }
        }
        
        float elapsed = 0f;
        float targetIntensity = 1f;
        
        // Transition phase: fade in all lights
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / transitionDuration;
            
            // Fade in all lights
            for (int i = 0; i < allLights.Length; i++)
            {
                if (allLights[i] != null)
                {
                    allLights[i].intensity = Mathf.Lerp(startIntensities[i], targetIntensity, progress);
                }
            }
            
            yield return null;
        }
        
        // Ensure final state for all lights
        foreach (Light2D light in allLights)
        {
            if (light != null)
            {
                light.intensity = targetIntensity;
                light.enabled = true;
            }
        }
        
        transitionCoroutine = null;
        
        Debug.Log("LightManager: All lights transition complete for Terminal stage");
    }
    
    /// <summary>
    /// Immediately sets a specific stage light to on
    /// </summary>
    /// <param name="stage">The stage whose light should be turned on</param>
    public void SetLightingImmediately(Stage stage)
    {
        // Stop any ongoing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        currentStage = stage;
        
        if (stage == Stage.Terminal)
        {
            // For Terminal stage, turn on all lights immediately
            Light2D[] allLights = { tcStageLight, easternCampusLight, firstStreetLight, pedmallLight, terminalLight };
            foreach (Light2D light in allLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                    light.intensity = 1f;
                }
            }
            Debug.Log("LightManager: Immediately turned on all lights for Terminal stage");
        }
        else
        {
            // For other stages, turn on specific light
            Light2D targetLight = GetLightForStage(stage);
            if (targetLight != null)
            {
                targetLight.enabled = true;
                targetLight.intensity = 1f; // Set to full intensity
            }
            Debug.Log($"LightManager: Immediately set light on for stage {stage}");
        }
    }
    
    /// <summary>
    /// Turns off all stage lights
    /// </summary>
    private void TurnOffAllLights()
    {
        Light2D[] allLights = { tcStageLight, easternCampusLight, firstStreetLight, pedmallLight, terminalLight };
        
        foreach (Light2D light in allLights)
        {
            if (light != null)
            {
                light.enabled = false;
                light.intensity = 0f;
            }
        }
    }
    
    /// <summary>
    /// Gets the current stage that lighting is set for
    /// </summary>
    /// <returns>The current Stage</returns>
    public Stage GetCurrentStage()
    {
        return currentStage;
    }
    
    /// <summary>
    /// Checks if a light is assigned for the specified stage
    /// </summary>
    /// <param name="stage">The stage to check</param>
    /// <returns>True if a light is assigned, false otherwise</returns>
    public bool HasLightForStage(Stage stage)
    {
        if (stage == Stage.Terminal)
        {
            // Terminal stage uses all lights, so check if any are assigned
            return tcStageLight != null || easternCampusLight != null || firstStreetLight != null || 
                   pedmallLight != null || terminalLight != null;
        }
        
        return GetLightForStage(stage) != null;
    }
}
