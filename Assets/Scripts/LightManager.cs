using UnityEngine;
using UnityEngine.Rendering.Universal;
using RamRoutes.Services;
using RamRoutes.Model;
using System.Collections;
using System.Collections.Generic;

public class LightManager : MonoBehaviour
{
    [Header("Stage Lights")]
    [Tooltip("Lights to activate when in Town Center (TC) stage")]
    [SerializeField] private Light2D[] tcStageLights;
    
    [Tooltip("Lights to activate when in Eastern Campus stage")]
    [SerializeField] private Light2D[] easternCampusLights;
    
    [Tooltip("Lights to activate when in First Street stage")]
    [SerializeField] private Light2D[] firstStreetLights;

    [Tooltip("Lights to activate when in Pedmall stage")]
    [SerializeField] private Light2D[] pedmallLights;

    [Tooltip("Lights to activate when in Terminal stage")]
    [SerializeField] private Light2D[] terminalLights;
    
    [Header("Settings")]
    [Tooltip("Duration for smooth light transitions between stages")]
    [SerializeField] private float transitionDuration = 1f;
    
    [Header("Terminal Stage Special Lighting")]
    [Tooltip("Special night light that turns on only during Terminal stage")]
    [SerializeField] private Light2D nightLight;
    
    [Header("Pedmall Stage Spooky Lighting")]
    [Tooltip("Controls how quickly the lights flicker (higher values = faster flickering)")]
    [Range(1f, 10f)]
    [SerializeField] private float flickerSpeed = 5f;
    
    [Tooltip("Controls how intense the flickering effect is (higher values = more dramatic changes)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float flickerIntensity = 0.3f;
    
    private Stage currentStage;
    private Coroutine transitionCoroutine;
    private Coroutine flickerCoroutine;
    
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
        
        // Stop any ongoing flicker effect
        StopFlickerEffect();
        
        // Adjust night light intensity based on stage
        if (nightLight != null)
        {
            float nightLightIntensity = stage switch
            {
                Stage.TC => 0f,
                Stage.EasternCampus => 0.05f,
                Stage.FirstStreet => 0.1f,
                Stage.Pedmall => 0f,
                Stage.Terminal => 1f,
                _ => 0f
            };
            
            nightLight.intensity = nightLightIntensity;
            nightLight.enabled = nightLightIntensity > 0;
        }
        
        if (stage == Stage.Terminal)
        {
            // For Terminal stage, activate night mode
            transitionCoroutine = StartCoroutine(ActivateTerminalNightMode());
        }
        else
        {
            // For other stages, turn on specific stage lights
            Light2D[] targetLights = GetLightsForStage(stage);
            if (targetLights != null && targetLights.Length > 0)
            {
                transitionCoroutine = StartCoroutine(TransitionToLights(targetLights));
                
                // Start spooky flickering effect specifically for Pedmall (stage 4)
                if (stage == Stage.Pedmall)
                {
                    // Wait for the transition to complete before starting the flicker effect
                    StartCoroutine(StartFlickerAfterTransition());
                }
            }
        }
        
        Debug.Log($"LightManager: Set lighting for stage {stage}");
    }
    
    /// <summary>
    /// Starts the flicker effect after the light transition is complete
    /// </summary>
    private IEnumerator StartFlickerAfterTransition()
    {
        // Wait for the transition to complete
        while (transitionCoroutine != null)
        {
            yield return null;
        }
        
        // Start the flickering effect
        flickerCoroutine = StartCoroutine(CreateSpookyFlickerEffect());
    }
    
    /// <summary>
    /// Gets the light components assigned to the specified stage
    /// </summary>
    /// <param name="stage">The game stage</param>
    /// <returns>The Light2D array for the stage, or null if not assigned</returns>
    private Light2D[] GetLightsForStage(Stage stage)
    {
        return stage switch
        {
            Stage.TC => tcStageLights,
            Stage.EasternCampus => easternCampusLights,
            Stage.FirstStreet => firstStreetLights,
            Stage.Pedmall => pedmallLights,
            Stage.Terminal => null, // Terminal stage uses all lights, not a specific set
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
    /// Smoothly transitions multiple lights to full intensity
    /// </summary>
    /// <param name="targetLights">The lights to turn on</param>
    private IEnumerator TransitionToLights(Light2D[] targetLights)
    {
        if (targetLights == null || targetLights.Length == 0) yield break;
        
        // Store starting intensities for all lights
        float[] startIntensities = new float[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                startIntensities[i] = targetLights[i].intensity;
                targetLights[i].enabled = true;
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
            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] != null)
                {
                    targetLights[i].intensity = Mathf.Lerp(startIntensities[i], targetIntensity, progress);
                }
            }
            
            yield return null;
        }
        
        // Ensure final state for all lights
        foreach (Light2D light in targetLights)
        {
            if (light != null)
            {
                light.intensity = targetIntensity;
                light.enabled = true;
            }
        }
        
        transitionCoroutine = null;
        
        Debug.Log($"LightManager: Lights transition complete for {targetLights.Length} lights in {currentStage} stage");
    }
    
