Shader "InteriorMapGeneratorShader/PreprojectAtlasshader"
{
    Properties
    {
        _RoomTex("Room Atlas RGB (A - back wall fraction)", 2D) = "white" {}
        _Rooms("Room Atlas Rows&Cols (XY)", Vector) = (1,1,0,0)
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
                    float2 uv : TEXCOORD0;
                    float3 tangentViewDir : TEXCOORD1;
                };
                sampler2D _RoomTex;
                float4 _RoomTex_ST;
                float2 _Rooms;
                float _RoomDepth;
                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _RoomTex);

                    // get tangent space camera vector
                    float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    float3 viewDir = v.vertex.xyz - objCam.xyz;
                    float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    float3 bitangent = cross(v.normal.xyz, v.tangent.xyz) * tangentSign;
                    o.tangentViewDir = float3(
                        dot(viewDir, v.tangent.xyz),
                        dot(viewDir, bitangent),
                        dot(viewDir, v.normal)
                        );
                    o.tangentViewDir *= _RoomTex_ST.xyx;
                    return o;
                }

                // psuedo random
                float2 rand2(float co) {
                    return frac(sin(co * float2(12.9898,78.233)) * 43758.5453);
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // room uvs
                    float2 roomUV = frac(i.uv);
                    float2 roomIndexUV = floor(i.uv);

                    // randomize the room
                    float2 n = floor(rand2(roomIndexUV.x + roomIndexUV.y * (roomIndexUV.x + 1)) * _Rooms.xy);
                    //float2 n = floor(_Rooms.xy);
                    roomIndexUV += n;

                    // get room depth from room atlas alpha
                    // fixed farFrac = tex2D(_RoomTex, (roomIndexUV + 0.5) / _Rooms).a;

                    // Specify depth manually
                    fixed farFrac = _RoomDepth;

                    //remap [0,1] to [+inf,0]
                    //->if input _RoomDepth = 0    -> depthScale = 0      (inf depth room)
                    //->if input _RoomDepth = 0.5  -> depthScale = 1
                    //->if input _RoomDepth = 1    -> depthScale = +inf   (0 volume room)
                    float depthScale = 1.0 / (1.0 - farFrac) - 1.0;

                    // raytrace box from view dir
                    // normalized box space's ray start pos is on trinagle surface, where z = -1
                    float3 pos = float3(roomUV * 2 - 1, -1);
                    // transform input ray dir from tangent space to normalized box space
                    i.tangentViewDir.z *= -depthScale;

                    // 预先处理倒数  t=(1-p)/view=1/view-p/view
                    float3 id = 1.0 / i.tangentViewDir;
                    float3 k = abs(id) - pos * id;
                    float kMin = min(min(k.x, k.y), k.z);
                    pos += kMin * i.tangentViewDir;

                    // remap from [-1,1] to [0,1] room depth
                    float interp = pos.z * 0.5 + 0.5;

                    // account for perspective in "room" textures
                    // assumes camera with an fov of 53.13 degrees (atan(0.5))
                    // visual result = transform nonlinear depth back to linear
                    float realZ = saturate(interp) / depthScale + 1;
                    interp = 1.0 - (1.0 / realZ);
                    interp *= depthScale + 1.0;

                    // iterpolate from wall back to near wall
                    float2 interiorUV = pos.xy * lerp(1.0, farFrac, interp);

                    interiorUV = interiorUV * 0.5 + 0.5;

                    // sample room atlas texture
                    fixed4 room = tex2D(_RoomTex, (roomIndexUV + interiorUV.xy) / _Rooms);
                    return room;
                }
            ENDCG
        }
    }
    FallBack "Diffuse"
}