Shader "InteriorMapGeneratorShader/atlasshader"
{
	Properties
	{
		_BAinfo("BADepth", vector) = (0,0,0)
		_MainTex("Texture", 2D) = "white" {}
		_PreprojectedTex("Texture", 2D) = "white" {}
		_UVInfo("UVInfo",vector) = (0,0,0,0)
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
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

            v2f vert (appdata v)
            {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _PreprojectedTex;
			float4 _UVInfo;
			float3 _BAinfo;
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 ImpostorColor= tex2D(_PreprojectedTex, i.uv);
				float2 start = float2(_UVInfo.x, _UVInfo.y);
				float width = _UVInfo.z;
				float height = _UVInfo.w;

				if (start.x + width > i.uv.x &&  i.uv.x > start.x  && start.y + height > i.uv.y && i.uv.y > start.y)
				{
					float u = (i.uv.x - start.x) / width; //z --_boundinginfo.x
					float v = (i.uv.y - start.y) / height; //y -- _boundinginfo.y
					if(_BAinfo.x > _BAinfo.y)
					{
					    v *= _BAinfo.y / _BAinfo.x;
						v += (1 - _BAinfo.y / _BAinfo.x) * 1/2;
					}
					else
					{
						u *= _BAinfo.x / _BAinfo.y;
						u += (1 -_BAinfo.x / _BAinfo.y) * 1/2;
					}
					
					ImpostorColor.rgb = tex2D(_MainTex, float2(u,v));
					ImpostorColor.a = _BAinfo.z;
				}
			    
				return ImpostorColor;
			}
			ENDCG
		}
    }
}
