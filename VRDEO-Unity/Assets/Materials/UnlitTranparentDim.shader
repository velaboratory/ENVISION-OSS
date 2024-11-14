// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/UnlitTranparentDim"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_DimMult("Dimming Multiplier", Range(0.0, 1.5)) = 0.8
    }

	SubShader{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD0;
					float3 normal : NORMAL;
					float4 worldPos : TEXCOORD2;
					float3 viewDir : TEXCOORD3;
					UNITY_FOG_COORDS(1)
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _DimMult;

				v2f vert(appdata_t v, float3 normal : NORMAL)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					UNITY_TRANSFER_FOG(o,o.vertex);
					//v.normal = -v.normal;
					o.normal = UnityObjectToWorldNormal(normal);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.texcoord);
					UNITY_APPLY_FOG(i.fogCoord, col);

					//get difference between normal and view
					float camDist = distance(i.normal, i.viewDir);

					//if the square isnt transparent, then its dim should depend on this
					if (col.a> 0.9) col.a = 1-camDist* _DimMult;
					return col;
				}
			ENDCG
		}

	}

}
