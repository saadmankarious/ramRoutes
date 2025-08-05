extern "C" void RegisterStaticallyLinkedModulesGranular()
{
	void RegisterModule_SharedInternals();
	RegisterModule_SharedInternals();

	void RegisterModule_Core();
	RegisterModule_Core();

	void RegisterModule_AndroidJNI();
	RegisterModule_AndroidJNI();

	void RegisterModule_Animation();
	RegisterModule_Animation();

	void RegisterModule_Audio();
	RegisterModule_Audio();

	void RegisterModule_Director();
	RegisterModule_Director();

	void RegisterModule_GameCenter();
	RegisterModule_GameCenter();

	void RegisterModule_GraphicsStateCollectionSerializer();
	RegisterModule_GraphicsStateCollectionSerializer();

	void RegisterModule_Grid();
	RegisterModule_Grid();

	void RegisterModule_HierarchyCore();
	RegisterModule_HierarchyCore();

	void RegisterModule_IMGUI();
	RegisterModule_IMGUI();

	void RegisterModule_ImageConversion();
	RegisterModule_ImageConversion();

	void RegisterModule_Input();
	RegisterModule_Input();

	void RegisterModule_InputLegacy();
	RegisterModule_InputLegacy();

	void RegisterModule_InputForUI();
	RegisterModule_InputForUI();

	void RegisterModule_JSONSerialize();
	RegisterModule_JSONSerialize();

	void RegisterModule_ParticleSystem();
	RegisterModule_ParticleSystem();

	void RegisterModule_Physics();
	RegisterModule_Physics();

	void RegisterModule_Physics2D();
	RegisterModule_Physics2D();

	void RegisterModule_Properties();
	RegisterModule_Properties();

	void RegisterModule_RuntimeInitializeOnLoadManagerInitializer();
	RegisterModule_RuntimeInitializeOnLoadManagerInitializer();

	void RegisterModule_Subsystems();
	RegisterModule_Subsystems();

	void RegisterModule_Terrain();
	RegisterModule_Terrain();

	void RegisterModule_TextRendering();
	RegisterModule_TextRendering();

	void RegisterModule_TextCoreFontEngine();
	RegisterModule_TextCoreFontEngine();

	void RegisterModule_TextCoreTextEngine();
	RegisterModule_TextCoreTextEngine();

	void RegisterModule_Tilemap();
	RegisterModule_Tilemap();

	void RegisterModule_UI();
	RegisterModule_UI();

	void RegisterModule_UIElements();
	RegisterModule_UIElements();

	void RegisterModule_UnityAnalyticsCommon();
	RegisterModule_UnityAnalyticsCommon();

	void RegisterModule_VFX();
	RegisterModule_VFX();

	void RegisterModule_XR();
	RegisterModule_XR();

	void RegisterModule_VR();
	RegisterModule_VR();

}

template <typename T> void RegisterUnityClass(const char*);
template <typename T> void RegisterStrippedType(int, const char*, const char*);

void InvokeRegisterStaticallyLinkedModuleClasses()
{
	// Do nothing (we're in stripping mode)
}

