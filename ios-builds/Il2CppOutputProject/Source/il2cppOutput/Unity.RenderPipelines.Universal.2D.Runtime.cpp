#include "pch-cpp.hpp"





template <typename R>
struct VirtualFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R>
struct InterfaceFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};

struct Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3;
struct Action_2_t38DEBB6BD6AE1CA882236F63F7E1DB3781D38994;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771;
struct Action_tD00B0A84D7945E50C2DFFC28EFEE6ED44ED2AD07;
struct BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9;
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184;
struct CancellationTokenSource_tAAE1E0033BCFC233801F8CB4CED5C852B350CB7B;
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3;
struct DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E;
struct IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4;
struct MethodInfo_t;
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71;
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C;
struct PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195;
struct PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC;
struct RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27;
struct String_t;
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD;

IL2CPP_EXTERN_C RuntimeClass* Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* RenderPipelineManager_t44E0175AAADDD5487593AEF2B009B1B154957CDB_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponent_TisCamera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_m64AC6C06DD93C5FB249091FEC84FA8475457CCC4_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172_RuntimeMethod_var;
struct Delegate_t_marshaled_com;
struct Delegate_t_marshaled_pinvoke;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_t586DC3021D6B0C910E4C2242911EDBBEA4058A65 
{
};
struct U24BurstDirectCallInitializer_tBD21393165D5E3967C8A76A971B5CE96E681231C  : public RuntimeObject
{
};
struct BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8  : public RuntimeObject
{
};
struct BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9  : public RuntimeObject
{
	bool ____enableBurstCompilation;
	bool ____enableBurstSafetyChecks;
	bool ___U3CIsGlobalU3Ek__BackingField;
	Action_tD00B0A84D7945E50C2DFFC28EFEE6ED44ED2AD07* ___U3COptionsChangedU3Ek__BackingField;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	double ___m_value;
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2  : public ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_pinvoke
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_com
{
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
};
struct Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 
{
	float ___m00;
	float ___m10;
	float ___m20;
	float ___m30;
	float ___m01;
	float ___m11;
	float ___m21;
	float ___m31;
	float ___m02;
	float ___m12;
	float ___m22;
	float ___m32;
	float ___m03;
	float ___m13;
	float ___m23;
	float ___m33;
};
struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 
{
	float ___x;
	float ___y;
	float ___z;
	float ___w;
};
struct Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D 
{
	float ___m_XMin;
	float ___m_YMin;
	float ___m_Width;
	float ___m_Height;
};
struct ShaderTagId_t453E2085B5EE9448FF75E550CAB111EFF690ECB0 
{
	int32_t ___m_Id;
};
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	float ___m_value;
};
struct Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A 
{
	int32_t ___m_X;
	int32_t ___m_Y;
};
struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 
{
	float ___x;
	float ___y;
	float ___z;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct Delegate_t  : public RuntimeObject
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	RuntimeObject* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	bool ___method_is_virtual;
};
struct Delegate_t_marshaled_pinvoke
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct Delegate_t_marshaled_com
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct FilterMode_t4AD57F1A3FE272D650E0E688BA044AE872BD2A34 
{
	int32_t ___value__;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C  : public RuntimeObject
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
	intptr_t ___m_CachedPtr;
};
struct PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC  : public RuntimeObject
{
	RuntimeObject* ___m_Component;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* ___m_SerializableComponent;
	float ___originalOrthoSize;
	bool ___hasPostProcessLayer;
	bool ___cropFrameXAndY;
	bool ___cropFrameXOrY;
	bool ___useStretchFill;
	int32_t ___zoom;
	bool ___useOffscreenRT;
	int32_t ___offscreenRTWidth;
	int32_t ___offscreenRTHeight;
	Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___pixelRect;
	float ___orthoSize;
	float ___unitsPerPixel;
	int32_t ___cinemachineVCamZoom;
	bool ___requiresUpscaling;
};
struct ScriptableRenderContext_t5AB09B3602BEB456E0DC3D53926D3A3BDAF08E36 
{
	intptr_t ___m_Ptr;
};
struct CropFrame_tC853726431832FAB21E282B9B77744623F6A26AB 
{
	int32_t ___value__;
};
struct GridSnapping_t92E7BC01CABCE60B2D33954E5B410F37520460C5 
{
	int32_t ___value__;
};
struct PixelPerfectFilterMode_t6A5EF3D253B10A9E4728BE231FAB0E8A206882E8 
{
	int32_t ___value__;
};
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct MulticastDelegate_t  : public Delegate_t
{
	DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771* ___delegates;
};
struct MulticastDelegate_t_marshaled_pinvoke : public Delegate_t_marshaled_pinvoke
{
	Delegate_t_marshaled_pinvoke** ___delegates;
};
struct MulticastDelegate_t_marshaled_com : public Delegate_t_marshaled_com
{
	Delegate_t_marshaled_com** ___delegates;
};
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3  : public MulticastDelegate_t
{
};
struct Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};
struct RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27  : public Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700
{
};
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
	uint32_t ___m_NonSerializedVersion;
};
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
	CancellationTokenSource_tAAE1E0033BCFC233801F8CB4CED5C852B350CB7B* ___m_CancellationTokenSource;
};
struct PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	int32_t ___m_AssetsPPU;
	int32_t ___m_RefResolutionX;
	int32_t ___m_RefResolutionY;
	int32_t ___m_CropFrame;
	int32_t ___m_GridSnapping;
	int32_t ___m_FilterMode;
	Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* ___m_Camera;
	PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* ___m_Internal;
	bool ___m_CinemachineCompatibilityMode;
};
struct BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_StaticFields
{
	bool ____IsEnabled;
	BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9* ___Options;
	MethodInfo_t* ___DummyMethodInfo;
};
struct BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9_StaticFields
{
	bool ___ForceDisableBurstCompilation;
	bool ___ForceBurstCompilationSynchronously;
	bool ___IsSecondaryUnityProcess;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct IntPtr_t_StaticFields
{
	intptr_t ___Zero;
};
struct Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6_StaticFields
{
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 ___zeroMatrix;
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 ___identityMatrix;
};
struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_StaticFields
{
	Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___identityQuaternion;
};
struct Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A_StaticFields
{
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Zero;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_One;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Up;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Down;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Left;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Right;
};
struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___zeroVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___oneVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___upVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___downVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___leftVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___rightVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___forwardVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___backVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___positiveInfinityVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___negativeInfinityVector;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_StaticFields
{
	int32_t ___OffsetOfInstanceIDInCPlusPlusObject;
};
struct ScriptableRenderContext_t5AB09B3602BEB456E0DC3D53926D3A3BDAF08E36_StaticFields
{
	ShaderTagId_t453E2085B5EE9448FF75E550CAB111EFF690ECB0 ___kRenderTypeTag;
};
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700_StaticFields
{
	int32_t ___GenerateAllMips;
};
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_StaticFields
{
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPreCull;
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPreRender;
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPostRender;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif


IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Action_2__ctor_m80999490097638177C6B639CEA321424D5BB0991_gshared (Action_2_t38DEBB6BD6AE1CA882236F63F7E1DB3781D38994* __this, RuntimeObject* ___0_object, intptr_t ___1_method, const RuntimeMethod* method) ;

IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelPerfectCameraInternal_CorrectCinemachineOrthoSize_m885222B60F8A525214DE5D945448F245E8C9A4D4 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, float ___0_targetOrthoSize, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, int32_t ___0_x, int32_t ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* Camera_get_targetTexture_mC856D7FF8351476068D04E245E4F08F5C56A55BD (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___0_x, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Screen_get_width_mF608FF3252213E7EFA1F0D2F744C28110E9E5AC9 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Screen_get_height_m01A3102DE71EE1FBEA51D09D6B0261CF864FE8F9 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Transform_get_position_m69CD5FA214FDAE7BB701552943674846C220FDE1 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelPerfectCamera_RoundToPixel_mDAE91343C303FF75DDCDBFF55A44675ADF34A919 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_position, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Subtraction_mE42023FF80067CB44A1D4A27EB7CF2B24CABB828_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___1_b, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___1_b, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline (const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 Matrix4x4_TRS_mCC04FD47347234B451ACC6CCD2CE6D02E1E0E1E3 (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_pos, Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___1_q, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___2_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 Matrix4x4_get_inverse_m4F4A881CD789281EA90EB68CFD39F36C8A81E6BD (Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Transform_get_rotation_m32AF40CA0D50C797DA639A696F8EAEC7524C179C (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 Matrix4x4_Rotate_m015442530DFF5651458BBFDFB3CBC9180FC09D9E (Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___0_q, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 Matrix4x4_Scale_m95902D2A889FD6E7B04BBEAE6FAE5D6D8A88E642 (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_vector, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 Matrix4x4_op_Multiply_m75E91775655DCA8DFC8EDE0AB787285BB3935162 (Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 ___0_lhs, Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 ___1_rhs, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_worldToCameraMatrix_mC199F02E435CE7261F7EECD1FD78A33EA96ABC0D (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 ___0_value, const RuntimeMethod* method) ;
inline Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* Component_GetComponent_TisCamera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_m64AC6C06DD93C5FB249091FEC84FA8475457CCC4 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal__ctor_m6BC5985512637F4B2AAD903E5D411B954CB8E795 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, RuntimeObject* ___0_component, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_UpdateCameraProperties_mDD3B992B0A44231F2FD60075821015FDDD625E99 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A PixelPerfectCamera_get_cameraRTSize_mE0CDEFF078B313BDA170E6EDC66A8A1C8F8B0656 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal_CalculateCameraProperties_mB201DE82608102113237D5509D085C7ED74BB9FE (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, int32_t ___0_screenWidth, int32_t ___1_screenHeight, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D PixelPerfectCameraInternal_CalculateFinalBlitPixelRect_mDBD399AEAA750ACC3B03C21110E361758DBC0C82 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, int32_t ___0_screenWidth, int32_t ___1_screenHeight, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_pixelRect_m4A9504577204D4E72B39BFB637ED808B778568A5 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect__ctor_m18C3033D135097BEE424AAA68D91C706D2647F23_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_x, float ___1_y, float ___2_width, float ___3_height, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_rect_mA81158BC169AF8674DE240AE9460FC5A0EADBB19 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_PixelSnap_mB4A7DDD3EC5A6BEE7086125E61E3A716CD25BFD7 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_orthographicSize_m76DD021032ACB3DDBD052B75EC66DCE3A7295A5C (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectRendering_set_pixelSnapSpacing_mCE94C24F8C7EBA84D8C6C47F10A32DDBD3329904 (float ___0_value, const RuntimeMethod* method) ;
inline void Action_2__ctor_mBEB5B9B513FE305CE98CA8065CC6E6CC0E5A4D51 (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* __this, RuntimeObject* ___0_object, intptr_t ___1_method, const RuntimeMethod* method)
{
	((  void (*) (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3*, RuntimeObject*, intptr_t, const RuntimeMethod*))Action_2__ctor_m80999490097638177C6B639CEA321424D5BB0991_gshared)(__this, ___0_object, ___1_method, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderPipelineManager_add_beginCameraRendering_m44DF94A62BE65F929101983FACE63BA4FE4B584A (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderPipelineManager_add_endCameraRendering_m664BCFE6FCD9D3172DF3157777EA3B45BF11476C (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderPipelineManager_remove_beginCameraRendering_m6A9B576247B531A6C1C715870A37343AC702976E (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderPipelineManager_remove_endCameraRendering_m0EC9DE4937A7B58074E35E75CCDE819D4A1E302A (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_ResetWorldToCameraMatrix_m25E544C8F31680DC08C58F7416AFD77DA3DB3F91 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E (MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D Rect_get_zero_m5341D8B63DEF1F4C308A685EEC8CFEA12A396C8D (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___0_x, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52 (int32_t ___0_val1, int32_t ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF (int32_t ___0_val1, int32_t ___1_val2, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Rect_op_Equality_mF2A038255CAF5F1E86079B9EE0FC96DE54307C1F_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___0_lhs, Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___1_rhs, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_x_mAB91AB71898A20762BC66FD0723C4C739C4C3406_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_y_mDE91F4B98A6E8623EFB1250FF6526D5DB5855629_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline (float ___0_f, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_x_mB267B718E0D067F2BAE31BA477647FBF964916EB_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_y_mC733E8D49F3CE21B2A3D40A1B72D687F22C97F49_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_cropFrame_m049E2C02AED3C1C244B3AFD1E0B6104AFCC33A60 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_CropFrame;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_cropFrame_m6CE5030A1594EE99DA8FAE0B032182DD88033A2E (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___m_CropFrame = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_gridSnapping_mEF34B7A6CFF739935B11FF478B1F096C5D321A80 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_GridSnapping;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_gridSnapping_m54000C548DDFE0CF863166F82D79E55F5D338796 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___m_GridSnapping = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelPerfectCamera_get_orthographicSize_m64808DF017F8BE75E1581746CB142F22A151A1A2 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_0 = __this->___m_Internal;
		NullCheck(L_0);
		float L_1 = L_0->___orthoSize;
		return L_1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_assetsPPU_mDF5D73D22D07FE0CAA7761673527FF30BD10EB4D (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_AssetsPPU;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_assetsPPU_m592A5390903DCB8D20122E25694705CA311B9CCE (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B2_0 = NULL;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B1_0 = NULL;
	int32_t G_B3_0 = 0;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B3_1 = NULL;
	{
		int32_t L_0 = ___0_value;
		if ((((int32_t)L_0) > ((int32_t)0)))
		{
			G_B2_0 = __this;
			goto IL_0008;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = 1;
		G_B3_1 = G_B1_0;
		goto IL_0009;
	}

IL_0008:
	{
		int32_t L_1 = ___0_value;
		G_B3_0 = L_1;
		G_B3_1 = G_B2_0;
	}

IL_0009:
	{
		NullCheck(G_B3_1);
		G_B3_1->___m_AssetsPPU = G_B3_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_refResolutionX_m5523E53A067744D8A32B721DE936B800B43790C0 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_RefResolutionX;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_refResolutionX_mE3F8317F6E4D4D8D355EF85CF8751C77A655DEB9 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B2_0 = NULL;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B1_0 = NULL;
	int32_t G_B3_0 = 0;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B3_1 = NULL;
	{
		int32_t L_0 = ___0_value;
		if ((((int32_t)L_0) > ((int32_t)0)))
		{
			G_B2_0 = __this;
			goto IL_0008;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = 1;
		G_B3_1 = G_B1_0;
		goto IL_0009;
	}

IL_0008:
	{
		int32_t L_1 = ___0_value;
		G_B3_0 = L_1;
		G_B3_1 = G_B2_0;
	}

IL_0009:
	{
		NullCheck(G_B3_1);
		G_B3_1->___m_RefResolutionX = G_B3_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_refResolutionY_m2E7C84EA248898609CA5ADD30E211976A44CD521 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_RefResolutionY;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_refResolutionY_m6440FB4E82E6E04091CDC83C8CF76E46CBF39370 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B2_0 = NULL;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B1_0 = NULL;
	int32_t G_B3_0 = 0;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B3_1 = NULL;
	{
		int32_t L_0 = ___0_value;
		if ((((int32_t)L_0) > ((int32_t)0)))
		{
			G_B2_0 = __this;
			goto IL_0008;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = 1;
		G_B3_1 = G_B1_0;
		goto IL_0009;
	}

IL_0008:
	{
		int32_t L_1 = ___0_value;
		G_B3_0 = L_1;
		G_B3_1 = G_B2_0;
	}

IL_0009:
	{
		NullCheck(G_B3_1);
		G_B3_1->___m_RefResolutionY = G_B3_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_upscaleRT_mE8E62C676679B7D8E889C9CC73996E36A677A7A5 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_GridSnapping;
		return (bool)((((int32_t)L_0) == ((int32_t)2))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_upscaleRT_mBF56C58F57DF202E220A51A6A0BCF447987ED9E8 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, bool ___0_value, const RuntimeMethod* method) 
{
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B2_0 = NULL;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B1_0 = NULL;
	int32_t G_B3_0 = 0;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B3_1 = NULL;
	{
		bool L_0 = ___0_value;
		if (L_0)
		{
			G_B2_0 = __this;
			goto IL_0007;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = 0;
		G_B3_1 = G_B1_0;
		goto IL_0008;
	}

IL_0007:
	{
		G_B3_0 = 2;
		G_B3_1 = G_B2_0;
	}

IL_0008:
	{
		NullCheck(G_B3_1);
		G_B3_1->___m_GridSnapping = G_B3_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_pixelSnapping_m016C9FFAF9D3A769188F842CC09060AD3B12E593 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_GridSnapping;
		return (bool)((((int32_t)L_0) == ((int32_t)1))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_pixelSnapping_mD5038B0AACF71EE91A9223E753091FD5E1BC756B (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, bool ___0_value, const RuntimeMethod* method) 
{
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B2_0 = NULL;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B1_0 = NULL;
	int32_t G_B3_0 = 0;
	PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* G_B3_1 = NULL;
	{
		bool L_0 = ___0_value;
		if (L_0)
		{
			G_B2_0 = __this;
			goto IL_0007;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = 0;
		G_B3_1 = G_B1_0;
		goto IL_0008;
	}

IL_0007:
	{
		G_B3_0 = 1;
		G_B3_1 = G_B2_0;
	}

IL_0008:
	{
		NullCheck(G_B3_1);
		G_B3_1->___m_GridSnapping = G_B3_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_cropFrameX_m1E3DA844B064A4A258919FD41AA39AA819CAB405 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_CropFrame;
		if ((((int32_t)L_0) == ((int32_t)4)))
		{
			goto IL_001c;
		}
	}
	{
		int32_t L_1 = __this->___m_CropFrame;
		if ((((int32_t)L_1) == ((int32_t)3)))
		{
			goto IL_001c;
		}
	}
	{
		int32_t L_2 = __this->___m_CropFrame;
		return (bool)((((int32_t)L_2) == ((int32_t)1))? 1 : 0);
	}

IL_001c:
	{
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_cropFrameX_m3C84FDF660EC143C9D15EA0F2EBA529E8C29BB60 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		if (!L_0)
		{
			goto IL_0024;
		}
	}
	{
		int32_t L_1 = __this->___m_CropFrame;
		if (L_1)
		{
			goto IL_0013;
		}
	}
	{
		__this->___m_CropFrame = 1;
		return;
	}

IL_0013:
	{
		int32_t L_2 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_2) == ((uint32_t)2))))
		{
			goto IL_004e;
		}
	}
	{
		__this->___m_CropFrame = 3;
		return;
	}

IL_0024:
	{
		int32_t L_3 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_3) == ((uint32_t)1))))
		{
			goto IL_0035;
		}
	}
	{
		__this->___m_CropFrame = 0;
		return;
	}

IL_0035:
	{
		int32_t L_4 = __this->___m_CropFrame;
		if ((((int32_t)L_4) == ((int32_t)3)))
		{
			goto IL_0047;
		}
	}
	{
		int32_t L_5 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_5) == ((uint32_t)4))))
		{
			goto IL_004e;
		}
	}

IL_0047:
	{
		__this->___m_CropFrame = 2;
	}

IL_004e:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_cropFrameY_mB6069E68FF57C4F9B5C5D45F434AA4E23DF6C166 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_CropFrame;
		if ((((int32_t)L_0) == ((int32_t)4)))
		{
			goto IL_001c;
		}
	}
	{
		int32_t L_1 = __this->___m_CropFrame;
		if ((((int32_t)L_1) == ((int32_t)3)))
		{
			goto IL_001c;
		}
	}
	{
		int32_t L_2 = __this->___m_CropFrame;
		return (bool)((((int32_t)L_2) == ((int32_t)2))? 1 : 0);
	}

IL_001c:
	{
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_cropFrameY_mF679DFACB003DAB2B21740E21CECD739C9754B6D (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		if (!L_0)
		{
			goto IL_0024;
		}
	}
	{
		int32_t L_1 = __this->___m_CropFrame;
		if (L_1)
		{
			goto IL_0013;
		}
	}
	{
		__this->___m_CropFrame = 2;
		return;
	}

IL_0013:
	{
		int32_t L_2 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_2) == ((uint32_t)1))))
		{
			goto IL_004e;
		}
	}
	{
		__this->___m_CropFrame = 3;
		return;
	}

IL_0024:
	{
		int32_t L_3 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_3) == ((uint32_t)2))))
		{
			goto IL_0035;
		}
	}
	{
		__this->___m_CropFrame = 0;
		return;
	}

IL_0035:
	{
		int32_t L_4 = __this->___m_CropFrame;
		if ((((int32_t)L_4) == ((int32_t)3)))
		{
			goto IL_0047;
		}
	}
	{
		int32_t L_5 = __this->___m_CropFrame;
		if ((!(((uint32_t)L_5) == ((uint32_t)4))))
		{
			goto IL_004e;
		}
	}

IL_0047:
	{
		__this->___m_CropFrame = 1;
	}

IL_004e:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_stretchFill_mE5BAB5E287F73A850CC9870D84FA176F7560BC44 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_CropFrame;
		return (bool)((((int32_t)L_0) == ((int32_t)4))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_set_stretchFill_m0D6DA91446635FA665C3D405875B8237B3E1E2FD (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		if (!L_0)
		{
			goto IL_000b;
		}
	}
	{
		__this->___m_CropFrame = 4;
		return;
	}

IL_000b:
	{
		__this->___m_CropFrame = 3;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_pixelRatio_m43A1ECE99E8FD38158D1AC65011DD98B50BD3A60 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___m_CinemachineCompatibilityMode;
		if (!L_0)
		{
			goto IL_0035;
		}
	}
	{
		int32_t L_1 = __this->___m_GridSnapping;
		if ((!(((uint32_t)L_1) == ((uint32_t)2))))
		{
			goto IL_0029;
		}
	}
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_2 = __this->___m_Internal;
		NullCheck(L_2);
		int32_t L_3 = L_2->___zoom;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_4 = __this->___m_Internal;
		NullCheck(L_4);
		int32_t L_5 = L_4->___cinemachineVCamZoom;
		return ((int32_t)il2cpp_codegen_multiply(L_3, L_5));
	}

IL_0029:
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_6 = __this->___m_Internal;
		NullCheck(L_6);
		int32_t L_7 = L_6->___cinemachineVCamZoom;
		return L_7;
	}

IL_0035:
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_8 = __this->___m_Internal;
		NullCheck(L_8);
		int32_t L_9 = L_8->___zoom;
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelPerfectCamera_get_requiresUpscalePass_mB8C4EA7518270939C98E1868DCF65DE8385A23FB (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_0 = __this->___m_Internal;
		NullCheck(L_0);
		bool L_1 = L_0->___requiresUpscaling;
		return L_1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelPerfectCamera_RoundToPixel_mDAE91343C303FF75DDCDBFF55A44675ADF34A919 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_position, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_1;
	memset((&V_1), 0, sizeof(V_1));
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_0 = __this->___m_Internal;
		NullCheck(L_0);
		float L_1 = L_0->___unitsPerPixel;
		V_0 = L_1;
		float L_2 = V_0;
		if ((!(((float)L_2) == ((float)(0.0f)))))
		{
			goto IL_0016;
		}
	}
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3 = ___0_position;
		return L_3;
	}

IL_0016:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___0_position;
		float L_5 = L_4.___x;
		float L_6 = V_0;
		float L_7;
		L_7 = bankers_roundf(((float)(L_5/L_6)));
		float L_8 = V_0;
		(&V_1)->___x = ((float)il2cpp_codegen_multiply(L_7, L_8));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9 = ___0_position;
		float L_10 = L_9.___y;
		float L_11 = V_0;
		float L_12;
		L_12 = bankers_roundf(((float)(L_10/L_11)));
		float L_13 = V_0;
		(&V_1)->___y = ((float)il2cpp_codegen_multiply(L_12, L_13));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_14 = ___0_position;
		float L_15 = L_14.___z;
		float L_16 = V_0;
		float L_17;
		L_17 = bankers_roundf(((float)(L_15/L_16)));
		float L_18 = V_0;
		(&V_1)->___z = ((float)il2cpp_codegen_multiply(L_17, L_18));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_19 = V_1;
		return L_19;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelPerfectCamera_CorrectCinemachineOrthoSize_m980A6969D95EA0F73DD16E9E3DA8E22565D94FB8 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, float ___0_targetOrthoSize, const RuntimeMethod* method) 
{
	{
		__this->___m_CinemachineCompatibilityMode = (bool)1;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_0 = __this->___m_Internal;
		if (L_0)
		{
			goto IL_0011;
		}
	}
	{
		float L_1 = ___0_targetOrthoSize;
		return L_1;
	}

IL_0011:
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_2 = __this->___m_Internal;
		float L_3 = ___0_targetOrthoSize;
		NullCheck(L_2);
		float L_4;
		L_4 = PixelPerfectCameraInternal_CorrectCinemachineOrthoSize_m885222B60F8A525214DE5D945448F245E8C9A4D4(L_2, L_3, NULL);
		return L_4;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelPerfectCamera_get_finalBlitFilterMode_m7DE0B801BB4BD1B1CD41AB0EF6CB9A2741B006AF (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___m_FilterMode;
		if (!L_0)
		{
			goto IL_000a;
		}
	}
	{
		return (int32_t)(0);
	}

IL_000a:
	{
		return (int32_t)(1);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A PixelPerfectCamera_get_offscreenRTSize_mB23A1DC33EC0A8422CF1B4D618D291E2A3BD2F5B (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_0 = __this->___m_Internal;
		NullCheck(L_0);
		int32_t L_1 = L_0->___offscreenRTWidth;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_2 = __this->___m_Internal;
		NullCheck(L_2);
		int32_t L_3 = L_2->___offscreenRTHeight;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_4;
		memset((&L_4), 0, sizeof(L_4));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_4), L_1, L_3, NULL);
		return L_4;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A PixelPerfectCamera_get_cameraRTSize_mE0CDEFF078B313BDA170E6EDC66A8A1C8F8B0656 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* V_0 = NULL;
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = __this->___m_Camera;
		NullCheck(L_0);
		RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* L_1;
		L_1 = Camera_get_targetTexture_mC856D7FF8351476068D04E245E4F08F5C56A55BD(L_0, NULL);
		V_0 = L_1;
		RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* L_2 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_3;
		L_3 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_2, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_3)
		{
			goto IL_0027;
		}
	}
	{
		RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* L_4 = V_0;
		NullCheck(L_4);
		int32_t L_5;
		L_5 = VirtualFuncInvoker0< int32_t >::Invoke(5, L_4);
		RenderTexture_tBA90C4C3AD9EECCFDDCC632D97C29FAB80D60D27* L_6 = V_0;
		NullCheck(L_6);
		int32_t L_7;
		L_7 = VirtualFuncInvoker0< int32_t >::Invoke(7, L_6);
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_8;
		memset((&L_8), 0, sizeof(L_8));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_8), L_5, L_7, NULL);
		return L_8;
	}

IL_0027:
	{
		int32_t L_9;
		L_9 = Screen_get_width_mF608FF3252213E7EFA1F0D2F744C28110E9E5AC9(NULL);
		int32_t L_10;
		L_10 = Screen_get_height_m01A3102DE71EE1FBEA51D09D6B0261CF864FE8F9(NULL);
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_11;
		memset((&L_11), 0, sizeof(L_11));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_11), L_9, L_10, NULL);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_PixelSnap_mB4A7DDD3EC5A6BEE7086125E61E3A716CD25BFD7 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 V_2;
	memset((&V_2), 0, sizeof(V_2));
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 V_3;
	memset((&V_3), 0, sizeof(V_3));
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 V_4;
	memset((&V_4), 0, sizeof(V_4));
	Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 V_5;
	memset((&V_5), 0, sizeof(V_5));
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = __this->___m_Camera;
		NullCheck(L_0);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_1;
		L_1 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_0, NULL);
		NullCheck(L_1);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2;
		L_2 = Transform_get_position_m69CD5FA214FDAE7BB701552943674846C220FDE1(L_1, NULL);
		V_0 = L_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3 = V_0;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4;
		L_4 = PixelPerfectCamera_RoundToPixel_mDAE91343C303FF75DDCDBFF55A44675ADF34A919(__this, L_3, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_5 = V_0;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6;
		L_6 = Vector3_op_Subtraction_mE42023FF80067CB44A1D4A27EB7CF2B24CABB828_inline(L_4, L_5, NULL);
		V_1 = L_6;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_7 = V_1;
		float L_8 = L_7.___z;
		(&V_1)->___z = ((-L_8));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9 = V_0;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = V_1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_11;
		L_11 = Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline(L_9, L_10, NULL);
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_12;
		L_12 = Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline(NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13;
		L_13 = Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline(NULL);
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_14;
		L_14 = Matrix4x4_TRS_mCC04FD47347234B451ACC6CCD2CE6D02E1E0E1E3(L_11, L_12, L_13, NULL);
		V_5 = L_14;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_15;
		L_15 = Matrix4x4_get_inverse_m4F4A881CD789281EA90EB68CFD39F36C8A81E6BD((&V_5), NULL);
		V_2 = L_15;
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_16 = __this->___m_Camera;
		NullCheck(L_16);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_17;
		L_17 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_16, NULL);
		NullCheck(L_17);
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_18;
		L_18 = Transform_get_rotation_m32AF40CA0D50C797DA639A696F8EAEC7524C179C(L_17, NULL);
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_19;
		L_19 = Matrix4x4_Rotate_m015442530DFF5651458BBFDFB3CBC9180FC09D9E(L_18, NULL);
		V_5 = L_19;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_20;
		L_20 = Matrix4x4_get_inverse_m4F4A881CD789281EA90EB68CFD39F36C8A81E6BD((&V_5), NULL);
		V_3 = L_20;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_21;
		memset((&L_21), 0, sizeof(L_21));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_21), (1.0f), (1.0f), (-1.0f), NULL);
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_22;
		L_22 = Matrix4x4_Scale_m95902D2A889FD6E7B04BBEAE6FAE5D6D8A88E642(L_21, NULL);
		V_4 = L_22;
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_23 = __this->___m_Camera;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_24 = V_4;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_25 = V_3;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_26;
		L_26 = Matrix4x4_op_Multiply_m75E91775655DCA8DFC8EDE0AB787285BB3935162(L_24, L_25, NULL);
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_27 = V_2;
		Matrix4x4_tDB70CF134A14BA38190C59AA700BCE10E2AED3E6 L_28;
		L_28 = Matrix4x4_op_Multiply_m75E91775655DCA8DFC8EDE0AB787285BB3935162(L_26, L_27, NULL);
		NullCheck(L_23);
		Camera_set_worldToCameraMatrix_mC199F02E435CE7261F7EECD1FD78A33EA96ABC0D(L_23, L_28, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_Awake_m25F30BF98810839D5A735CCAE9F57469B0398A99 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponent_TisCamera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_m64AC6C06DD93C5FB249091FEC84FA8475457CCC4_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0;
		L_0 = Component_GetComponent_TisCamera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_m64AC6C06DD93C5FB249091FEC84FA8475457CCC4(__this, Component_GetComponent_TisCamera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_m64AC6C06DD93C5FB249091FEC84FA8475457CCC4_RuntimeMethod_var);
		__this->___m_Camera = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___m_Camera), (void*)L_0);
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_1 = (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC*)il2cpp_codegen_object_new(PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC_il2cpp_TypeInfo_var);
		PixelPerfectCameraInternal__ctor_m6BC5985512637F4B2AAD903E5D411B954CB8E795(L_1, __this, NULL);
		__this->___m_Internal = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___m_Internal), (void*)L_1);
		PixelPerfectCamera_UpdateCameraProperties_mDD3B992B0A44231F2FD60075821015FDDD625E99(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_UpdateCameraProperties_mDD3B992B0A44231F2FD60075821015FDDD625E99 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_0;
		L_0 = PixelPerfectCamera_get_cameraRTSize_mE0CDEFF078B313BDA170E6EDC66A8A1C8F8B0656(__this, NULL);
		V_0 = L_0;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_1 = __this->___m_Internal;
		int32_t L_2;
		L_2 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_0), NULL);
		int32_t L_3;
		L_3 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_0), NULL);
		NullCheck(L_1);
		PixelPerfectCameraInternal_CalculateCameraProperties_mB201DE82608102113237D5509D085C7ED74BB9FE(L_1, L_2, L_3, NULL);
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_4 = __this->___m_Internal;
		NullCheck(L_4);
		bool L_5 = L_4->___useOffscreenRT;
		if (!L_5)
		{
			goto IL_0052;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_6 = __this->___m_Camera;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_7 = __this->___m_Internal;
		int32_t L_8;
		L_8 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_0), NULL);
		int32_t L_9;
		L_9 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_0), NULL);
		NullCheck(L_7);
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_10;
		L_10 = PixelPerfectCameraInternal_CalculateFinalBlitPixelRect_mDBD399AEAA750ACC3B03C21110E361758DBC0C82(L_7, L_8, L_9, NULL);
		NullCheck(L_6);
		Camera_set_pixelRect_m4A9504577204D4E72B39BFB637ED808B778568A5(L_6, L_10, NULL);
		return;
	}