    /// <summary>
    /// Smoothly transitions all lights to full intensity (used for Terminal stage)
    /// </summary>
    private IEnumerator TransitionAllLights()
    {
        // Collect all lights from all stages
        List<Light2D> allLightsList = new List<Light2D>();
        
        // Add lights from each stage array
        if (tcStageLights != null) allLightsList.AddRange(tcStageLights);
        if (easternCampusLights != null) allLightsList.AddRange(easternCampusLights);
        if (firstStreetLights != null) allLightsList.AddRange(firstStreetLights);
        if (pedmallLights != null) allLightsList.AddRange(pedmallLights);
        if (terminalLights != null) allLightsList.AddRange(terminalLights);
        
        // Remove null references
        allLightsList.RemoveAll(light => light == null);
        
        if (allLightsList.Count == 0)
        {
            Debug.LogWarning("LightManager: No lights found for Terminal stage transition");
            yield break;
        }
        
        Light2D[] allLights = allLightsList.ToArray();
        
        // Store starting intensities for all lights
        float[] startIntensities = new float[allLights.Length];
        for (int i = 0; i < allLights.Length; i++)
        {
            startIntensities[i] = allLights[i].intensity;
            allLights[i].enabled = true;
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
                allLights[i].intensity = Mathf.Lerp(startIntensities[i], targetIntensity, progress);
            }
            
            yield return null;
        }
        
        // Ensure final state for all lights
        foreach (Light2D light in allLights)
        {
            light.intensity = targetIntensity;
            light.enabled = true;
        }
        
        transitionCoroutine = null;
        
