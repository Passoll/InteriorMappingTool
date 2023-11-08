Shader "InteriorMapGeneratorShader/Cubemapshader"
{
    Properties
    {
        _RoomCube("Room Cube Map", Cube) = "white" {}
        [Toggle(_USEOBJECTSPACE)] _UseObjectSpace("Use Object Space", Float) = 0.0
        _RoomDepth("Room Depth",range(0.001,0.999)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _USEOBJECTSPACE

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            #ifdef _USEOBJECTSPACE
                float3 uvw : TEXCOORD0;
            #else
                float2 uv : TEXCOORD0;
            #endif
                float3 viewDir : TEXCOORD1;
            };

            samplerCUBE _RoomCube;
            float4 _RoomCube_ST;
            float _RoomDepth;
            // psuedo random 伪随机
            float3 rand3(float co) {
                return frac(sin(co * float3(12.9898,78.233,43.2316)) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
            #ifdef _USEOBJECTSPACE
                // slight scaling adjustment to work around "noisy wall" when frac() returns a 0 on surface
                o.uvw = v.vertex * _RoomCube_ST.xyx * 0.999 + _RoomCube_ST.zwz;

                // get object space camera vector
                float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                o.viewDir = v.vertex.xyz - objCam.xyz;

                // adjust for tiling
                o.viewDir *= _RoomCube_ST.xyx;
            #else
                // uvs
                o.uv = TRANSFORM_TEX(v.uv, _RoomCube);

                // get tangent space camera vector
                float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                float3 viewDir = v.vertex.xyz - objCam.xyz;
                float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 bitangent = cross(v.normal.xyz, v.tangent.xyz) * tangentSign;
                o.viewDir = float3(
                    dot(viewDir, v.tangent.xyz),
                    dot(viewDir, bitangent),
                    dot(viewDir, v.normal)
                    );

                // adjust for tiling
                o.viewDir *= _RoomCube_ST.xyx;
            #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                // Specify depth manually
                fixed farFrac = _RoomDepth;

                //remap [0,1] to [+inf,0]
                //->if input _RoomDepth = 0    -> depthScale = 0      (inf depth room)
                //->if input _RoomDepth = 0.5  -> depthScale = 1
                //->if input _RoomDepth = 1    -> depthScale = +inf   (0 volume room)
                float depthScale = 1.0 / (1.0 - farFrac) - 1.0;
                i.viewDir.z *= depthScale;
            #ifdef _USEOBJECTSPACE
                // room uvws
                float3 roomUVW = frac(i.uvw);

                // raytrace box from object view dir
                // transform object space uvw( min max corner = (0,0,0) & (+1,+1,+1))  
                // to normalized box space(min max corner = (-1,-1,-1) & (+1,+1,+1))
                float3 pos = roomUVW * 2.0 - 1.0;
                float3 id = 1.0 / i.viewDir;

                // k means normalized box space depth hit per x/y/z plane seperated
                // (we dont care about near hit result here, we only want far hit result)
                float3 k = abs(id) - pos * id;
                // kmin = normalized box space real hit ray length
                float kMin = min(min(k.x, k.y), k.z);
                // normalized box Space real hit pos = rayOrigin + kmin * rayDir.
                pos += kMin * i.viewDir;

                // randomly flip & rotate cube map for some variety
                float3 flooredUV = floor(i.uvw);
                float3 r = rand3(flooredUV.x + flooredUV.y + flooredUV.z);
                float2 cubeflip = floor(r.xy * 2.0) * 2.0 - 1.0;
                pos.xz *= cubeflip;
                pos.xz = r.z > 0.5 ? pos.xz : pos.zx;
            #else
                // room uvs
                float2 roomUV = frac(i.uv);

                // raytrace box from tangent view dir
                float3 pos = float3(roomUV * 2.0 - 1.0, 1.0);
                float3 id = 1.0 / i.viewDir;
                float3 k = abs(id) - pos * id;
                float kMin = min(min(k.x, k.y), k.z);
                pos += kMin * i.viewDir;

                // randomly flip & rotate cube map for some variety
                float2 flooredUV = floor(i.uv);
                float3 r = rand3(flooredUV.x + 1.0 + flooredUV.y * (flooredUV.x + 1));
                float2 cubeflip = floor(r.xy * 2.0) * 2.0 - 1.0;
                pos.xz *= cubeflip;
                pos.xz = r.z > 0.5 ? pos.xz : pos.zx;
            #endif

                // sample room cube map
                fixed4 room = texCUBE(_RoomCube, pos.xyz);
                return fixed4(room.rgb, 1.0);
            }
            ENDCG
        }
    }
}