Shader "Spookline/SPC/PolyDrawGLURP"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _OccludedAlpha ("Occluded Alpha Multiplier", Range(0, 1)) = 0.2
        _OccludedColorMultiplier ("Occluded Color Multiplier", Range(0, 1)) = 0.65
        _DepthBias ("Depth Bias", Float) = 0.01
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
            Name "PrimitiveDrawGizmoOverlayGL"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _OccludedAlpha;
                half _OccludedColorMultiplier;
                float _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);

                output.positionCS = positionInputs.positionCS;
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

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = input.color;

                ApplyOcclusion(input, color);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}