        Debug.Log($"LightManager: All {allLights.Length} lights transition complete for Terminal stage");
    }
    
    /// <summary>
    /// Activates terminal night mode - turns on night light, disables all other lights, and hides poles
    /// </summary>
    private IEnumerator ActivateTerminalNightMode()
    {
        // Hide all objects tagged with "Pole"
        GameObject[] poles = GameObject.FindGameObjectsWithTag("Pole");
                GameObject[] gates = GameObject.FindGameObjectsWithTag("Gate");

        foreach (GameObject pole in poles)
        {
            pole.SetActive(false);
        }
        foreach (GameObject gate in gates)
        {
            gate.SetActive(false);
        }
        Debug.Log($"LightManager: Hidden {poles.Length} poles and {gates.Length} gates for Terminal stage");

        // Turn on the night light if assigned
        if (nightLight != null)
        {
            nightLight.enabled = true;
            // Smoothly transition night light to full intensity
            float elapsed = 0f;
            float startIntensity = nightLight.intensity;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / transitionDuration;
                nightLight.intensity = Mathf.Lerp(startIntensity, 1f, progress);
                yield return null;
            }
            
            nightLight.intensity = 1f;
            Debug.Log("LightManager: Night light activated for Terminal stage");
        }
        
        // Find all Light2D components in the scene and disable them (except the night light)
        Light2D[] allSceneLights = FindObjectsOfType<Light2D>();
        int disabledCount = 0;
        
        foreach (Light2D light in allSceneLights)
        {
            // Skip the night light - we want it to stay on
            if (light == nightLight) continue;
            
            // Disable all other lights by setting intensity to 0
            if (light.intensity > 0)
            {
                light.intensity = 0f;
                disabledCount++;
            }
        }
        
        transitionCoroutine = null;
        Debug.Log($"LightManager: Terminal night mode activated - disabled {disabledCount} scene lights, night light on");
    }
    
    /// <summary>
    /// Immediately sets a specific stage lights to on
    /// </summary>
    /// <param name="stage">The stage whose lights should be turned on</param>
    public void SetLightingImmediately(Stage stage)
    {
        // Stop any ongoing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        // Stop any ongoing flicker effect
        StopFlickerEffect();
        
        currentStage = stage;
        
        // Adjust night light intensity based on stage
        if (nightLight != null)
        {
            float nightLightIntensity = stage switch
            {
                Stage.TC => 0f,
                Stage.EasternCampus => 0.05f,
                Stage.FirstStreet => 0.1f,
                Stage.Pedmall => 0f,
                Stage.Terminal => 1f,
                _ => 0f
            };
            
            nightLight.intensity = nightLightIntensity;
            nightLight.enabled = nightLightIntensity > 0;
        }
        
        if (stage == Stage.Terminal)
        {
            // Hide all objects tagged with "Pole"
            GameObject[] poles = GameObject.FindGameObjectsWithTag("Pole");
            GameObject[] gates = GameObject.FindGameObjectsWithTag("Gate");

            foreach (GameObject pole in poles)
            {
                pole.SetActive(false);
            }
            foreach (GameObject gate in gates)
            {
                gate.SetActive(false);
            }
            Debug.Log($"LightManager: Hidden {poles.Length} poles and {gates.Length} gates for Terminal stage");

            // Find all Light2D components in the scene and disable them (except the night light)
            Light2D[] allSceneLights = FindObjectsOfType<Light2D>();
            int disabledCount = 0;
            
            foreach (Light2D light in allSceneLights)
            {
                // Skip the night light - we want it to stay on
                if (light == nightLight) continue;
                
                // Disable all other lights by setting intensity to 0
                if (light.intensity > 0)
                {
                    light.intensity = 0f;
                    disabledCount++;
                }
            }
            
            Debug.Log($"LightManager: Immediately activated Terminal night mode - disabled {disabledCount} scene lights, night light on");
        }
        else
        {
            // For other stages, turn on specific stage lights
            Light2D[] targetLights = GetLightsForStage(stage);
            if (targetLights != null)
            {
                foreach (Light2D light in targetLights)
                {
                    if (light != null)
                    {
                        light.enabled = true;
                        light.intensity = 1f; // Set to full intensity
                    }
                }
                Debug.Log($"LightManager: Immediately set {targetLights.Length} lights on for stage {stage}");
                
                // Start spooky flickering effect specifically for Pedmall (stage 4)
                if (stage == Stage.Pedmall)
                {
                    flickerCoroutine = StartCoroutine(CreateSpookyFlickerEffect());
                }
            }
        }
    }
    
    /// <summary>
    /// Turns off all stage lights
    /// </summary>
    private void TurnOffAllLights()
    {
        // Show all poles that might have been hidden in Terminal stage
        GameObject[] poles = GameObject.FindGameObjectsWithTag("Pole");
        foreach (GameObject pole in poles)
        {
            pole.SetActive(true);
        }
        
        // Collect all lights from all stage arrays
        List<Light2D> allLightsList = new List<Light2D>();
        
        if (tcStageLights != null) allLightsList.AddRange(tcStageLights);
        if (easternCampusLights != null) allLightsList.AddRange(easternCampusLights);
        if (firstStreetLights != null) allLightsList.AddRange(firstStreetLights);
        if (pedmallLights != null) allLightsList.AddRange(pedmallLights);
        if (terminalLights != null) allLightsList.AddRange(terminalLights);
        
        foreach (Light2D light in allLightsList)
        {
            if (light != null)
            {
                light.enabled = false;
                light.intensity = 0f;
            }
        }
        
        Debug.Log($"LightManager: Turned off {allLightsList.Count} lights");
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
    /// Creates a spooky flickering effect for Pedmall (stage 4) lights
    /// </summary>
    private IEnumerator CreateSpookyFlickerEffect()
    {
        if (pedmallLights == null || pedmallLights.Length == 0)
        {
            Debug.LogWarning("LightManager: No Pedmall lights assigned for flickering effect");
            yield break;
        }
        
        Debug.Log("LightManager: Starting spooky flickering effect for Pedmall lights");
        
        // Store original intensity values to use as baseline
        float[] baseIntensities = new float[pedmallLights.Length];
        for (int i = 0; i < pedmallLights.Length; i++)
        {
            if (pedmallLights[i] != null)
            {
                baseIntensities[i] = pedmallLights[i].intensity;
            }
        }
        
        // Keep flickering until told to stop
        while (true)
        {
            // Generate random values for each light
            for (int i = 0; i < pedmallLights.Length; i++)
            {
                if (pedmallLights[i] != null)
                {
                    // Create a perlin noise value that changes over time for a more natural flicker
                    float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + i * 0.3f, i * 0.3f);
                    
                    // Map the noise to a useful range and apply intensity control
                    float intensityVariation = (noise * 2 - 1) * flickerIntensity;
                    
                    // Apply the variation to the base intensity, keeping it in a reasonable range
                    float newIntensity = Mathf.Clamp(baseIntensities[i] + intensityVariation, 
                                                    baseIntensities[i] * (1 - flickerIntensity), 
                                                    baseIntensities[i] * (1 + flickerIntensity * 0.5f));
                    
                    pedmallLights[i].intensity = newIntensity;
                }
            }
            
            // Wait a frame before updating again
            yield return null;
        }
    }
    
    /// <summary>
    /// Stops the flickering effect if it's running
    /// </summary>
    private void StopFlickerEffect()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
            Debug.Log("LightManager: Stopped spooky flickering effect");
            
            // Reset lights to their original intensity
            if (pedmallLights != null)
            {
                foreach (Light2D light in pedmallLights)
                {
                    if (light != null)
                    {
                        light.intensity = 1f;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if lights are assigned for the specified stage
    /// </summary>
    /// <param name="stage">The stage to check</param>
    /// <returns>True if lights are assigned, false otherwise</returns>
    public bool HasLightForStage(Stage stage)
    {
        if (stage == Stage.Terminal)
        {
            // Terminal stage uses all lights, so check if any are assigned
            return (tcStageLights != null && tcStageLights.Length > 0) ||
                   (easternCampusLights != null && easternCampusLights.Length > 0) ||
                   (firstStreetLights != null && firstStreetLights.Length > 0) ||
                   (pedmallLights != null && pedmallLights.Length > 0) ||
                   (terminalLights != null && terminalLights.Length > 0);
        }
        
        Light2D[] stageLights = GetLightsForStage(stage);
        return stageLights != null && stageLights.Length > 0;
    }
}
