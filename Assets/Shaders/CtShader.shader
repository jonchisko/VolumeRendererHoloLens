Shader "Unlit/CtShader"
{
    Properties
    {
        _CompTopTex("CtDataTexture", 3D) = "" {}
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
        ZTest LEqual
        ZWrite On
        //Blend SrcAlpha OneMinusSrcAlpha
        //ZWrite Off

        // Cull Front/Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma multi_compile MODE_DVR MODE_MIP MODE_SRF MODE_CINEMA
            #pragma multi_compile __ TF1D_MODE TF2D_MODE
            #pragma multi_compile __ LOCAL_LIGHTING_BP LOCAL_LIGHTING_CT
            #pragma multi_compile __ CLIP
            #pragma multi_compile __ B_SPLINE_FILTER
            #pragma multi_compile __ PERLIN_NOISE 

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define MAX_CUBE_DIST 1.732f
            #define SPECULAR_INTENSITY 0.25
            #define ALBEDO_INTENSITY 0.2
            #define SMALL_EPSILON 0.00000001;

            #define PI 3.14159265358979323846;
            #define INV_PI 0.31830988618379067154;
            #define INV_2PI 0.15915494309189533577;
            #define INV_4PI 0.07957747154594766788;
            #define SQRT2 1.41421356237309504880;
            #define ONE_MINUS_EPSILON 0x1.fffffffffffffp-1;



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

            sampler3D _CompTopTex;
            sampler3D _GradientTex;
            sampler3D _RandomTex;
            sampler2D _Transfer2D;

            float _MinVal;
            float _MaxVal;
            float4 _Plane;

            float _XRange;
            float _YRange;
            float _ZRange;

            fixed4 _LightColor;
            float _Shininess;
            float4 _LightPosition;
            float _LightPower;

            float _NumSteps;
            float _ScaleSuperSample;

            float _G;

            float _MaxDensityVal;
            float _SigmaT;
            int _RayNumber;
            int _RayBounces;

            // helper functions
            /*
                Like @Invertex said, using
                tex2Dlod
                instead of
                tex2D
                is theoretically slightly more efficient in terms of the GPU not having to compute the mip level. 
                If you're doing something like a full screen, screen space texture sample (like for a post process effect) 
                where the texture is only ever going to need a specific mip level, then yes, this could be faster.
            */
            float getDensity(float3 pos) 
            {
                return tex3Dlod(_CompTopTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).r;
            }

            float3 getGradient(float3 pos)
            {
                return normalize(tex3Dlod(_GradientTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).rgb);
            }

            float4 getRandomValue(float3 pos) 
            {
                return tex3Dlod(_RandomTex, float4(pos.xyz, 0.0f) + float4(0.5f, 0.5f, 0.5f, 0.0f)).rgba;
            }

            float4 getTF1DColor(float density) 
            {
                return tex2Dlod(_Transfer2D, float4(density, 0.0f, 0.0f, 0.0f)); 
            }

            float4 getTF2DColor(float density, float gradientMagnitude) 
            {
                return tex2Dlod(_Transfer2D, float4(density, gradientMagnitude/MAX_CUBE_DIST, 0.0f, 0.0f));
            }

            //blinn phong model
            float3 calculateLighting_blinnphong(float3 col, float3 normal, float3 position, float3 eyeDir)
            {
                float3 lightDir = -normalize(position - _LightPosition);
                float ndotl = max(dot(normal, lightDir), 0.25);
                float3 diffuse = ndotl * col;
                float3 v = normalize(eyeDir);
                //float3 r = normalize(reflect(-lightDir, normal));
                float3 h = normalize(lightDir + v);
                float ndoth = max(dot(normal, h), 0.0);
                float3 specular = pow(ndoth, _Shininess) * _LightColor.rgb * SPECULAR_INTENSITY;
                return col * ALBEDO_INTENSITY + (diffuse + specular) * _LightPower;
            }

            //cook torrance model
            float3 calculateLighting_cooktorrance(float3 col, float3 normal, float3 position, float3 eyeDir) 
            {

                float roughness = 0.5; // roughness
                float material_ref = 0.5; // material index of refraction n

                float3 lightDir = -normalize(position - _LightPosition);

                float3 v = normalize(eyeDir);
                float h = normalize(lightDir + v);
                float nh = max(dot(normal, h), 0.1);
                float nv = max(dot(normal, v), 0.1);
                float vh = max(dot(v, h), 0.1);
                float nl = max(dot(normal, lightDir), 0.1);

                float ndotl = max(dot(normal, lightDir), 0.25);
                float3 diffuse = ndotl * col;

                // fresnel
                float f0 = pow(material_ref - 1, 2) / pow(material_ref + 1, 2);
                float fresnel = f0 + (1 - f0) * pow(1 - vh, 5);
                // microfacet distribution D
                float power = max((2 / pow(roughness, 2)) - 2, 0);
                float microfacet = pow(nh, power) / (4 * pow(roughness, 2));
                //sel-shadowing term
                float term1 = (2 * nh * nv) / vh;
                float term2 = (2 * nh * nl) / vh;
                float selfshadow = min(1, min(term1, term2));

                float ct_term = (fresnel * microfacet * selfshadow) / (4 * nl * nv);

                float3 specular = max(_LightColor.rgb * SPECULAR_INTENSITY * ct_term * _Shininess, 0);
                return col * ALBEDO_INTENSITY + (diffuse + specular) * _LightPower;
            }
            // TODO
            /*float3 local_ambient(float3 pos, int ray_samples, float a_offset) {

                float4 randomVector4_1 = float4(random3(), random3( + 5), random3( + 10), random3( + 15));
                getRandomDirectionOnUnitSphere(float u_randomPhi, float u_randomZ);
            }*/

            float rand_1_05(float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            float2 rand_2_10(float2 uv) {
                float noiseX = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                float noiseY = sqrt(1 - noiseX * noiseX);
                return float2(noiseX, noiseY);
            }

            float2 rand_2_0004(float2 uv)
            {
                float noiseX = (frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453));
                float noiseY = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return float2(noiseX, noiseY) * 0.004;
            }


            // random (???) source: https://stackoverflow.com/questions/4200224/random-noise-functions-for-glsl
            // A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm.
            uint hash(uint x) {
                x += (x << 10u);
                x ^= (x >> 6u);
                x += (x << 3u);
                x ^= (x >> 11u);
                x += (x << 15u);
                return x;
            }

            // Compound versions of the hashing algorithm
            uint hash2(float2 v) { return hash(asuint(v.x) ^ hash(asuint(v.y))); }
            uint hash3(float3 v) { return hash(asuint(v.x) ^ hash(asuint(v.y)) ^ hash(asuint(v.z))); }
            uint hash4(float4 v) { return hash(asuint(v.x) ^ hash(asuint(v.y)) ^ hash(asuint(v.z)) ^ hash(asuint(v.w))); }

            // Construct a float with half-open range [0:1] using low 23 bits.
            // All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
            float floatConstruct(uint m) {
                const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
                const uint ieeeOne = 0x3F800000u; // 1.0 in IEEE binary32

                m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
                m |= ieeeOne;                          // Add fractional part to 1.0

                float  f = asfloat(m);       // Range [1:2]
                return f - 1.0;                        // Range [0:1]
            }

            // Pseudo-random value in half-open range [0:1].
            float random1(float x) { return floatConstruct(hash(asuint(x))); }
            float random2(float2  v) { return floatConstruct(hash2(asuint(v))); }
            float random3(float3  v) { return floatConstruct(hash3(asuint(v))); }
            float random4(float4  v) { return floatConstruct(hash4(asuint(v))); }

            //

            float3 getAbsFloat3(float3 position) {
                return float3(abs(position.x), abs(position.y), abs(position.z));
            }

            float3 getOffsetPosition(v2f i, float3 dir) {
#ifdef PERLIN_NOISE
                return i.vertexLocal + (2.0f * dir / _NumSteps) * perlinNoise(i.uv);
#else
                return i.vertexLocal + (2.0f * dir / _NumSteps) * rand_1_05(i.uv);
#endif
            }

            bool isInBounds(float3 currentPosition) {
                float3 abs_xyz = getAbsFloat3(currentPosition);
                if (abs_xyz.x > _XRange || abs_xyz.y > _YRange || abs_xyz.z > _ZRange) { // TODO: you have to change this, because changing this range, also removes the back fase since its absoulte!!!!
                    // object space cube goes from -0.5 to 0.5 with one unit size
                    // if you use less than 0.5, obviously you dont see anything, because you discard at the entry already!
                    return false;
                } // dont sample if we are outside the unit box bounds
                return true;
            }

            bool isInsideClip(float3 currentPos) {
                float3 currentPos_inWorld = mul(unity_ObjectToWorld, float4(currentPos, 1.0));
                float clip_distance = dot(currentPos_inWorld, _Plane.xyz);
                clip_distance += _Plane.w;

                if (clip_distance <= 0) return false;
                return true;
            }


            // VOLUME PATH TRACER functions
            // Phase Henyey Greenstein
            float phaseHG(float cosTheta) {
                float denom = 1 + _G * _G + 2 * _G * cosTheta;
                float nomin = (1 - _G * _G) * INV_4PI;
                return nomin / (denom * sqrt(denom));
            }

            float getHgP(float3 wo, float3 wi) {
                // wi and wo should be normalized input vectors
                return phaseHG(dot(wo, wi));
            }

            float sample_phase(float3 wo, out float3 wi, float u1, float u2) {
                
                float cosTheta;
                if (abs(_G) < 1e-3) {
                    cosTheta = 1.0f - 2.0f * u1;
                } else {
                    float sqrTerm = (1.0f - _G * _G) / (1.0 - _G + 2.0f * _G * u1);
                    cosTheta = (1.0f + _G * _G - sqrTerm * sqrTerm) / (2.0f * _G);
                }

                float sinTheta = sqrt(max(0.0f, 1.0f - cosTheta * cosTheta));

                float phi = 2.0f * u2 * PI;

                float3 v1;
                float3 v2;

                // create coordinate system of wo and v1, v2
                if (abs(wo.x) > abs(wo.y)) {
                    v1 = normalize(float3(-wo.z, 0.0f, wo.x));
                } else {
                    v1 = normalize(float3(0.0f, wo.z, -wo.y));
                }

                v2 = cross(wo, v1);
                // set wi
                wi = v1 * (sinTheta * cos(phi)) + v2 * (sinTheta * sin(phi)) + wo * (cosTheta);

                return phaseHG(cosTheta);
            }
            // END Phase Henyey Greenstein

            float getInvMax(float max) {
                return 1 / max;
            }

            float3 getRandomDirectionOnUnitSphere(float u_randomPhi, float u_randomZ) {
                float randomPhi = u_randomPhi * 2 * PI;  // 0 ... 2pi
                float randomZ = u_randomZ * 2 - 1; // -1 ... 1
                return float3(sqrt(1 - randomZ * randomZ) * cos(randomPhi), sqrt(1 - randomZ * randomZ) * sin(randomPhi), randomZ);
            }

            bool isColorBlack(float4 col) {
                bool colx = col.x < SMALL_EPSILON;
                bool coly = col.y < SMALL_EPSILON;
                bool colz = col.z < SMALL_EPSILON;
                return colx && coly && colz; //&& cola < SMALL_EPSILON;
            }

            float powerHeuristic(float nf, float fPdf, float ng, float gPdf) {
                float f = nf * fPdf;
                float g = ng * gPdf;
                return (f * f) / (f * f + g * g);
            }

            // Medium functions

            float4 mediumSample(float3 currentPosition, float3 currentDirection, out float3 newPosition, float2 random) {
                float t = 0;
                int counter = 0;
                while (true) {
                    float2 rand2 = float2(random2(random.xy + counter), random2(random.xy + counter + 100));
                    t = t - (log(1.0f - rand2.x) * getInvMax(_MaxDensityVal) / _SigmaT);
                    currentPosition = currentPosition + t * currentDirection;
                    
                    newPosition = currentPosition; // update out newPosition!
                    if (!isInBounds(currentPosition)) {
                        break;
                    }

                    float orgDensity = getDensity(currentPosition);
                    if (orgDensity < _MinVal || orgDensity > _MaxVal) continue;

                    float4 transferFunctionDensity = float4(0.0f, 0.0f, 0.0f, 0.0f);
                    float maxGradient;
#if TF2D_MODE
                    maxGradient = getGradient(currentPosition);
#endif
                    transferFunctionDensity = getTF1DColor(orgDensity);
#if TF2D_MODE
                    transferFunctionDensity = getTF2DColor(orgDensity, length(maxGradient));
#endif

                    if (orgDensity * getInvMax(_MaxDensityVal) > rand2.y) { // transferFunctionDensity.w
                        return float4(transferFunctionDensity.x * orgDensity, transferFunctionDensity.y * orgDensity, transferFunctionDensity.z * orgDensity, 1.0f); // orgDensity
                    }

                    counter++;
                }
                return float4(0.0f, 0.0f, 0.0f, 0.0f); // // TEMPORARY FIX, these are all ones
            }

            float4 mediumTr(float3 currentPosition, float3 currentDirection, float2 random) {
                float tr = 1;
                float t = 0;
                int counter = 0;
                while (true) {
                    float2 rand2 = float2(random2(random.xy + counter), random2(random.xy + counter + 100));
                    t = t - (log(1.0f - rand2.x) * getInvMax(_MaxDensityVal) / _SigmaT);
                    currentPosition = currentPosition + t * currentDirection;
                    if (!isInBounds(currentPosition)) {
                        break;
                    }


                    float orgDensity = getDensity(currentPosition);
                    float4 transferFunctionDensity = float4(0.0f, 0.0f, 0.0f, 0.0f);
                    float maxGradient;
#if TF2D_MODE
                    maxGradient = getGradient(currentPosition);
#endif
                    transferFunctionDensity = getTF1DColor(orgDensity);
#if TF2D_MODE
                    transferFunctionDensity = getTF2DColor(orgDensity, length(maxGradient));
#endif

                    tr = tr * (1.0f - max(0, orgDensity * getInvMax(_MaxDensityVal))); // transferFunctionDensity.w

                    counter++;
                }

                return float4(tr, tr, tr, 1.0f);
            }

            // END Medium functions


            // PointLight
            float3 setWiPointLight(float3 currentPosition) {
                float3 wi = normalize(_LightPosition.xyz - currentPosition);
                return wi;
            }

            float setLightPdfPointLight() {
                return 1.0;
            }

            float getPointLightPower() {
                return _LightColor * 4 * PI;
            }

            float4 samplePointLightLi(float3 currentPosition) {
                float sqrterm = length(_LightPosition.xyz - currentPosition);
                return _LightColor / (sqrterm * sqrterm);
            }

            float4 visibilityPointLightTr(float3 currentPosition, float2 twoRands) {
                // get noramlized direction
                // float4 tr = 1
                // sampleMedium Tr *= mediumTr(direction)
                // return tr
                float4 Tr = float4(1, 1, 1, 1);
                float3 direction = normalize(_LightPosition - currentPosition);
                Tr = Tr * mediumTr(currentPosition, direction, twoRands);
                return Tr;
            }
            // END PointLight

            float4 estimateDirect(float3 currentPosition, float3 wo, float ulight1, float ulight2, float uscattering1, float uscattering2, float2 twoRands) {
                float4 Ld = float4(0, 0, 0, 0);
                // sample light source with MIS
                float3 wi;
                float lightPdf = 0;
                float scatteringPdf = 0;

                float4 Li = samplePointLightLi(currentPosition); // todo: if area light, should sampleAreaLightLi
                lightPdf = setLightPdfPointLight();
                wi = setWiPointLight(currentPosition);

                if (lightPdf > 0 && !isColorBlack(Li)) {
                    float p = getHgP(wo, wi);
                    float4 f = float4(p, p, p, p);
                    scatteringPdf = p;

                    if (!isColorBlack(f)) {
                        Li *= visibilityPointLightTr(currentPosition, twoRands); // todo: if area light, should use visibilityAreaLightTr()
                        if (!isColorBlack(Li)) {
                            // todo: currently only point light, add an option for point lights or sphere/area
                            if (true) { // always point light
                                Ld += f * Li / lightPdf;
                            } else { // sphere option
                                float weight = powerHeuristic(1.0, lightPdf, 1.0, scatteringPdf);
                                Ld += f * Li * weight / lightPdf;
                            }
                        }
                    }

                }
                // todo: implement area light, example SampleSphereLight()

                // sample BSDF with MIS
                if (false) { // todo: this is actually !pointLight, so this only executes for area lights, because sampling the sphere around our current position would almost always miss the delta light

                }

                return Ld;
            }

            float4 UniformSampleOneLight(float3 currentPosition, float3 wo, float2 twoRands, float4 fourRands) {
                float2 ulight2 = fourRands.xy; // getRandomValue(currentPosition).xy; //rand_2_0004(currentPosition.xz); // TODO: sample from textures
                float2 uscattering2 = fourRands.zw; // getRandomValue(currentPosition).zw; // rand_2_0004(wo.xy);
                return estimateDirect(currentPosition, wo, ulight2.x, ulight2.y, uscattering2.x, uscattering2.y, twoRands);
            }

            float4 Li(float3 currentPosition, float3 currentDirection, int bounceLimit, float3 vert_pos) {
                float4 L = float4(0, 0, 0, 0);
                float4 beta = float4(0, 0, 0, 0); // TEMPORARY FIX,, these are all ones

                for (int bounce = 0;; bounce++) {
                    float3 newPosition = float3(0, 0, 0);
                    float4 randomVector4_1 = float4(random3(vert_pos + currentPosition + bounce), random3(vert_pos + currentPosition + bounce + 5), random3(vert_pos + currentPosition + bounce + 10), random3(vert_pos + currentPosition + bounce + 15));
                    float4 randomVector4_2 = float4(random3(vert_pos + currentPosition + bounce + 20), random3(vert_pos + currentPosition + bounce + 25), random3(vert_pos + currentPosition + bounce + 30), random3(vert_pos + currentPosition + bounce + 35));
                    float3 randomVector3_3 = float3(random3(vert_pos + currentPosition + bounce + 40), random3(vert_pos + currentPosition + bounce + 45), random3(vert_pos + currentPosition + bounce + 50));
                    beta += mediumSample(currentPosition, currentDirection, newPosition, randomVector4_1.xy); // TEMPORARY FIX, this is multiplication otherwise!!

                    float3 wo = -currentDirection; // try with and without minus
                    L += beta * UniformSampleOneLight(currentPosition, wo, randomVector4_1.zw, randomVector4_2); // // TEMPORARY FIX, should be *
                    float2 u2 = randomVector3_3.xy; // getRandomValue(currentPosition).xz; //rand_2_0004(wo.xy); // TODO: sample from textures
                    float3 wi = float3(0, 0, 0);
                    sample_phase(wo, wi, u2.x, u2.y);


                    if (bounce > bounceLimit) {
                        float q = max(0.08, 1 - beta.y); // default 0.05
                        float rand = randomVector3_3.z; //getRandomValue(currentPosition).w;
                        if (rand <= q) {
                            break;
                        }
                        beta /= 1 - q; 
                    }
                    currentPosition = newPosition;
                    currentDirection = wi;
                }
                return L;
            }

            fixed4 frag_volpath(v2f i) : SV_Target
            {
                const float StepSize = MAX_CUBE_DIST / _NumSteps;

                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);

                float4 radiance = float4(0, 0, 0, 0);

                for (int s = 0; s < _RayNumber; s++) {
                    radiance += Li(rayStartPos, rayDir, _RayBounces, i.vertex);
                }

                return radiance / _RayNumber;
            }


            // end VOLUME PATH TRACER functions


            // end helper functions

            // different fragment functions

            fixed4 frag_mip(v2f i) : SV_Target
            {
                const float StepSize = MAX_CUBE_DIST / _NumSteps;
                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);

                float4 maxCol = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float3 maxGradient = float3(0.0f, 0.0f, 0.0f);
                float3 maxPosition = float3(0.0f, 0.0f, 0.0f);
                float maxDensity = 0.0f;
                for (int iStep = 0; iStep < _NumSteps; iStep++) {
                    const float t = iStep * StepSize;
                    const float3 currentPos = rayStartPos + t * rayDir;
                    float3 abs_xyz = getAbsFloat3(currentPos);
                    if (abs_xyz.x > _XRange || abs_xyz.y > _YRange || abs_xyz.z > _ZRange) { // TODO: you have to change this, because changing this range, also removes the back fase since its absoulte!!!!
                        // object space cube goes from -0.5 to 0.5 with one unit size
                        // if you use less than 0.5, obviously you dont see anything, because you discard at the entry already! That s why continue might be a better choice
                       continue;
                    } // dont sample if we are outside the unit box bounds
                    const float density = getDensity(currentPos);
                    const float gradient = getGradient(currentPos);

#if CLIP
                    if (!isInsideClip(currentPos)) continue;
#endif


                    if (density >= _MinVal && density <= _MaxVal) {
                        if (density > maxDensity) {
                            maxDensity = density;
#if TF2D_MODE || LOCAL_LIGHTING_BP || LOCAL_LIGHTING_CT
                            maxGradient = getGradient(currentPos);
#endif

#if TF1D_MODE
                            maxCol = getTF1DColor(density);
#elif TF2D_MODE
                            maxCol = getTF2DColor(density, length(maxGradient));
#else
                            maxCol = float4(1.0f, 1.0f, 1.0f, maxDensity);
#endif 
                            maxDensity = density;
                            maxPosition = currentPos;
                        }
                    }
                }


#if LOCAL_LIGHTING_BP
                float4 finalCol = float4(calculateLighting_blinnphong(maxCol.rgb, maxGradient, maxPosition, rayDir), maxDensity);
#elif LOCAL_LIGHTING_CT
                float4 finalCol = float4(calculateLighting_cooktorrance(maxCol.rgb, maxGradient, maxPosition, rayDir), maxDensity);
#else
                float4 finalCol = float4(maxCol.rgb, maxDensity);
#endif
                return finalCol;
            }

            // direct volume rendering
            fixed4 frag_dvr(v2f i) : SV_Target
            {
                const float StepSize = MAX_CUBE_DIST / _NumSteps;

                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);
                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);

                for (int iStep = 0; iStep < _NumSteps; iStep++) {
                    const float t = iStep * StepSize;
                    const float3 currentPos = rayStartPos + t * rayDir;
                    float3 abs_xyz = getAbsFloat3(currentPos);
                    if (abs_xyz.x > _XRange || abs_xyz.y > _YRange || abs_xyz.z > _ZRange) {
                        continue;
                    } // dont sample if we are outside the unit box bounds


#if CLIP
                    if (!isInsideClip(currentPos)) continue;
#endif


                    const float density = getDensity(currentPos);

#if TF2D_MODE || LOCAL_LIGHTING_BP || LOCAL_LIGHTING_CT
                    float3 normal = getGradient(currentPos);
#endif

#if TF2D_MODE
                    float4 src = getTF2DColor(density, length(normal));
#else
                    float4 src = getTF1DColor(density);
#endif

#if LOCAL_LIGHTING_BP
                    src.rgb = calculateLighting_blinnphong(src.rgb, normal, currentPos, rayDir);
#elif LOCAL_LIGHTING_CT
                    src.rgb = calculateLighting_cooktorrance(src.rgb, normal, currentPos, rayDir);
#endif

                    if (density <= _MinVal || density >= _MaxVal)
                        src.a = 0.0f;

                    col.rgb = src.a * src.rgb + (1.0f - src.a) * col.rgb; // interpolating old col value and new src value
                    col.a = src.a + (1.0f - src.a) * col.a;
                    if (col.a > 1.0f) break;
                }
                return col;
            }

            fixed4 frag_dvr_2(v2f i) : SV_Target{
                const float StepSize = MAX_CUBE_DIST / _NumSteps;

                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);
                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);

                for (int iStep = 0; iStep < _NumSteps; iStep++) {
                    const float t = iStep * StepSize;
                    const float3 currentPos = rayStartPos + t * rayDir;
                    float3 abs_xyz = getAbsFloat3(currentPos);
                    if (abs_xyz.x > _XRange || abs_xyz.y > _YRange || abs_xyz.z > _ZRange) {
                        continue;
                    } // dont sample if we are outside the unit box bounds
                    const float density = getDensity(currentPos);


#if CLIP
                    if (!isInsideClip(currentPos)) continue;
#endif


#if TF2D_MODE || LOCAL_LIGHTING_BP || LOCAL_LIGHTING_CT
                    float3 normal = getGradient(currentPos);
#endif

#if TF2D_MODE
                    float4 src = getTF2DColor(density, length(normal));
#else
                    float4 src = getTF1DColor(density);
#endif

#if LOCAL_LIGHTING_BP 
                    src.rgb = calculateLighting_blinnphong(src.rgb, normal, currentPos, rayDir);
#elif LOCAL_LIGHTING_CT
                    src.rgb = calculateLighting_cooktorrance(src.rgb, normal, currentPos, rayDir);
#endif
                    

                    if (density <= _MinVal || density >= _MaxVal) {
                        src.r = 0.0f;
                        src.g = 0.0f;
                        src.b = 0.0f;
                        src.a = 0.0f;
                    }
                    col = src + (1.0f - src.a) * col;
                    //if (col.a > 1.0f) break; // early break
                }
                return col;
            }


            fixed4 frag_surf_r(v2f i) : SV_Target
            {
                const float StepSize = MAX_CUBE_DIST / _NumSteps;

                float3 rayDir = -normalize(i.vectorToSurface);
                float3 rayStartPos = getOffsetPosition(i, rayDir);

                rayStartPos = rayStartPos + (StepSize * _NumSteps) * rayDir;
                rayDir = -rayDir;

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                for (int iStep = 0; iStep < _NumSteps; iStep++) {
                    const float t = iStep * StepSize;
                    const float3 currentPos = rayStartPos + t * rayDir;
                    float3 abs_xyz = getAbsFloat3(currentPos);
                    if (abs_xyz.x > _XRange || abs_xyz.y > _YRange || abs_xyz.z > _ZRange) {
                        continue;
                    } // dont sample if we are outside the unit box bounds*/
                    const float density = getDensity(currentPos);

#if CLIP
                    if (!isInsideClip(currentPos)) continue;
#endif


                    if (density >= _MinVal && density <= _MaxVal) {
#if TF2D_MODE || LOCAL_LIGHTING_BP || LOCAL_LIGHTING_CT
                        float3 normal = getGradient(currentPos);
#endif

#if TF2D_MODE
                        col = getTF2DColor(density, length(normal));
#else
                        col = getTF1DColor(density);
#endif

#if LOCAL_LIGHTING_BP
                        col.rgb = calculateLighting_blinnphong(col.rgb, normal, currentPos, -rayDir);
#elif LOCAL_LIGHTING_CT
                        col.rgb = calculateLighting_cooktorrance(col.rgb, normal, currentPos, -rayDir);
#endif
                        col.a = 1.0f;
                        break;
                    }
                }

                return col;
            }
            // end different fragment functions

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

            fixed4 frag(v2f i) : SV_Target
            {
                float4 final_result = float4(0.0f, 0.0f, 0.0f, 0.0f);
                int number_samples = pow(2, _ScaleSuperSample);
                
                for (int ind = 0; ind < number_samples; ind++) {
#if MODE_DVR
                    final_result += frag_dvr(i);
#elif MODE_SRF
                    final_result += frag_surf_r(i);
#elif MODE_CINEMA
                    final_result += frag_volpath(i);
#elif MODE_MIP
                    final_result += frag_mip(i);
#endif
                }
                return final_result / number_samples;
            }

            ENDCG
        }
    }
}
