Shader "Unlit/Map3dShader"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100
        Cull Front
        ZTest LEqual
        ZWrite On
        //Blend SrcAlpha OneMinusSrcAlpha
        //ZWrite Off

        // Cull Front/Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define MAX_CUBE_DIST 1.732f
            #define NUM_STEPS 128

            struct appdata // vertex in 
            {
                float4 vertex : POSITION;
                float4 normal: NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f // fragment in
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 vertexLocal: TEXCOORD1;
                float3 normal: NORMAL;
                float3 vectorToSurface: TEXCOORD2;
            };

            sampler3D _MainTex;


            float getDensity(float3 pos)
            {
                return tex3Dlod(_MainTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).r;
            }


            float3 getAbsFloat3(float3 position) {
                return float3(abs(position.x), abs(position.y), abs(position.z));
            }

            float rand_1_05(float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            float3 getOffsetPosition(v2f i, float3 dir) {
                return i.vertexLocal + (2.0f * dir / NUM_STEPS) * rand_1_05(i.uv);
            }



            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexLocal = v.vertex; // vertex in object space 
                o.vectorToSurface = v.vertex - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)); // ray direction is in object space
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag_mip(v2f i) : SV_Target
            {
                const float StepSize = MAX_CUBE_DIST / NUM_STEPS;
                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);

                float4 maxCol = float4(1.0f, 1.0f, 1.0f, 0.2f);
                float3 maxPosition = float3(0.0f, 0.0f, 0.0f);
                float maxDensity = 0.0f;
                for (int iStep = 0; iStep < NUM_STEPS; iStep++) {
                    const float t = iStep * StepSize;
                    const float3 currentPos = rayStartPos + t * rayDir;
                    float3 abs_xyz = getAbsFloat3(currentPos);
                    if (abs_xyz.x > 0.5f || abs_xyz.y > 0.5f || abs_xyz.z > 0.5f) {
                       continue;
                    } // dont sample if we are outside the unit box bounds
                    const float density = getDensity(currentPos);

                    if (density > maxDensity) {
                        maxDensity = density;
                        maxCol = float4(1.0f, 0.0f, 0.0f, maxDensity);
                        maxDensity = density;
                        maxPosition = currentPos;
                    }
                }
                return maxCol;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = frag_mip(i);
                return col;
            }
            ENDCG
        }
    }
}
