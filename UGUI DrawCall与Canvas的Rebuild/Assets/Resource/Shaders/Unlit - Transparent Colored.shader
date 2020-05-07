// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Transparent Colored"
{
	Properties
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
	}
	
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Offset -1, -1
			Fog { Mode Off }
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __  EFFECT_GRAY_ON
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;

			struct appdata_t
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 worldPos : TEXCOORD1;
				//float  graySlerp : TEXCOORD2;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color * _Color;
				o.texcoord = v.texcoord;
				o.worldPos = TRANSFORM_TEX(v.vertex.xy, _MainTex);
				//o.graySlerp = step(v.color.r + v.color.g + v.color.b,0.001);
				//float fa = v.color.a;				
				//half4 c = lerp(v.color,1,o.graySlerp);
				//c.a = fa;
				//o.color = c * _Color;

				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				// Sample the texture
				half4 col = tex2D(_MainTex, IN.texcoord);
				//float fvalue = Luminance(col.rgb);// col.r * 0.298999995 + col.g * 0.578000009 + col.b * 0.143999994;
				//float4 gray = float4(fvalue, fvalue, fvalue, col.a);
				//col = lerp(col, gray, IN.graySlerp);
				
				#if EFFECT_GRAY_ON				
				float fvalue = col.r * 0.298999995 + col.g * 0.578000009 + col.b * 0.143999994;
				col = float4(fvalue, fvalue, fvalue, col.a);				
				#endif
				
				return col * IN.color;
			}
			ENDCG
		}
	}	
}