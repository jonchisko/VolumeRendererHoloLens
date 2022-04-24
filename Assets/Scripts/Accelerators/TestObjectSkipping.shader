Shader "Unlit/TestObjectSkipping"
{
    Properties
    {
        _IsoLimit ("IsoMinLimit", Range(0.0, 1.0)) = 0.1
        _ActiveGridTex ("ActiveGrid", 3D) = "" {}
        _ActiveGridDims ("ActiveGridDims", Vector) = (0, 0, 0, 0)
        _CompTopTex("CtDataTexture", 3D) = "" {}
        _CompTopTexDims("CtDataTexDims", Vector) = (0, 0, 0, 0)

        _GradientTex("CtGradientTexture", 3D) = "" {}
        _RandomTex("RandomValues", 3D) = "" {}
        _Transfer2D("Transfer2D", 2D) = "" {}

        _MinVal("MinVal", Range(0.0, 1.1)) = 0.0
        _MaxVal("MaxVal", Range(0.0, 1.1)) = 1.0

        _Plane("ClipPlane", Vector) = (1, 1, 1, 1)
        _XRange("XRange", Range(0.0, 0.5)) = 0.5
        _YRange("YRange", Range(0.0, 0.5)) = 0.5
        _ZRange("ZRange", Range(0.0, 0.5)) = 0.5

        _LightColor("LightColor", Vector) = (1, 1, 1, 1)
        _LightPosition("LightDirection", Vector) = (1, 1, 1, 1)
        _Shininess("Shinines", Range(1.0, 10.0)) = 1.0
        _LightPower("LightPower", Range(0.0, 10.0)) = 1.0

        _NumSteps("NumberOfSamplesAlongRay", Range(1, 10000)) = 512
        _ScaleSuperSample("ScaleSuperSample", Range(0, 10)) = 0

        _G("G_phaseFunctionHG", Range(-1, 1)) = 0
        _MaxDensityVal("MaxDesnityVal", Range(0.0, 1.1)) = 1
        _SigmaT("SigmaT", Range(0.1, 10000)) = 10
        _RayNumber("RayNumber", Range(1, 2000)) = 50
        _RayBounces("RayBounces", Range(1, 200)) = 50


    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100
        Cull Front
        ZTest Greater
        ZWrite On
        //Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Back
            ZTest Less
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 index : TEXCOORD0;
            };

            struct v2g 
            {
                float4 vertex : POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
            };

            float _IsoLimit;
            texture3D _ActiveGridTex;

            int4 _ActiveGridDims;

            // start "helper functions"

            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            // end "helper functions"


            v2g vert (appdata v)
            {
                g2f o;
                o.vertex = v.vertex;
                o.index = v.index;
                return o;
            }

            // 6 cube sides * 4 vertices
            [maxvertexcount(24)]
            void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
            {
                v2g p = IN[0];
                float3 grid_index = p.index.xyz;
                float3 grid_pos = p.vertex.xyz;
                g2f v;

                static float3 normals[6] = {
                    float3(1, 0, 0), 
                    float3(-1, 0, 0),
                    float3(0, 1, 0),
                    float3(0, -1, 0),
                    float3(0, 0, 1),
                    float3(0, 0, -1)
                };
                static float3 offsets[6] = {
                    float3(0, 0.5, 0.5),
                    float3(0, 0.5, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0.5, 0),
                    float3(0.5, 0.5, 0)
                };
                
                float center_intensity = get_max_iso(grid_index);
                float3 scaler = _ActiveGridDims.xyz;
                for (int i = 0; i < 6; i++) {
                    float3 normal_ind = normals[i];
                    float3 offset0 = offsets[i];

                    float3 neighbour_index = grid_index + normal_ind;
                    float neighbour_intensity = get_max_iso(neighbour_index);

                    // to ensure you only generate one quad and not two!
                    if (!(center_intensity >= _IsoLimit && neighbour_intensity < _IsoLimit) 
                        && (center_intensity < _IsoLimit || neighbour_index.x >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.y >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.z >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.x < _ActiveGridDims.x)
                        && (center_intensity < _IsoLimit || neighbour_index.y < _ActiveGridDims.y)
                        && (center_intensity < _IsoLimit || neighbour_index.z < _ActiveGridDims.z)) {
                        // this does not work with just return because it whines that not all fields were set ...
                        v.vertex = float4(0, 0, 0, 0) / 0; // force vert "discard"
                        v.index = float4(0, 0, 0, 0);
                        v.object_space = float3(0, 0, 0);
                        triStream.Append(v);
                        triStream.RestartStrip();
                        continue;
                    }
                    
                    // compute the center of the 'quad' we are generating
                    float3 quad_center = grid_pos + 0.5 * normal_ind / scaler;
                    // compute the other offset vector
                    float3 offset1 = cross(normal_ind, offset0);

                    // v0 ----- v2
                    //  |     / |
                    //  |    /  |
                    //  |   c   |
                    //  |  /    |
                    //  | /     |
                    // v1 ----- v3

                    //v0
                    v.vertex = UnityObjectToClipPos(float4(quad_center + offset0 / scaler, 1.0));
                    v.index = p.index;
                    v.object_space = float3(quad_center + offset0 / scaler);
                    triStream.Append(v); 

                    //v1
                    v.vertex = UnityObjectToClipPos(float4(quad_center + offset1 / scaler, 1.0));
                    v.index = p.index;
                    v.object_space = float3(quad_center + offset1 / scaler);
                    triStream.Append(v);

                    //v2
                    v.vertex = UnityObjectToClipPos(float4(quad_center - offset1 / scaler, 1.0));
                    v.index = p.index;
                    v.object_space = float3(quad_center - offset1 / scaler);
                    triStream.Append(v);

                    //v3
                    v.vertex = UnityObjectToClipPos(float4(quad_center - offset0 / scaler, 1.0));
                    v.index = p.index;
                    v.object_space = float3(quad_center - offset0 / scaler);
                    triStream.Append(v);

                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = fixed4(i.object_space.xyz, 1.0);
                return col;
            }
            ENDCG
        }

        GrabPass
        {
            "_BoundsCoordinatesTexture"
        }

        Pass
        {
            //Cull Front
            //ZTest Greater
            //ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 index : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
                float4 grab_pos: TEXCOORD2;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
                float4 grab_pos : TEXCOORD2;
            };

            float _IsoLimit;
            texture3D _ActiveGridTex;
            texture3D _CompTopTex;

            int4 _ActiveGridDims;
            sampler2D _BoundsCoordinatesTexture;

            // start "declare extra properties"
            int4 _CompTopTexDims;



            // end "declare extra properties"


            // start "helper functions"

            float trilinear_interpolation(float3 main_pos, texture3D tex3d, int4 tex3dDims) {
                // source: https://en.wikipedia.org/wiki/Trilinear_interpolation
                // deltas
                // main_pos has vals from 0.0 to 1.0, need to recalc them to actual tex dims
                float3 recalc_m_p = main_pos * tex3dDims.xyz;
                int x_min = int(floor(recalc_m_p.x));
                int x_max = int(ceil(recalc_m_p.x));
                float x_delta = (recalc_m_p.x - x_min) / (x_max - x_min);

                int y_min = int(floor(recalc_m_p.y));
                int y_max = int(ceil(recalc_m_p.y));
                float y_delta = (recalc_m_p.y - y_min) / (y_max - y_min);

                int z_min = int(floor(recalc_m_p.z));
                int z_max = int(ceil(recalc_m_p.z));
                float z_delta = (recalc_m_p.z - z_min) / (z_max - z_min);

                // c000, c001, c010, c011, c100, c101, c110, c111
                int4 c000_index = int4(x_min, y_min, z_min, 0);
                int4 c001_index = int4(x_min, y_max, z_min, 0);
                int4 c010_index = int4(x_min, y_min, z_max, 0);
                int4 c011_index = int4(x_min, y_max, z_max, 0);

                int4 c100_index = int4(x_max, y_min, z_min, 0);
                int4 c101_index = int4(x_max, y_max, z_min, 0);
                int4 c110_index = int4(x_max, y_min, z_max, 0);
                int4 c111_index = int4(x_max, y_max, z_max, 0);

                float3 c000 = tex3d.Load(c000_index).rgb;
                float3 c001 = tex3d.Load(c001_index).rgb;
                float3 c010 = tex3d.Load(c010_index).rgb;
                float3 c011 = tex3d.Load(c011_index).rgb;

                float3 c100 = tex3d.Load(c100_index).rgb;
                float3 c101 = tex3d.Load(c101_index).rgb;
                float3 c110 = tex3d.Load(c110_index).rgb;
                float3 c111 = tex3d.Load(c111_index).rgb;
                // c00, c01, c10, c11
                float c00 = c000 * (1.0 - x_delta) + c100 * x_delta;
                float c01 = c001 * (1.0 - x_delta) + c101 * x_delta;
                float c10 = c010 * (1.0 - x_delta) + c110 * x_delta;
                float c11 = c011 * (1.0 - x_delta) + c111 * x_delta;

                float c0 = c00 * (1.0 - y_delta) + c10 * y_delta;
                float c1 = c01 * (1.0 - y_delta) + c11 * y_delta;

                float c = c0 * (1.0 - z_delta) + c1 * z_delta;
                return c;
            }

            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            float getDensity(float3 pos)
            {
                return _CompTopTex.Load(int4(pos.xyz * float3(512, 512, 125), 0)).r; // TODO use dimensions that are SET!!!!! @Jon
            }

            // end "helper functions"


            // start "volume renderers"


            fixed4 frag_mip(g2f i) : SV_Target
            {
                float4 front_pos = tex2Dproj(_BoundsCoordinatesTexture, i.grab_pos);

                const float stepSize = 0.01;
                float3 rayDir = normalize(front_pos.xyz - i.object_space);
                float3 rayStartPos = i.object_space;
                float maxDistance = length(i.object_space - front_pos.xyz);

                float4 maxCol = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float maxDensity = 0.0f;
                for (float t = 0.0f; t < maxDistance; t += stepSize) {
                    const float3 currentPos = rayStartPos + t * rayDir;

                    const float density = getDensity(currentPos);

                    const float4 col = float4(density, density, density, density);

                    if (col.w > maxCol.w) {
                        maxCol = col;
                    }
                }
                float4 finalCol = maxCol;
                return finalCol;
            }
            // end "volume renderers"


            v2g vert(appdata v)
            {
                g2f o;
                o.vertex = v.vertex;
                o.index = v.index;
                return o;
            }

            // 6 cube sides * 4 vertices
            [maxvertexcount(24)]
            void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
            {
                v2g p = IN[0];
                float3 grid_index = p.index.xyz;
                float3 grid_pos = p.vertex.xyz;
                g2f v;

                static float3 normals[6] = {
                    float3(1, 0, 0),
                    float3(-1, 0, 0),
                    float3(0, 1, 0),
                    float3(0, -1, 0),
                    float3(0, 0, 1),
                    float3(0, 0, -1)
                };
                static float3 offsets[6] = {
                    float3(0, 0.5, 0.5),
                    float3(0, 0.5, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0, 0.5),
                    float3(0.5, 0.5, 0),
                    float3(0.5, 0.5, 0)
                };

                float center_intensity = get_max_iso(grid_index);
                float3 scaler = _ActiveGridDims.xyz;
                for (int i = 0; i < 6; i++) {
                    float3 normal_ind = normals[i];
                    float3 offset0 = offsets[i];

                    float3 neighbour_index = grid_index + normal_ind;
                    float neighbour_intensity = get_max_iso(neighbour_index);

                    // to ensure you only generate one quad and not two!
                    if (!(center_intensity >= _IsoLimit && neighbour_intensity < _IsoLimit)
                        && (center_intensity < _IsoLimit || neighbour_index.x >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.y >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.z >= 0)
                        && (center_intensity < _IsoLimit || neighbour_index.x < _ActiveGridDims.x)
                        && (center_intensity < _IsoLimit || neighbour_index.y < _ActiveGridDims.y)
                        && (center_intensity < _IsoLimit || neighbour_index.z < _ActiveGridDims.z)) {
                        // this does not work with just return because it whines that not all fields were set ...
                        v.vertex = float4(0, 0, 0, 0) / 0; // force vert "discard"
                        v.index = float4(0, 0, 0, 0);
                        v.object_space = float3(0, 0, 0);
                        v.grab_pos = float4(0, 0, 0, 0);
                        triStream.Append(v);
                        triStream.RestartStrip();
                        continue;
                    }

                    // compute the center of the 'quad' we are generating
                    float3 quad_center = grid_pos + 0.5 * normal_ind / scaler;
                    // compute the other offset vector
                    float3 offset1 = cross(normal_ind, offset0);

                    // v0 ----- v2
                    //  |     / |
                    //  |    /  |
                    //  |   c   |
                    //  |  /    |
                    //  | /     |
                    // v1 ----- v3

                    //v0
                    float4 computed_pos = float4(quad_center + offset0 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = computed_pos.xyz;
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    triStream.Append(v);

                    //v1
                    computed_pos = float4(quad_center + offset1 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = computed_pos.xyz;
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    triStream.Append(v);

                    //v2
                    computed_pos = float4(quad_center - offset1 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = computed_pos.xyz;
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    triStream.Append(v);

                    //v3
                    computed_pos = float4(quad_center - offset0 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = computed_pos.xyz;
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    triStream.Append(v);

                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = frag_mip(i);
                return col;
            }
            ENDCG
        }
    }
}