IL_0052:
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_11 = __this->___m_Camera;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_12;
		memset((&L_12), 0, sizeof(L_12));
		Rect__ctor_m18C3033D135097BEE424AAA68D91C706D2647F23_inline((&L_12), (0.0f), (0.0f), (1.0f), (1.0f), NULL);
		NullCheck(L_11);
		Camera_set_rect_mA81158BC169AF8674DE240AE9460FC5A0EADBB19(L_11, L_12, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, ScriptableRenderContext_t5AB09B3602BEB456E0DC3D53926D3A3BDAF08E36 ___0_context, Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* ___1_camera, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = ___1_camera;
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_1 = __this->___m_Camera;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, L_1, NULL);
		if (!L_2)
		{
			goto IL_0048;
		}
	}
	{
		PixelPerfectCamera_UpdateCameraProperties_mDD3B992B0A44231F2FD60075821015FDDD625E99(__this, NULL);
		PixelPerfectCamera_PixelSnap_mB4A7DDD3EC5A6BEE7086125E61E3A716CD25BFD7(__this, NULL);
		bool L_3 = __this->___m_CinemachineCompatibilityMode;
		if (L_3)
		{
			goto IL_0038;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_4 = __this->___m_Camera;
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_5 = __this->___m_Internal;
		NullCheck(L_5);
		float L_6 = L_5->___orthoSize;
		NullCheck(L_4);
		Camera_set_orthographicSize_m76DD021032ACB3DDBD052B75EC66DCE3A7295A5C(L_4, L_6, NULL);
	}

IL_0038:
	{
		PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* L_7 = __this->___m_Internal;
		NullCheck(L_7);
		float L_8 = L_7->___unitsPerPixel;
		PixelPerfectRendering_set_pixelSnapSpacing_mCE94C24F8C7EBA84D8C6C47F10A32DDBD3329904(L_8, NULL);
	}

IL_0048:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, ScriptableRenderContext_t5AB09B3602BEB456E0DC3D53926D3A3BDAF08E36 ___0_context, Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* ___1_camera, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = ___1_camera;
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_1 = __this->___m_Camera;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, L_1, NULL);
		if (!L_2)
		{
			goto IL_0018;
		}
	}
	{
		PixelPerfectRendering_set_pixelSnapSpacing_mCE94C24F8C7EBA84D8C6C47F10A32DDBD3329904((0.0f), NULL);
	}

