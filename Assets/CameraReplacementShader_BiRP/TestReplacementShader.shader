Shader "Test Replacement Shader"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
	}

	CGINCLUDE

		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;

		float4 _Color;
		
		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			return o;
		}

	ENDCG

	SubShader // Opaque = Red
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque" }
		Cull Off Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			fixed4 frag (v2f i) : SV_Target
			{
				return float4(1,0,0,0.1);
			}
			
			ENDCG
		}
	}
	SubShader // Transparent = Green
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Off Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag (v2f i) : SV_Target
			{
				return float4(0,1,0,0.1);
			}
			
			ENDCG
		}
	}
	SubShader // Background = Cyan
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Background" }
		Cull Off Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag (v2f i) : SV_Target
			{
				return float4(0,1,1,0.1);
			}
			
			ENDCG
		}
	}
	SubShader // Overlay = Yellow
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Overlay" }
		Cull Off Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag (v2f i) : SV_Target
			{
				return float4(1,1,0,0.1);
			}
			
			ENDCG
		}
	}
}