class Animation; template <> void RegisterUnityClass<Animation>(const char*);
class AnimationClip; template <> void RegisterUnityClass<AnimationClip>(const char*);
class Animator; template <> void RegisterUnityClass<Animator>(const char*);
class AnimatorController; template <> void RegisterUnityClass<AnimatorController>(const char*);
class AnimatorOverrideController; template <> void RegisterUnityClass<AnimatorOverrideController>(const char*);
class Avatar; template <> void RegisterUnityClass<Avatar>(const char*);
class AvatarMask; template <> void RegisterUnityClass<AvatarMask>(const char*);
class Motion; template <> void RegisterUnityClass<Motion>(const char*);
class RuntimeAnimatorController; template <> void RegisterUnityClass<RuntimeAnimatorController>(const char*);
class AudioBehaviour; template <> void RegisterUnityClass<AudioBehaviour>(const char*);
class AudioClip; template <> void RegisterUnityClass<AudioClip>(const char*);
class AudioListener; template <> void RegisterUnityClass<AudioListener>(const char*);
class AudioManager; template <> void RegisterUnityClass<AudioManager>(const char*);
class AudioResource; template <> void RegisterUnityClass<AudioResource>(const char*);
class AudioSource; template <> void RegisterUnityClass<AudioSource>(const char*);
class SampleClip; template <> void RegisterUnityClass<SampleClip>(const char*);
class Behaviour; template <> void RegisterUnityClass<Behaviour>(const char*);
class BuildSettings; template <> void RegisterUnityClass<BuildSettings>(const char*);
class Camera; template <> void RegisterUnityClass<Camera>(const char*);
namespace Unity { class Component; } template <> void RegisterUnityClass<Unity::Component>(const char*);
class ComputeShader; template <> void RegisterUnityClass<ComputeShader>(const char*);
class Cubemap; template <> void RegisterUnityClass<Cubemap>(const char*);
class CubemapArray; template <> void RegisterUnityClass<CubemapArray>(const char*);
class DelayedCallManager; template <> void RegisterUnityClass<DelayedCallManager>(const char*);
class EditorExtension; template <> void RegisterUnityClass<EditorExtension>(const char*);
class GameManager; template <> void RegisterUnityClass<GameManager>(const char*);
class GameObject; template <> void RegisterUnityClass<GameObject>(const char*);
class GlobalGameManager; template <> void RegisterUnityClass<GlobalGameManager>(const char*);
class GraphicsSettings; template <> void RegisterUnityClass<GraphicsSettings>(const char*);
class InputManager; template <> void RegisterUnityClass<InputManager>(const char*);
class LODGroup; template <> void RegisterUnityClass<LODGroup>(const char*);
class LevelGameManager; template <> void RegisterUnityClass<LevelGameManager>(const char*);
class Light; template <> void RegisterUnityClass<Light>(const char*);
class LightProbeProxyVolume; template <> void RegisterUnityClass<LightProbeProxyVolume>(const char*);
class LightProbes; template <> void RegisterUnityClass<LightProbes>(const char*);
class LightingSettings; template <> void RegisterUnityClass<LightingSettings>(const char*);
class LightmapSettings; template <> void RegisterUnityClass<LightmapSettings>(const char*);
class LowerResBlitTexture; template <> void RegisterUnityClass<LowerResBlitTexture>(const char*);
class Material; template <> void RegisterUnityClass<Material>(const char*);
class Mesh; template <> void RegisterUnityClass<Mesh>(const char*);
class MeshFilter; template <> void RegisterUnityClass<MeshFilter>(const char*);
class MeshRenderer; template <> void RegisterUnityClass<MeshRenderer>(const char*);
class MonoBehaviour; template <> void RegisterUnityClass<MonoBehaviour>(const char*);
class MonoManager; template <> void RegisterUnityClass<MonoManager>(const char*);
class MonoScript; template <> void RegisterUnityClass<MonoScript>(const char*);
class NamedObject; template <> void RegisterUnityClass<NamedObject>(const char*);
class Object; template <> void RegisterUnityClass<Object>(const char*);
class PlayerSettings; template <> void RegisterUnityClass<PlayerSettings>(const char*);
class PreloadData; template <> void RegisterUnityClass<PreloadData>(const char*);
class QualitySettings; template <> void RegisterUnityClass<QualitySettings>(const char*);
class RayTracingShader; template <> void RegisterUnityClass<RayTracingShader>(const char*);
namespace UI { class RectTransform; } template <> void RegisterUnityClass<UI::RectTransform>(const char*);
class ReflectionProbe; template <> void RegisterUnityClass<ReflectionProbe>(const char*);
class RenderSettings; template <> void RegisterUnityClass<RenderSettings>(const char*);
class RenderTexture; template <> void RegisterUnityClass<RenderTexture>(const char*);
class Renderer; template <> void RegisterUnityClass<Renderer>(const char*);
class ResourceManager; template <> void RegisterUnityClass<ResourceManager>(const char*);
class RuntimeInitializeOnLoadManager; template <> void RegisterUnityClass<RuntimeInitializeOnLoadManager>(const char*);
class Shader; template <> void RegisterUnityClass<Shader>(const char*);
class ShaderNameRegistry; template <> void RegisterUnityClass<ShaderNameRegistry>(const char*);
class Skybox; template <> void RegisterUnityClass<Skybox>(const char*);
class SortingGroup; template <> void RegisterUnityClass<SortingGroup>(const char*);
class Sprite; template <> void RegisterUnityClass<Sprite>(const char*);
class SpriteAtlas; template <> void RegisterUnityClass<SpriteAtlas>(const char*);
class SpriteRenderer; template <> void RegisterUnityClass<SpriteRenderer>(const char*);
class TagManager; template <> void RegisterUnityClass<TagManager>(const char*);
class TextAsset; template <> void RegisterUnityClass<TextAsset>(const char*);
class Texture; template <> void RegisterUnityClass<Texture>(const char*);
class Texture2D; template <> void RegisterUnityClass<Texture2D>(const char*);
class Texture2DArray; template <> void RegisterUnityClass<Texture2DArray>(const char*);
class Texture3D; template <> void RegisterUnityClass<Texture3D>(const char*);
class TimeManager; template <> void RegisterUnityClass<TimeManager>(const char*);
class Transform; template <> void RegisterUnityClass<Transform>(const char*);
class PlayableDirector; template <> void RegisterUnityClass<PlayableDirector>(const char*);
class Grid; template <> void RegisterUnityClass<Grid>(const char*);
class GridLayout; template <> void RegisterUnityClass<GridLayout>(const char*);
class ParticleSystem; template <> void RegisterUnityClass<ParticleSystem>(const char*);
class ParticleSystemRenderer; template <> void RegisterUnityClass<ParticleSystemRenderer>(const char*);
class BoxCollider; template <> void RegisterUnityClass<BoxCollider>(const char*);
class CapsuleCollider; template <> void RegisterUnityClass<CapsuleCollider>(const char*);
class Collider; template <> void RegisterUnityClass<Collider>(const char*);
class MeshCollider; template <> void RegisterUnityClass<MeshCollider>(const char*);
class PhysicsManager; template <> void RegisterUnityClass<PhysicsManager>(const char*);
class Rigidbody; template <> void RegisterUnityClass<Rigidbody>(const char*);
class SphereCollider; template <> void RegisterUnityClass<SphereCollider>(const char*);
class BoxCollider2D; template <> void RegisterUnityClass<BoxCollider2D>(const char*);
class Collider2D; template <> void RegisterUnityClass<Collider2D>(const char*);
class CompositeCollider2D; template <> void RegisterUnityClass<CompositeCollider2D>(const char*);
class Physics2DSettings; template <> void RegisterUnityClass<Physics2DSettings>(const char*);
class PolygonCollider2D; template <> void RegisterUnityClass<PolygonCollider2D>(const char*);
class Rigidbody2D; template <> void RegisterUnityClass<Rigidbody2D>(const char*);
class Terrain; template <> void RegisterUnityClass<Terrain>(const char*);
class TerrainData; template <> void RegisterUnityClass<TerrainData>(const char*);
namespace TextRendering { class Font; } template <> void RegisterUnityClass<TextRendering::Font>(const char*);
class Tilemap; template <> void RegisterUnityClass<Tilemap>(const char*);
class TilemapRenderer; template <> void RegisterUnityClass<TilemapRenderer>(const char*);
namespace UI { class Canvas; } template <> void RegisterUnityClass<UI::Canvas>(const char*);
namespace UI { class CanvasGroup; } template <> void RegisterUnityClass<UI::CanvasGroup>(const char*);
namespace UI { class CanvasRenderer; } template <> void RegisterUnityClass<UI::CanvasRenderer>(const char*);
class UIRenderer; template <> void RegisterUnityClass<UIRenderer>(const char*);
class VFXManager; template <> void RegisterUnityClass<VFXManager>(const char*);
class VFXRenderer; template <> void RegisterUnityClass<VFXRenderer>(const char*);
class VisualEffect; template <> void RegisterUnityClass<VisualEffect>(const char*);
class VisualEffectAsset; template <> void RegisterUnityClass<VisualEffectAsset>(const char*);
class VisualEffectObject; template <> void RegisterUnityClass<VisualEffectObject>(const char*);

