﻿Shader "Custom/Estuary"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        #include "Water.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 riverUV;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.riverUV = v.texcoord1.xy;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            /***
            float2 uv = IN.uv_MainTex;
            uv.x = uv.x * 0.0625 ;//+ _Time.y * 0.005;
            uv.y = _Time.y * 0.25;
            float4 noise = tex2D(_MainTex, uv);
            float2 uv2 = IN.uv_MainTex;
            uv2.x = uv2.x * 0.0625 ;//- _Time.y * 0.0052;
            uv2.y -= _Time.y * 0.23;
            float4 noise2 = tex2D(_MainTex, uv2);
            fixed4 c = saturate(_Color + (noise.r * noise2.a));
            ***/
            float shore = sqrt(IN.uv_MainTex.y) * 0.9;
            float foam = Foam(shore, IN.worldPos.xz, _MainTex);
            float wave = Waves(IN.worldPos.xz, _MainTex);
            float shoreWater = max(wave, foam);
            float river = River(IN.riverUV, _MainTex);
            float water = lerp(shoreWater, river, IN.uv_MainTex.x);
            fixed4 c = saturate(_Color + water);
            
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
