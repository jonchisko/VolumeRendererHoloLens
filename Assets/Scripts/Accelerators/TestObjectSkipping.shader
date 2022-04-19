Shader "Unlit/TestObjectSkipping"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IsoLimit ("IsoMinLimit", Range(0.0, 1.0)) = 0.1
        _ActiveGridTex ("ActiveGrid", 3D) = "" {}
        _ActiveGridDims ("ActiveGridDims", Vector) = (0, 0, 0, 0)
        _CompTopTex("CtDataTexture", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _IsoLimit;
            texture3D _ActiveGridTex;

            int4 _ActiveGridDims;

            // helper functions
            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            // end helper functions


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
            Cull Front
            ZTest Greater
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
                float4 grab_pos: TEXCOORD2;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
                float4 grab_pos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _IsoLimit;
            texture3D _ActiveGridTex;

            int4 _ActiveGridDims;
            sampler2D _BoundsCoordinatesTexture;

            // helper functions
            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            // end helper functions


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
                // sample the texture
                float4 first_pass_value = tex2Dproj(_BoundsCoordinatesTexture, i.grab_pos);
                float diff_length = length(i.object_space.xyz - first_pass_value.xyz);
                fixed4 col = fixed4(i.object_space.xyz, 1.0);
                return col;
            }
            ENDCG
        }

        GrabPass
        {
            "_BoundsCoordinatesTexture2"
        }

        Pass
        {
            Cull back
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
                float4 grab_pos : TEXCOORD2;
                float3 vec_to_surf : TEXCOORD3;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 index : TEXCOORD0;
                float3 object_space : TEXCOORD1;
                float4 grab_pos : TEXCOORD2;
                float3 vec_to_surf : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _IsoLimit;
            texture3D _ActiveGridTex;

            int4 _ActiveGridDims;
            sampler2D _BoundsCoordinatesTexture2;
            texture3D _CompTopTex;

            // helper functions
            float get_max_iso(float3 grid_pos) {
                return _ActiveGridTex.Load(int4(grid_pos.xyz, 0)).r;
            }

            // end helper functions


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
                        v.vec_to_surf = float3(0, 0, 0);
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
                    v.object_space = float3(computed_pos.xyz);
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    v.vec_to_surf = computed_pos - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    triStream.Append(v);

                    //v1
                    computed_pos = float4(quad_center + offset1 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = float3(computed_pos.xyz);
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    v.vec_to_surf = computed_pos - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    triStream.Append(v);

                    //v2
                    computed_pos = float4(quad_center - offset1 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = float3(computed_pos.xyz);
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    v.vec_to_surf = computed_pos - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    triStream.Append(v);

                    //v3
                    computed_pos = float4(quad_center - offset0 / scaler, 1.0);
                    v.vertex = UnityObjectToClipPos(computed_pos);
                    v.index = p.index;
                    v.object_space = float3(computed_pos.xyz);
                    v.grab_pos = ComputeGrabScreenPos(v.vertex);
                    v.vec_to_surf = computed_pos - mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                    triStream.Append(v);

                    triStream.RestartStrip();
                }
            }


            float getDensity(float3 pos)
            {
                //return tex3Dlod(_CompTopTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).r;
                //return tex3Dlod(_CompTopTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).r;
                return _CompTopTex.Load(int4(pos.xyz * float3(512, 512, 125), 0)).r;
            }

            fixed4 frag_mip(g2f i) : SV_Target
            {
                float4 endPos = tex2Dproj(_BoundsCoordinatesTexture2, i.grab_pos);

                const float stepSize = 0.01;
                float3 rayDir = normalize(i.vec_to_surf);
                float3 rayStartPos = i.object_space;
                float maxDistance = length(i.object_space - endPos.xyz);

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
                //return float4(endPos.xyz - i.object_space, 1.0);
                return finalCol;// float4(rayStartPos.xyz, 1.0);// finalCol;
            }

                fixed4 frag(g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = frag_mip(i);
                //float4 endPos = tex2Dproj(_BoundsCoordinatesTexture2, i.grab_pos);
                //float dist = length(i.object_space - endPos.xyz);
                //fixed4 col = float4(dist, dist, dist, 1);
                return col;
            }
            ENDCG
        }
    }
}
