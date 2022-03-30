Shader "Unlit/TestObjectSkipping"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IsoLimit ("IsoMinLimit", Range(0.0, 1.0)) = 0.1
        _ActiveGridTex ("ActiveGrid", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off // TODO JUST FOR TEST
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 tangent: TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2g 
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _IsoLimit;
            texture3D _ActiveGridTex;


            // helper functions
            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            // end helper functions


            v2g vert (appdata v)
            {
                g2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            // 6 cube sides * 4 vertices
            [maxvertexcount(24)]
            void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
            {
                v2g p = IN[0];
                float3 grid_pos = p.vertex.xyz;
                g2f v;

                static float3 normals[6] = {
                    float3(1, 0, 0), 
                    float3(-1, 0, 0),
                    float3(0, 1, 0),
                    float3(0, -1, 0),
                    float3(0, 0, 1),
                    float3(0, 0, -1),
                };
                static float3 offsets[6] = {
                    float3(0, 0.5, 0.5),
                    float3(0, 0.5, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0.5, 0),
                    float3(0.5, 0.5, 0)
                };
                
                for (int i = 0; i < 6; i++) {
                    float3 normal = normals[i];
                    float3 offset0 = offsets[i];

                    float center_intensity = get_max_iso(grid_pos);
                    float3 neighbour_pos = grid_pos + normal;
                    float neighbour_intensity = get_max_iso(neighbour_pos);

                    // to ensure you only generate one quad and not two!
                    /*if (!(center_intensity >= _IsoLimit && neighbour_intensity <= _IsoLimit)) {
                        //return; // this does not work with just return because it whines that not all fields were set ...
                        v.vertex = float4(0, 0, 0, 0) / 0; // force vert "discard"
                        triStream.Append(v);
                        triStream.RestartStrip();
                        return;
                    }*/

                    if (center_intensity < _IsoLimit) {
                        v.vertex = float4(0, 0, 0, 0) / 0; // force vert "discard"
                        triStream.Append(v);
                        triStream.RestartStrip();
                        return;
                    }

                    // compute the center of the 'quad' we are generating
                    float3 quad_center = grid_pos + 0.5 * normal;
                    // compute the other offset vector
                    float3 offset1 = cross(normal, offset0);

                    // v0 ----- v2
                    //  |     / |
                    //  |    /  |
                    //  |   c   |
                    //  |  /    |
                    //  | /     |
                    // v1 ----- v3

                    //v0
                    v.vertex = UnityObjectToClipPos(float4(quad_center + offset0, 1.0));
                    v.uv = TRANSFORM_TEX(p.uv, _MainTex);
                    triStream.Append(v); 

                    //v1
                    v.vertex = UnityObjectToClipPos(float4(quad_center + offset1, 1.0));
                    v.uv = TRANSFORM_TEX(p.uv, _MainTex);
                    triStream.Append(v);

                    //v2
                    v.vertex = UnityObjectToClipPos(float4(quad_center - offset1, 1.0));
                    v.uv = TRANSFORM_TEX(p.uv, _MainTex);
                    triStream.Append(v);

                    //v3
                    v.vertex = UnityObjectToClipPos(float4(quad_center - offset0, 1.0));
                    v.uv = TRANSFORM_TEX(p.uv, _MainTex);
                    triStream.Append(v);

                    triStream.RestartStrip();
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