void RegisterAllClasses()
{
void RegisterBuiltinTypes();
RegisterBuiltinTypes();
	//Total: 105 non stripped classes
	//0. Animation
	RegisterUnityClass<Animation>("Animation");
	//1. AnimationClip
	RegisterUnityClass<AnimationClip>("Animation");
	//2. Animator
	RegisterUnityClass<Animator>("Animation");
	//3. AnimatorController
	RegisterUnityClass<AnimatorController>("Animation");
	//4. AnimatorOverrideController
	RegisterUnityClass<AnimatorOverrideController>("Animation");
	//5. Avatar
	RegisterUnityClass<Avatar>("Animation");
	//6. AvatarMask
	RegisterUnityClass<AvatarMask>("Animation");
	//7. Motion
	RegisterUnityClass<Motion>("Animation");
	//8. RuntimeAnimatorController
	RegisterUnityClass<RuntimeAnimatorController>("Animation");
	//9. AudioBehaviour
	RegisterUnityClass<AudioBehaviour>("Audio");
	//10. AudioClip
	RegisterUnityClass<AudioClip>("Audio");
	//11. AudioListener
	RegisterUnityClass<AudioListener>("Audio");
	//12. AudioManager
	RegisterUnityClass<AudioManager>("Audio");
	//13. AudioResource
	RegisterUnityClass<AudioResource>("Audio");
	//14. AudioSource
	RegisterUnityClass<AudioSource>("Audio");
	//15. SampleClip
	RegisterUnityClass<SampleClip>("Audio");
	//16. Behaviour
	RegisterUnityClass<Behaviour>("Core");
	//17. BuildSettings
	RegisterUnityClass<BuildSettings>("Core");
	//18. Camera
	RegisterUnityClass<Camera>("Core");
	//19. Component
	RegisterUnityClass<Unity::Component>("Core");
	//20. ComputeShader
	RegisterUnityClass<ComputeShader>("Core");
	//21. Cubemap
	RegisterUnityClass<Cubemap>("Core");
	//22. CubemapArray
	RegisterUnityClass<CubemapArray>("Core");
	//23. DelayedCallManager
	RegisterUnityClass<DelayedCallManager>("Core");
	//24. EditorExtension
	RegisterUnityClass<EditorExtension>("Core");
	//25. GameManager
	RegisterUnityClass<GameManager>("Core");
	//26. GameObject
	RegisterUnityClass<GameObject>("Core");
	//27. GlobalGameManager
	RegisterUnityClass<GlobalGameManager>("Core");
	//28. GraphicsSettings
	RegisterUnityClass<GraphicsSettings>("Core");
	//29. InputManager
	RegisterUnityClass<InputManager>("Core");
	//30. LODGroup
	RegisterUnityClass<LODGroup>("Core");
	//31. LevelGameManager
	RegisterUnityClass<LevelGameManager>("Core");
	//32. Light
	RegisterUnityClass<Light>("Core");
	//33. LightProbeProxyVolume
	RegisterUnityClass<LightProbeProxyVolume>("Core");
	//34. LightProbes
	RegisterUnityClass<LightProbes>("Core");
	//35. LightingSettings
	RegisterUnityClass<LightingSettings>("Core");
	//36. LightmapSettings
	RegisterUnityClass<LightmapSettings>("Core");
	//37. LowerResBlitTexture
	RegisterUnityClass<LowerResBlitTexture>("Core");
	//38. Material
	RegisterUnityClass<Material>("Core");
	//39. Mesh
	RegisterUnityClass<Mesh>("Core");
	//40. MeshFilter
	RegisterUnityClass<MeshFilter>("Core");
	//41. MeshRenderer
	RegisterUnityClass<MeshRenderer>("Core");
	//42. MonoBehaviour
	RegisterUnityClass<MonoBehaviour>("Core");
	//43. MonoManager
	RegisterUnityClass<MonoManager>("Core");
	//44. MonoScript
	RegisterUnityClass<MonoScript>("Core");
	//45. NamedObject
	RegisterUnityClass<NamedObject>("Core");
	//46. Object
	//Skipping Object
	//47. PlayerSettings
	RegisterUnityClass<PlayerSettings>("Core");
	//48. PreloadData
	RegisterUnityClass<PreloadData>("Core");
	//49. QualitySettings
	RegisterUnityClass<QualitySettings>("Core");
	//50. RayTracingShader
	RegisterUnityClass<RayTracingShader>("Core");
	//51. RectTransform
	RegisterUnityClass<UI::RectTransform>("Core");
	//52. ReflectionProbe
	RegisterUnityClass<ReflectionProbe>("Core");
	//53. RenderSettings
	RegisterUnityClass<RenderSettings>("Core");
	//54. RenderTexture
	RegisterUnityClass<RenderTexture>("Core");
	//55. Renderer
	RegisterUnityClass<Renderer>("Core");
	//56. ResourceManager
	RegisterUnityClass<ResourceManager>("Core");
	//57. RuntimeInitializeOnLoadManager
	RegisterUnityClass<RuntimeInitializeOnLoadManager>("Core");
	//58. Shader
	RegisterUnityClass<Shader>("Core");
	//59. ShaderNameRegistry
	RegisterUnityClass<ShaderNameRegistry>("Core");
	//60. Skybox
	RegisterUnityClass<Skybox>("Core");
	//61. SortingGroup
	RegisterUnityClass<SortingGroup>("Core");
	//62. Sprite
	RegisterUnityClass<Sprite>("Core");
	//63. SpriteAtlas
	RegisterUnityClass<SpriteAtlas>("Core");
	//64. SpriteRenderer
	RegisterUnityClass<SpriteRenderer>("Core");
	//65. TagManager
	RegisterUnityClass<TagManager>("Core");
	//66. TextAsset
	RegisterUnityClass<TextAsset>("Core");
	//67. Texture
	RegisterUnityClass<Texture>("Core");
	//68. Texture2D
	RegisterUnityClass<Texture2D>("Core");
	//69. Texture2DArray
	RegisterUnityClass<Texture2DArray>("Core");
	//70. Texture3D
	RegisterUnityClass<Texture3D>("Core");
	//71. TimeManager
	RegisterUnityClass<TimeManager>("Core");
	//72. Transform
	RegisterUnityClass<Transform>("Core");
	//73. PlayableDirector
	RegisterUnityClass<PlayableDirector>("Director");
	//74. Grid
	RegisterUnityClass<Grid>("Grid");
	//75. GridLayout
	RegisterUnityClass<GridLayout>("Grid");
	//76. ParticleSystem
	RegisterUnityClass<ParticleSystem>("ParticleSystem");
	//77. ParticleSystemRenderer
	RegisterUnityClass<ParticleSystemRenderer>("ParticleSystem");
	//78. BoxCollider
	RegisterUnityClass<BoxCollider>("Physics");
	//79. CapsuleCollider
	RegisterUnityClass<CapsuleCollider>("Physics");
	//80. Collider
	RegisterUnityClass<Collider>("Physics");
	//81. MeshCollider
	RegisterUnityClass<MeshCollider>("Physics");
	//82. PhysicsManager
	RegisterUnityClass<PhysicsManager>("Physics");
	//83. Rigidbody
	RegisterUnityClass<Rigidbody>("Physics");
	//84. SphereCollider
	RegisterUnityClass<SphereCollider>("Physics");
	//85. BoxCollider2D
	RegisterUnityClass<BoxCollider2D>("Physics2D");
	//86. Collider2D
	RegisterUnityClass<Collider2D>("Physics2D");
	//87. CompositeCollider2D
	RegisterUnityClass<CompositeCollider2D>("Physics2D");
	//88. Physics2DSettings
	RegisterUnityClass<Physics2DSettings>("Physics2D");
	//89. PolygonCollider2D
	RegisterUnityClass<PolygonCollider2D>("Physics2D");
	//90. Rigidbody2D
	RegisterUnityClass<Rigidbody2D>("Physics2D");
	//91. Terrain
	RegisterUnityClass<Terrain>("Terrain");
	//92. TerrainData
	RegisterUnityClass<TerrainData>("Terrain");
	//93. Font
	RegisterUnityClass<TextRendering::Font>("TextRendering");
	//94. Tilemap
	RegisterUnityClass<Tilemap>("Tilemap");
	//95. TilemapRenderer
	RegisterUnityClass<TilemapRenderer>("Tilemap");
	//96. Canvas
	RegisterUnityClass<UI::Canvas>("UI");
	//97. CanvasGroup
	RegisterUnityClass<UI::CanvasGroup>("UI");
	//98. CanvasRenderer
	RegisterUnityClass<UI::CanvasRenderer>("UI");
	//99. UIRenderer
	RegisterUnityClass<UIRenderer>("UIElements");
	//100. VFXManager
	RegisterUnityClass<VFXManager>("VFX");
	//101. VFXRenderer
	RegisterUnityClass<VFXRenderer>("VFX");
	//102. VisualEffect
	RegisterUnityClass<VisualEffect>("VFX");
	//103. VisualEffectAsset
	RegisterUnityClass<VisualEffectAsset>("VFX");
	//104. VisualEffectObject
	RegisterUnityClass<VisualEffectObject>("VFX");

}
