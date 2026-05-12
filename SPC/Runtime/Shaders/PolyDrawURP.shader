Shader "Spookline/SPC/PolyDrawURP"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OccludedAlpha ("Occluded Alpha Multiplier", Range(0, 1)) = 0.2
        _OccludedColorMultiplier ("Occluded Color Multiplier", Range(0, 1)) = 0.65
        _DepthBias ("Depth Bias", Float) = 0.01

        _BackfaceAlphaMultiplier ("Backface Alpha Multiplier", Range(0, 1)) = 0.35
        _BackfaceColorMultiplier ("Backface Color Multiplier", Range(0, 1)) = 0.75

        _EdgeShadowStrength ("Edge Shadow Strength", Range(0, 1)) = 0.35
        _EdgeShadowPower ("Edge Shadow Power", Range(0.25, 8)) = 2.0
        _FaceShadeStrength ("Face Shade Strength", Range(0, 1)) = 0.18
        _LightDirection ("Fake Light Direction", Vector) = (0.35, 0.7, 0.45, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "PrimitiveDrawGizmoOverlayBackfaces"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBack

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _OccludedAlpha;
                half _OccludedColorMultiplier;
                float _DepthBias;

                half _BackfaceAlphaMultiplier;
                half _BackfaceColorMultiplier;

                half _EdgeShadowStrength;
                half _EdgeShadowPower;
                half _FaceShadeStrength;
                float4 _LightDirection;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float primitiveFlags : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half primitiveFlags : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.color = input.color * _Color;
                output.primitiveFlags = input.primitiveFlags;

                return output;
            }

            void ApplyOcclusion(Varyings input, inout half4 color)
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;

                float sceneRawDepth = SampleSceneDepth(screenUV);
                float primitiveRawDepth = input.positionCS.z;

                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float primitiveEyeDepth = LinearEyeDepth(primitiveRawDepth, _ZBufferParams);

                bool occluded = primitiveEyeDepth > sceneEyeDepth + _DepthBias;

                if (occluded)
                {
                    color.rgb *= _OccludedColorMultiplier;
                    color.a *= _OccludedAlpha;
                }
            }

            half4 FragBack(Varyings input) : SV_Target
            {
                half4 color = input.color;

                bool doublesided = input.primitiveFlags > 0.5h;

                if (!doublesided)
                {
                    color.rgb *= _BackfaceColorMultiplier;
                    color.a *= _BackfaceAlphaMultiplier;
                }

                ApplyOcclusion(input, color);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PrimitiveDrawGizmoOverlayFrontfaces"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Back
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFront

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _OccludedAlpha;
                half _OccludedColorMultiplier;
                float _DepthBias;

                half _BackfaceAlphaMultiplier;
                half _BackfaceColorMultiplier;

                half _GLMode;

                half _EdgeShadowStrength;
                half _EdgeShadowPower;
                half _FaceShadeStrength;
                float4 _LightDirection;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.color = input.color * _Color;

                return output;
            }

            void ApplyOcclusion(Varyings input, inout half4 color)
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;

                float sceneRawDepth = SampleSceneDepth(screenUV);
                float primitiveRawDepth = input.positionCS.z;

                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float primitiveEyeDepth = LinearEyeDepth(primitiveRawDepth, _ZBufferParams);

                bool occluded = primitiveEyeDepth > sceneEyeDepth + _DepthBias;

                if (occluded)
                {
                    color.rgb *= _OccludedColorMultiplier;
                    color.a *= _OccludedAlpha;
                }
            }

            half4 FragFront(Varyings input) : SV_Target
            {
                half4 color = input.color;

                if (_GLMode < 0.5h)
                {
                    half3 normalWS = normalize(input.normalWS);
                    half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                    half3 lightDirWS = normalize((half3)_LightDirection.xyz);

                    half ndotv = saturate(abs(dot(normalWS, viewDirWS)));
                    half edge = pow(1.0h - ndotv, _EdgeShadowPower);
                    half edgeShadow = 1.0h - edge * _EdgeShadowStrength;

                    half ndotl = saturate(dot(normalWS, lightDirWS));
                    half faceShade = lerp(1.0h - _FaceShadeStrength, 1.0h, ndotl);

                    color.rgb *= edgeShadow * faceShade;
                }

                ApplyOcclusion(input, color);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}