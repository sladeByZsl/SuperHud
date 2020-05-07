// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/HUDSprite"
{
	Properties
	{
		_MainTex ("Alpha (A)", 2D) = "white" {}
		_MainAlpha("MainAlpha (A)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
		_ReverseY("ReverseY", Float) = 1.0
	}
	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
		Fog { Mode Off }
		ColorMask RGB
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{	
			CGPROGRAM
			#pragma multi_compile MAIN_ALPHA_OFF  MAIN_ALPHA_ON
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				//float4 ScreenPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			sampler2D _MainAlpha;
			uniform float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			//sampler2D_float _CameraDepthTexture;
			float4 _Color;
			float  _ReverseY;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				//float4  oPos = UnityObjectToClipPos(v.vertex);
				//o.vertex = v.vertex;
				//o.vertex.x = (v.uv2.x / _ScreenParams.x - 0.5) * 2.0;
				//o.vertex.y = (v.uv2.y / _ScreenParams.y - 0.5) * 2.0;
				//o.vertex.z = 1;
				//o.vertex.w = 1;

				//float   fScale = 0.01;
				float fScale = min(12.80 / _ScreenParams.x, 7.2 / _ScreenParams.y);
				float2  uvOffset = v.uv2;// * fScale;
				uvOffset.x *= fScale;
				uvOffset.y *= fScale;

				float3  right = UNITY_MATRIX_IT_MV[0].xyz;
				float3  up = UNITY_MATRIX_IT_MV[1].xyz;
				float3  vPos = v.vertex.xyz + uvOffset.x * right + uvOffset.y * up;
				float4  vFinal = float4(vPos.xyz, 1.0);
				o.vertex = UnityObjectToClipPos(vFinal);

				//o.ScreenPos.x = v.uv2.x / _ScreenParams.x;
				//o.ScreenPos.y = v.uv2.y / _ScreenParams.y;
				//o.ScreenPos.z = oPos.z;
				//o.ScreenPos.w = 1.0;
				//COMPUTE_EYEDEPTH(o.ScreenPos.z);

				o.color = v.color * _Color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

//#if UNITY_UV_STARTS_AT_TOP
//				if (_MainTex_TexelSize.y > 0)
//					o.vertex.y = -o.vertex.y;
//#endif
//				o.vertex.y *= _ReverseY;
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				//float depth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.ScreenPos));
				//depth = LinearEyeDepth(depth);
				//bool culled = (i.ScreenPos.z - 1e-2f) > depth;
				//if (culled)
				//	discard;
				fixed4 col = tex2D(_MainTex, i.texcoord);
#if defined(MAIN_ALPHA_ON)
				fixed4 alpha = tex2D(_MainAlpha, i.texcoord);
				col.a = alpha.g;
#endif
				return col * i.color;
			}
			#pragma vertex vert
			#pragma fragment frag
			ENDCG 
		}
	}	
}