IL_0018:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnEnable_m1FC65FDDB4482827D6111686FA435CDCBA163CC1 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&RenderPipelineManager_t44E0175AAADDD5487593AEF2B009B1B154957CDB_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		__this->___m_CinemachineCompatibilityMode = (bool)0;
		Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* L_0 = (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3*)il2cpp_codegen_object_new(Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		Action_2__ctor_mBEB5B9B513FE305CE98CA8065CC6E6CC0E5A4D51(L_0, __this, (intptr_t)((void*)PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A_RuntimeMethod_var), NULL);
		il2cpp_codegen_runtime_class_init_inline(RenderPipelineManager_t44E0175AAADDD5487593AEF2B009B1B154957CDB_il2cpp_TypeInfo_var);
		RenderPipelineManager_add_beginCameraRendering_m44DF94A62BE65F929101983FACE63BA4FE4B584A(L_0, NULL);
		Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* L_1 = (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3*)il2cpp_codegen_object_new(Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		Action_2__ctor_mBEB5B9B513FE305CE98CA8065CC6E6CC0E5A4D51(L_1, __this, (intptr_t)((void*)PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172_RuntimeMethod_var), NULL);
		RenderPipelineManager_add_endCameraRendering_m664BCFE6FCD9D3172DF3157777EA3B45BF11476C(L_1, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnDisable_m3F6D16F4423CD5622C68EA0138A37E3947D120A9 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&RenderPipelineManager_t44E0175AAADDD5487593AEF2B009B1B154957CDB_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* L_0 = (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3*)il2cpp_codegen_object_new(Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		Action_2__ctor_mBEB5B9B513FE305CE98CA8065CC6E6CC0E5A4D51(L_0, __this, (intptr_t)((void*)PixelPerfectCamera_OnBeginCameraRendering_m587FA327280AC017B622DB1B4706FF6D113D279A_RuntimeMethod_var), NULL);
		il2cpp_codegen_runtime_class_init_inline(RenderPipelineManager_t44E0175AAADDD5487593AEF2B009B1B154957CDB_il2cpp_TypeInfo_var);
		RenderPipelineManager_remove_beginCameraRendering_m6A9B576247B531A6C1C715870A37343AC702976E(L_0, NULL);
		Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3* L_1 = (Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3*)il2cpp_codegen_object_new(Action_2_t8E07914D7090FF200FE84404EEEFAF3CE183C9F3_il2cpp_TypeInfo_var);
		Action_2__ctor_mBEB5B9B513FE305CE98CA8065CC6E6CC0E5A4D51(L_1, __this, (intptr_t)((void*)PixelPerfectCamera_OnEndCameraRendering_m5D4D2899F818CFB9566FA00DB9734B19FE4F5172_RuntimeMethod_var), NULL);
		RenderPipelineManager_remove_endCameraRendering_m0EC9DE4937A7B58074E35E75CCDE819D4A1E302A(L_1, NULL);
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_2 = __this->___m_Camera;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_3;
		memset((&L_3), 0, sizeof(L_3));
		Rect__ctor_m18C3033D135097BEE424AAA68D91C706D2647F23_inline((&L_3), (0.0f), (0.0f), (1.0f), (1.0f), NULL);
		NullCheck(L_2);
		Camera_set_rect_mA81158BC169AF8674DE240AE9460FC5A0EADBB19(L_2, L_3, NULL);
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_4 = __this->___m_Camera;
		NullCheck(L_4);
		Camera_ResetWorldToCameraMatrix_m25E544C8F31680DC08C58F7416AFD77DA3DB3F91(L_4, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnBeforeSerialize_mED54D4AD64682B1314D338134ECF4790E0062546 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera_OnAfterDeserialize_m4C38B4CC1F67277C234831D0F0F6608DBD8F818B (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCamera__ctor_mCF6FB2072357E5CB9F7AA1EB4E09E5BA84BC54C7 (PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* __this, const RuntimeMethod* method) 
{
	{
		__this->___m_AssetsPPU = ((int32_t)100);
		__this->___m_RefResolutionX = ((int32_t)320);
		__this->___m_RefResolutionY = ((int32_t)180);
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal__ctor_m6BC5985512637F4B2AAD903E5D411B954CB8E795 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, RuntimeObject* ___0_component, const RuntimeMethod* method) 
{
	{
		__this->___zoom = 1;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_0;
		L_0 = Rect_get_zero_m5341D8B63DEF1F4C308A685EEC8CFEA12A396C8D(NULL);
		__this->___pixelRect = L_0;
		__this->___orthoSize = (1.0f);
		__this->___cinemachineVCamZoom = 1;
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		RuntimeObject* L_1 = ___0_component;
		__this->___m_Component = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___m_Component), (void*)L_1);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal_OnBeforeSerialize_m4AE0DDB47BB97880367D494025A2CD9B0EDCCB45 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		RuntimeObject* L_0 = __this->___m_Component;
		__this->___m_SerializableComponent = ((PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195*)IsInstClass((RuntimeObject*)L_0, PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195_il2cpp_TypeInfo_var));
		Il2CppCodeGenWriteBarrier((void**)(&__this->___m_SerializableComponent), (void*)((PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195*)IsInstClass((RuntimeObject*)L_0, PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195_il2cpp_TypeInfo_var)));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal_OnAfterDeserialize_mEA652F30B205113EED7848CE7948EB8F7709ED3B (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* L_0 = __this->___m_SerializableComponent;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		PixelPerfectCamera_t158E7699626BAB0C2E3CFD4E2592DF1AFB8FB195* L_2 = __this->___m_SerializableComponent;
		__this->___m_Component = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___m_Component), (void*)L_2);
	}

IL_001a:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelPerfectCameraInternal_CalculateCameraProperties_mB201DE82608102113237D5509D085C7ED74BB9FE (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, int32_t ___0_screenWidth, int32_t ___1_screenHeight, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	bool V_3 = false;
	bool V_4 = false;
	bool V_5 = false;
	bool V_6 = false;
	bool V_7 = false;
	int32_t V_8 = 0;
	int32_t V_9 = 0;
	float V_10 = 0.0f;
	float V_11 = 0.0f;
	float G_B24_0 = 0.0f;
	float G_B31_0 = 0.0f;
	{
		RuntimeObject* L_0 = __this->___m_Component;
		NullCheck(L_0);
		int32_t L_1;
		L_1 = InterfaceFuncInvoker0< int32_t >::Invoke(0, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_0);
		V_0 = L_1;
		RuntimeObject* L_2 = __this->___m_Component;
		NullCheck(L_2);
		int32_t L_3;
		L_3 = InterfaceFuncInvoker0< int32_t >::Invoke(1, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_2);
		V_1 = L_3;
		RuntimeObject* L_4 = __this->___m_Component;
		NullCheck(L_4);
		int32_t L_5;
		L_5 = InterfaceFuncInvoker0< int32_t >::Invoke(2, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_4);
		V_2 = L_5;
		RuntimeObject* L_6 = __this->___m_Component;
		NullCheck(L_6);
		bool L_7;
		L_7 = InterfaceFuncInvoker0< bool >::Invoke(3, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_6);
		V_3 = L_7;
		RuntimeObject* L_8 = __this->___m_Component;
		NullCheck(L_8);
		bool L_9;
		L_9 = InterfaceFuncInvoker0< bool >::Invoke(4, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_8);
		V_4 = L_9;
		RuntimeObject* L_10 = __this->___m_Component;
		NullCheck(L_10);
		bool L_11;
		L_11 = InterfaceFuncInvoker0< bool >::Invoke(5, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_10);
		V_5 = L_11;
		RuntimeObject* L_12 = __this->___m_Component;
		NullCheck(L_12);
		bool L_13;
		L_13 = InterfaceFuncInvoker0< bool >::Invoke(6, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_12);
		V_6 = L_13;
		RuntimeObject* L_14 = __this->___m_Component;
		NullCheck(L_14);
		bool L_15;
		L_15 = InterfaceFuncInvoker0< bool >::Invoke(7, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_14);
		V_7 = L_15;
		bool L_16 = V_6;
		bool L_17 = V_5;
		__this->___cropFrameXAndY = (bool)((int32_t)((int32_t)L_16&(int32_t)L_17));
		bool L_18 = V_6;
		bool L_19 = V_5;
		__this->___cropFrameXOrY = (bool)((int32_t)((int32_t)L_18|(int32_t)L_19));
		bool L_20 = __this->___cropFrameXAndY;
		bool L_21 = V_7;
		__this->___useStretchFill = (bool)((int32_t)((int32_t)L_20&(int32_t)L_21));
		bool L_22 = __this->___useStretchFill;
		__this->___requiresUpscaling = L_22;
		int32_t L_23 = ___1_screenHeight;
		int32_t L_24 = V_2;
		V_8 = ((int32_t)(L_23/L_24));
		int32_t L_25 = ___0_screenWidth;
		int32_t L_26 = V_1;
		V_9 = ((int32_t)(L_25/L_26));
		int32_t L_27 = V_8;
		int32_t L_28 = V_9;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_29;
		L_29 = Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52(L_27, L_28, NULL);
		int32_t L_30;
		L_30 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(1, L_29, NULL);
		__this->___zoom = L_30;
		__this->___useOffscreenRT = (bool)0;
		__this->___offscreenRTWidth = 0;
		__this->___offscreenRTHeight = 0;
		bool L_31 = __this->___cropFrameXOrY;
		if (!L_31)
		{
			goto IL_0191;
		}
	}
	{
		__this->___useOffscreenRT = (bool)1;
		bool L_32 = V_3;
		if (L_32)
		{
			goto IL_013f;
		}
	}
	{
		bool L_33 = __this->___cropFrameXAndY;
		if (!L_33)
		{
			goto IL_0107;
		}
	}
	{
		int32_t L_34 = __this->___zoom;
		int32_t L_35 = V_1;
		__this->___offscreenRTWidth = ((int32_t)il2cpp_codegen_multiply(L_34, L_35));
		int32_t L_36 = __this->___zoom;
		int32_t L_37 = V_2;
		__this->___offscreenRTHeight = ((int32_t)il2cpp_codegen_multiply(L_36, L_37));
		goto IL_01c8;
	}

IL_0107:
	{
		bool L_38 = V_6;
		if (!L_38)
		{
			goto IL_0125;
		}
	}
	{
		int32_t L_39 = ___0_screenWidth;
		__this->___offscreenRTWidth = L_39;
		int32_t L_40 = __this->___zoom;
		int32_t L_41 = V_2;
		__this->___offscreenRTHeight = ((int32_t)il2cpp_codegen_multiply(L_40, L_41));
		goto IL_01c8;
	}

IL_0125:
	{
		int32_t L_42 = __this->___zoom;
		int32_t L_43 = V_1;
		__this->___offscreenRTWidth = ((int32_t)il2cpp_codegen_multiply(L_42, L_43));
		int32_t L_44 = ___1_screenHeight;
		__this->___offscreenRTHeight = L_44;
		goto IL_01c8;
	}

IL_013f:
	{
		bool L_45 = __this->___cropFrameXAndY;
		if (!L_45)
		{
			goto IL_0157;
		}
	}
	{
		int32_t L_46 = V_1;
		__this->___offscreenRTWidth = L_46;
		int32_t L_47 = V_2;
		__this->___offscreenRTHeight = L_47;
		goto IL_01c8;
	}

IL_0157:
	{
		bool L_48 = V_6;
		if (!L_48)
		{
			goto IL_0176;
		}
	}
	{
		int32_t L_49 = ___0_screenWidth;
		int32_t L_50 = __this->___zoom;
		__this->___offscreenRTWidth = ((int32_t)il2cpp_codegen_multiply(((int32_t)(((int32_t)(L_49/L_50))/2)), 2));
		int32_t L_51 = V_2;
		__this->___offscreenRTHeight = L_51;
		goto IL_01c8;
	}

IL_0176:
	{
		int32_t L_52 = V_1;
		__this->___offscreenRTWidth = L_52;
		int32_t L_53 = ___1_screenHeight;
		int32_t L_54 = __this->___zoom;
		__this->___offscreenRTHeight = ((int32_t)il2cpp_codegen_multiply(((int32_t)(((int32_t)(L_53/L_54))/2)), 2));
		goto IL_01c8;
	}

IL_0191:
	{
		bool L_55 = V_3;
		if (!L_55)
		{
			goto IL_01c8;
		}
	}
	{
		int32_t L_56 = __this->___zoom;
		if ((((int32_t)L_56) <= ((int32_t)1)))
		{
			goto IL_01c8;
		}
	}
	{
		__this->___useOffscreenRT = (bool)1;
		int32_t L_57 = ___0_screenWidth;
		int32_t L_58 = __this->___zoom;
		__this->___offscreenRTWidth = ((int32_t)il2cpp_codegen_multiply(((int32_t)(((int32_t)(L_57/L_58))/2)), 2));
		int32_t L_59 = ___1_screenHeight;
		int32_t L_60 = __this->___zoom;
		__this->___offscreenRTHeight = ((int32_t)il2cpp_codegen_multiply(((int32_t)(((int32_t)(L_59/L_60))/2)), 2));
	}

IL_01c8:
	{
		bool L_61 = __this->___useOffscreenRT;
		if (!L_61)
		{
			goto IL_01f5;
		}
	}
	{
		int32_t L_62 = __this->___offscreenRTWidth;
		int32_t L_63 = __this->___offscreenRTHeight;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_64;
		memset((&L_64), 0, sizeof(L_64));
		Rect__ctor_m18C3033D135097BEE424AAA68D91C706D2647F23_inline((&L_64), (0.0f), (0.0f), ((float)L_62), ((float)L_63), NULL);
		__this->___pixelRect = L_64;
		goto IL_0200;
	}

IL_01f5:
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_65;
		L_65 = Rect_get_zero_m5341D8B63DEF1F4C308A685EEC8CFEA12A396C8D(NULL);
		__this->___pixelRect = L_65;
	}

IL_0200:
	{
		bool L_66 = V_6;
		if (!L_66)
		{
			goto IL_021a;
		}
	}
	{
		int32_t L_67 = V_2;
		int32_t L_68 = V_0;
		__this->___orthoSize = ((float)(((float)il2cpp_codegen_multiply(((float)L_67), (0.5f)))/((float)L_68)));
		goto IL_02c5;
	}

IL_021a:
	{
		bool L_69 = V_5;
		if (!L_69)
		{
			goto IL_0266;
		}
	}
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_70 = __this->___pixelRect;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_71;
		L_71 = Rect_get_zero_m5341D8B63DEF1F4C308A685EEC8CFEA12A396C8D(NULL);
		bool L_72;
		L_72 = Rect_op_Equality_mF2A038255CAF5F1E86079B9EE0FC96DE54307C1F_inline(L_70, L_71, NULL);
		if (L_72)
		{
			goto IL_0249;
		}
	}
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* L_73 = (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D*)(&__this->___pixelRect);
		float L_74;
		L_74 = Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline(L_73, NULL);
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* L_75 = (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D*)(&__this->___pixelRect);
		float L_76;
		L_76 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline(L_75, NULL);
		G_B24_0 = ((float)(L_74/L_76));
		goto IL_024e;
	}

IL_0249:
	{
		int32_t L_77 = ___0_screenWidth;
		int32_t L_78 = ___1_screenHeight;
		G_B24_0 = ((float)(((float)L_77)/((float)L_78)));
	}

IL_024e:
	{
		V_10 = G_B24_0;
		int32_t L_79 = V_1;
		float L_80 = V_10;
		int32_t L_81 = V_0;
		__this->___orthoSize = ((float)(((float)il2cpp_codegen_multiply(((float)(((float)L_79)/L_80)), (0.5f)))/((float)L_81)));
		goto IL_02c5;
	}

IL_0266:
	{
		bool L_82 = V_3;
		if (!L_82)
		{
			goto IL_028a;
		}
	}
	{
		int32_t L_83 = __this->___zoom;
		if ((((int32_t)L_83) <= ((int32_t)1)))
		{
			goto IL_028a;
		}
	}
	{
		int32_t L_84 = __this->___offscreenRTHeight;
		int32_t L_85 = V_0;
		__this->___orthoSize = ((float)(((float)il2cpp_codegen_multiply(((float)L_84), (0.5f)))/((float)L_85)));
		goto IL_02c5;
	}

IL_028a:
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_86 = __this->___pixelRect;
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_87;
		L_87 = Rect_get_zero_m5341D8B63DEF1F4C308A685EEC8CFEA12A396C8D(NULL);
		bool L_88;
		L_88 = Rect_op_Equality_mF2A038255CAF5F1E86079B9EE0FC96DE54307C1F_inline(L_86, L_87, NULL);
		if (L_88)
		{
			goto IL_02a9;
		}
	}
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* L_89 = (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D*)(&__this->___pixelRect);
		float L_90;
		L_90 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline(L_89, NULL);
		G_B31_0 = L_90;
		goto IL_02ab;
	}

IL_02a9:
	{
		int32_t L_91 = ___1_screenHeight;
		G_B31_0 = ((float)L_91);
	}

IL_02ab:
	{
		V_11 = G_B31_0;
		float L_92 = V_11;
		int32_t L_93 = __this->___zoom;
		int32_t L_94 = V_0;
		__this->___orthoSize = ((float)(((float)il2cpp_codegen_multiply(L_92, (0.5f)))/((float)((int32_t)il2cpp_codegen_multiply(L_93, L_94)))));
	}

IL_02c5:
	{
		bool L_95 = V_3;
		if (L_95)
		{
			goto IL_02d1;
		}
	}
	{
		bool L_96 = V_3;
		bool L_97 = V_4;
		if (!((int32_t)(((((int32_t)L_96) == ((int32_t)0))? 1 : 0)&(int32_t)L_97)))
		{
			goto IL_02e0;
		}
	}

IL_02d1:
	{
		int32_t L_98 = V_0;
		__this->___unitsPerPixel = ((float)((1.0f)/((float)L_98)));
		return;
	}

IL_02e0:
	{
		int32_t L_99 = __this->___zoom;
		int32_t L_100 = V_0;
		__this->___unitsPerPixel = ((float)((1.0f)/((float)((int32_t)il2cpp_codegen_multiply(L_99, L_100)))));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D PixelPerfectCameraInternal_CalculateFinalBlitPixelRect_mDBD399AEAA750ACC3B03C21110E361758DBC0C82 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, int32_t ___0_screenWidth, int32_t ___1_screenHeight, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D V_0;
	memset((&V_0), 0, sizeof(V_0));
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	{
		il2cpp_codegen_initobj((&V_0), sizeof(Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D));
		bool L_0 = __this->___useStretchFill;
		if (!L_0)
		{
			goto IL_00dd;
		}
	}
	{
		int32_t L_1 = ___0_screenWidth;
		int32_t L_2 = ___1_screenHeight;
		V_1 = ((float)(((float)L_1)/((float)L_2)));
		RuntimeObject* L_3 = __this->___m_Component;
		NullCheck(L_3);
		int32_t L_4;
		L_4 = InterfaceFuncInvoker0< int32_t >::Invoke(1, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_3);
		RuntimeObject* L_5 = __this->___m_Component;
		NullCheck(L_5);
		int32_t L_6;
		L_6 = InterfaceFuncInvoker0< int32_t >::Invoke(2, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_5);
		V_2 = ((float)(((float)L_4)/((float)L_6)));
		float L_7 = V_1;
		float L_8 = V_2;
		if ((!(((float)L_7) > ((float)L_8))))
		{
			goto IL_006d;
		}
	}
	{
		int32_t L_9 = ___1_screenHeight;
		Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline((&V_0), ((float)L_9), NULL);
		int32_t L_10 = ___1_screenHeight;
		float L_11 = V_2;
		Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline((&V_0), ((float)il2cpp_codegen_multiply(((float)L_10), L_11)), NULL);
		int32_t L_12 = ___0_screenWidth;
		float L_13;
		L_13 = Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline((&V_0), NULL);
		Rect_set_x_mAB91AB71898A20762BC66FD0723C4C739C4C3406_inline((&V_0), ((float)((int32_t)(((int32_t)il2cpp_codegen_subtract(L_12, il2cpp_codegen_cast_double_to_int<int32_t>(L_13)))/2))), NULL);
		Rect_set_y_mDE91F4B98A6E8623EFB1250FF6526D5DB5855629_inline((&V_0), (0.0f), NULL);
		goto IL_00a1;
	}

IL_006d:
	{
		int32_t L_14 = ___0_screenWidth;
		Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline((&V_0), ((float)L_14), NULL);
		int32_t L_15 = ___0_screenWidth;
		float L_16 = V_2;
		Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline((&V_0), ((float)(((float)L_15)/L_16)), NULL);
		int32_t L_17 = ___1_screenHeight;
		float L_18;
		L_18 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline((&V_0), NULL);
		Rect_set_y_mDE91F4B98A6E8623EFB1250FF6526D5DB5855629_inline((&V_0), ((float)((int32_t)(((int32_t)il2cpp_codegen_subtract(L_17, il2cpp_codegen_cast_double_to_int<int32_t>(L_18)))/2))), NULL);
		Rect_set_x_mAB91AB71898A20762BC66FD0723C4C739C4C3406_inline((&V_0), (0.0f), NULL);
	}

IL_00a1:
	{
		int32_t L_19 = ___0_screenWidth;
		RuntimeObject* L_20 = __this->___m_Component;
		NullCheck(L_20);
		int32_t L_21;
		L_21 = InterfaceFuncInvoker0< int32_t >::Invoke(1, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_20);
		if (((int32_t)(L_19%L_21)))
		{
			goto IL_00bf;
		}
	}
	{
		float L_22 = V_2;
		float L_23 = V_1;
		__this->___requiresUpscaling = (bool)((((float)L_22) < ((float)L_23))? 1 : 0);
		goto IL_015a;
	}

IL_00bf:
	{
		int32_t L_24 = ___1_screenHeight;
		RuntimeObject* L_25 = __this->___m_Component;
		NullCheck(L_25);
		int32_t L_26;
		L_26 = InterfaceFuncInvoker0< int32_t >::Invoke(2, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_25);
		if (((int32_t)(L_24%L_26)))
		{
			goto IL_015a;
		}
	}
	{
		float L_27 = V_2;
		float L_28 = V_1;
		__this->___requiresUpscaling = (bool)((((float)L_27) > ((float)L_28))? 1 : 0);
		goto IL_015a;
	}

IL_00dd:
	{
		RuntimeObject* L_29 = __this->___m_Component;
		NullCheck(L_29);
		bool L_30;
		L_30 = InterfaceFuncInvoker0< bool >::Invoke(3, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_29);
		if (!L_30)
		{
			goto IL_0116;
		}
	}
	{
		int32_t L_31 = __this->___zoom;
		int32_t L_32 = __this->___offscreenRTHeight;
		Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline((&V_0), ((float)((int32_t)il2cpp_codegen_multiply(L_31, L_32))), NULL);
		int32_t L_33 = __this->___zoom;
		int32_t L_34 = __this->___offscreenRTWidth;
		Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline((&V_0), ((float)((int32_t)il2cpp_codegen_multiply(L_33, L_34))), NULL);
		goto IL_0132;
	}

IL_0116:
	{
		int32_t L_35 = __this->___offscreenRTHeight;
		Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline((&V_0), ((float)L_35), NULL);
		int32_t L_36 = __this->___offscreenRTWidth;
		Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline((&V_0), ((float)L_36), NULL);
	}

IL_0132:
	{
		int32_t L_37 = ___0_screenWidth;
		float L_38;
		L_38 = Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline((&V_0), NULL);
		Rect_set_x_mAB91AB71898A20762BC66FD0723C4C739C4C3406_inline((&V_0), ((float)((int32_t)(((int32_t)il2cpp_codegen_subtract(L_37, il2cpp_codegen_cast_double_to_int<int32_t>(L_38)))/2))), NULL);
		int32_t L_39 = ___1_screenHeight;
		float L_40;
		L_40 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline((&V_0), NULL);
		Rect_set_y_mDE91F4B98A6E8623EFB1250FF6526D5DB5855629_inline((&V_0), ((float)((int32_t)(((int32_t)il2cpp_codegen_subtract(L_39, il2cpp_codegen_cast_double_to_int<int32_t>(L_40)))/2))), NULL);
	}

IL_015a:
	{
		Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D L_41 = V_0;
		return L_41;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelPerfectCameraInternal_CorrectCinemachineOrthoSize_m885222B60F8A525214DE5D945448F245E8C9A4D4 (PixelPerfectCameraInternal_tAEDD9ED6B941D1E486D99AE7387C8C0BD984C3CC* __this, float ___0_targetOrthoSize, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	{
		RuntimeObject* L_0 = __this->___m_Component;
		NullCheck(L_0);
		bool L_1;
		L_1 = InterfaceFuncInvoker0< bool >::Invoke(3, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_0);
		if (!L_1)
		{
			goto IL_0037;
		}
	}
	{
		float L_2 = __this->___orthoSize;
		float L_3 = ___0_targetOrthoSize;
		int32_t L_4;
		L_4 = Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline(((float)(L_2/L_3)), NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_5;
		L_5 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(1, L_4, NULL);
		__this->___cinemachineVCamZoom = L_5;
		float L_6 = __this->___orthoSize;
		int32_t L_7 = __this->___cinemachineVCamZoom;
		V_0 = ((float)(L_6/((float)L_7)));
		goto IL_006f;
	}

IL_0037:
	{
		int32_t L_8 = __this->___zoom;
		float L_9 = __this->___orthoSize;
		float L_10 = ___0_targetOrthoSize;
		int32_t L_11;
		L_11 = Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline(((float)(((float)il2cpp_codegen_multiply(((float)L_8), L_9))/L_10)), NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_12;
		L_12 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(1, L_11, NULL);
		__this->___cinemachineVCamZoom = L_12;
		int32_t L_13 = __this->___zoom;
		float L_14 = __this->___orthoSize;
		int32_t L_15 = __this->___cinemachineVCamZoom;
		V_0 = ((float)(((float)il2cpp_codegen_multiply(((float)L_13), L_14))/((float)L_15)));
	}

IL_006f:
	{
		RuntimeObject* L_16 = __this->___m_Component;
		NullCheck(L_16);
		bool L_17;
		L_17 = InterfaceFuncInvoker0< bool >::Invoke(3, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_16);
		if (L_17)
		{
			goto IL_00a8;
		}
	}
	{
		RuntimeObject* L_18 = __this->___m_Component;
		NullCheck(L_18);
		bool L_19;
		L_19 = InterfaceFuncInvoker0< bool >::Invoke(4, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_18);
		if (L_19)
		{
			goto IL_00a8;
		}
	}
	{
		int32_t L_20 = __this->___cinemachineVCamZoom;
		RuntimeObject* L_21 = __this->___m_Component;
		NullCheck(L_21);
		int32_t L_22;
		L_22 = InterfaceFuncInvoker0< int32_t >::Invoke(0, IPixelPerfectCamera_tBE8802CBCF00955B1E84AF5B4924A0EA72C7D1E4_il2cpp_TypeInfo_var, L_21);
		__this->___unitsPerPixel = ((float)((1.0f)/((float)((int32_t)il2cpp_codegen_multiply(L_20, L_22)))));
	}

IL_00a8:
	{
		float L_23 = V_0;
		return L_23;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U24BurstDirectCallInitializer_Initialize_m15347CE4997B95D8D58B24CEC10BDB0ACB562281 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9* V_0 = NULL;
	{
		il2cpp_codegen_runtime_class_init_inline(BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_il2cpp_TypeInfo_var);
		BurstCompilerOptions_t5F93118F305E1B0C950C6F9AF8BCA74033DA01C9* L_0 = ((BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_StaticFields*)il2cpp_codegen_static_fields_for(BurstCompiler_t2715484E1FF256726FC4D4D8E17C35A4C8DFA2B8_il2cpp_TypeInfo_var))->___Options;
		V_0 = L_0;
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, int32_t ___0_x, int32_t ___1_y, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_x;
		__this->___m_X = L_0;
		int32_t L_1 = ___1_y;
		__this->___m_Y = L_1;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Subtraction_mE42023FF80067CB44A1D4A27EB7CF2B24CABB828_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___1_b, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___0_a;
		float L_1 = L_0.___x;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___1_b;
		float L_3 = L_2.___x;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___0_a;
		float L_5 = L_4.___y;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___1_b;
		float L_7 = L_6.___y;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8 = ___0_a;
		float L_9 = L_8.___z;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = ___1_b;
		float L_11 = L_10.___z;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		memset((&L_12), 0, sizeof(L_12));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_12), ((float)il2cpp_codegen_subtract(L_1, L_3)), ((float)il2cpp_codegen_subtract(L_5, L_7)), ((float)il2cpp_codegen_subtract(L_9, L_11)), NULL);
		V_0 = L_12;
		goto IL_0030;
	}

IL_0030:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13 = V_0;
		return L_13;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___1_b, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___0_a;
		float L_1 = L_0.___x;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___1_b;
		float L_3 = L_2.___x;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___0_a;
		float L_5 = L_4.___y;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___1_b;
		float L_7 = L_6.___y;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8 = ___0_a;
		float L_9 = L_8.___z;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = ___1_b;
		float L_11 = L_10.___z;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		memset((&L_12), 0, sizeof(L_12));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_12), ((float)il2cpp_codegen_add(L_1, L_3)), ((float)il2cpp_codegen_add(L_5, L_7)), ((float)il2cpp_codegen_add(L_9, L_11)), NULL);
		V_0 = L_12;
		goto IL_0030;
	}

IL_0030:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13 = V_0;
		return L_13;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_0 = ((Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_StaticFields*)il2cpp_codegen_static_fields_for(Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var))->___identityQuaternion;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___oneVector;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___x = L_0;
		float L_1 = ___1_y;
		__this->___y = L_1;
		float L_2 = ___2_z;
		__this->___z = L_2;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->___m_X;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		int32_t L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->___m_Y;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		int32_t L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect__ctor_m18C3033D135097BEE424AAA68D91C706D2647F23_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_x, float ___1_y, float ___2_width, float ___3_height, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___m_XMin = L_0;
		float L_1 = ___1_y;
		__this->___m_YMin = L_1;
		float L_2 = ___2_width;
		__this->___m_Width = L_2;
		float L_3 = ___3_height;
		__this->___m_Height = L_3;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Rect_op_Equality_mF2A038255CAF5F1E86079B9EE0FC96DE54307C1F_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___0_lhs, Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D ___1_rhs, const RuntimeMethod* method) 
{
	bool V_0 = false;
	int32_t G_B5_0 = 0;
	{
		float L_0;
		L_0 = Rect_get_x_mB267B718E0D067F2BAE31BA477647FBF964916EB_inline((&___0_lhs), NULL);
		float L_1;
		L_1 = Rect_get_x_mB267B718E0D067F2BAE31BA477647FBF964916EB_inline((&___1_rhs), NULL);
		if ((!(((float)L_0) == ((float)L_1))))
		{
			goto IL_0043;
		}
	}
	{
		float L_2;
		L_2 = Rect_get_y_mC733E8D49F3CE21B2A3D40A1B72D687F22C97F49_inline((&___0_lhs), NULL);
		float L_3;
		L_3 = Rect_get_y_mC733E8D49F3CE21B2A3D40A1B72D687F22C97F49_inline((&___1_rhs), NULL);
		if ((!(((float)L_2) == ((float)L_3))))
		{
			goto IL_0043;
		}
	}
	{
		float L_4;
		L_4 = Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline((&___0_lhs), NULL);
		float L_5;
		L_5 = Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline((&___1_rhs), NULL);
		if ((!(((float)L_4) == ((float)L_5))))
		{
			goto IL_0043;
		}
	}
	{
		float L_6;
		L_6 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline((&___0_lhs), NULL);
		float L_7;
		L_7 = Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline((&___1_rhs), NULL);
		G_B5_0 = ((((float)L_6) == ((float)L_7))? 1 : 0);
		goto IL_0044;
	}

IL_0043:
	{
		G_B5_0 = 0;
	}

IL_0044:
	{
		V_0 = (bool)G_B5_0;
		goto IL_0047;
	}

IL_0047:
	{
		bool L_8 = V_0;
		return L_8;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_width_m620D67551372073C9C32C4C4624C2A5713F7F9A9_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0 = __this->___m_Width;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		float L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_height_mE1AA6C6C725CCD2D317BD2157396D3CF7D47C9D8_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0 = __this->___m_Height;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		float L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_height_mD00038E6E06637137A5626CA8CD421924005BF03_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_value;
		__this->___m_Height = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_width_m93B6217CF3EFF89F9B0C81F34D7345DE90B93E5A_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_value;
		__this->___m_Width = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_x_mAB91AB71898A20762BC66FD0723C4C739C4C3406_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_value;
		__this->___m_XMin = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Rect_set_y_mDE91F4B98A6E8623EFB1250FF6526D5DB5855629_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, float ___0_value, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_value;
		__this->___m_YMin = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline (float ___0_f, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		float L_0 = ___0_f;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_1;
		L_1 = bankers_round(((double)L_0));
		V_0 = il2cpp_codegen_cast_double_to_int<int32_t>(L_1);
		goto IL_000c;
	}

IL_000c:
	{
		int32_t L_2 = V_0;
		return L_2;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_x_mB267B718E0D067F2BAE31BA477647FBF964916EB_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0 = __this->___m_XMin;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		float L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Rect_get_y_mC733E8D49F3CE21B2A3D40A1B72D687F22C97F49_inline (Rect_tA04E0F8A1830E767F40FB27ECD8D309303571F0D* __this, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0 = __this->___m_YMin;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		float L_1 = V_0;
		return L_1;
	}
}
