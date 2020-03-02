//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============
 // Computes lighting and shadows and apply them to the real
Shader "ZED/ZED Filter"
{
	Properties{
		[MaterialToggle] directionalLightEffect("Directional light affects image", Int) = 0
        _Range("Filtered Distance", Float) = 1.2
	}
    SubShader{
        ZWrite On
        Pass{
            Name "FORWARD"
            Tags{ "LightMode" = "Always" }
            Cull Off
            CGPROGRAM
            // compile directives
            #pragma target 4.0
            #pragma vertex vert_surf
            #pragma fragment frag_surf
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile __ NO_DEPTH_OCC

            #include "HLSLSupport.cginc"
            #include "UnityShaderVariables.cginc"
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "ZED_Utils.cginc"
            #include "ZED_Lighting.cginc"

            #define UNITY_PASS_FORWARDBASE
            #define ZED_SPOT_LIGHT_DECLARATION
            #define ZED_POINT_LIGHT_DECLARATION

            struct Input {
                float2 uv_MainTex;
            };

            struct v2f_surf {
                float4 pos : SV_POSITION;
                float4 pack0 : TEXCOORD0;
                float3 worldPos : TEXCOORD3;
                SHADOW_COORDS(4)
                ZED_WORLD_DIR(1)
            };
            
            sampler2D _MainTex;
            sampler2D _DirectionalShadowMap;
            sampler2D _DepthXYZTex;
            sampler2D _MaskTex;
            float4x4 _CameraMatrix;
            float4 _MainTex_ST;
            float4 _DepthXYZTex_ST;
            int _HasShadows;
            float4 ZED_directionalLight[2];
            int directionalLightEffect;
            float _ZEDFactorAffectReal;
            //my attribute
            float _Range;

            // vertex shader
            v2f_surf vert_surf(appdata_full v) {
                v2f_surf o;
                UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
                o.pos = UnityObjectToClipPos(v.vertex);
                ZED_TRANSFER_WORLD_DIR(o)
                o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.pack0.zw = TRANSFORM_TEX(v.texcoord, _DepthXYZTex);
                o.pack0.y = 1 - o.pack0.y;
                o.pack0.w = 1 - o.pack0.w;
                TRANSFER_SHADOW(o);
                o.worldPos = o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                return o;
            }
            // fragment shader
            void frag_surf(v2f_surf IN, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth) {
                UNITY_INITIALIZE_OUTPUT(fixed4,outColor);
                float4 uv = IN.pack0;
                float3 zed_xyz = tex2D(_DepthXYZTex, uv.zw).xxx;

                #ifdef NO_DEPTH_OCC
                    outDepth = 0;
                #else
                    outDepth = computeDepthXYZ(zed_xyz.z);
                #endif
                
                fixed4 c = 0;
                float4 color = tex2D(_MainTex, uv.xy).bgra;
                float3 normals = tex2D(_NormalsTex, uv.zw).rgb;
                c = half4(color);
                c.a = 0;
                outColor.rgb = c;

                if(zed_xyz.z > _Range)
                    discard;
            }

	        ENDCG
	    }
	}
	Fallback Off
}
