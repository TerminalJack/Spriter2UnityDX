Shader "Hidden/SeparableGaussianBlur"
{
    Properties
    {
        _MainTex    ("SourceTex", 2D) = "white" {}
        _Direction  ("Blur Direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float2    _Direction;   // (1,0)=horiz, (0,1)=vert

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 5-tap Catmull-Rom weights: [1,4,6,4,1]/16
                const float w0 = 0.0625; // 1/16
                const float w1 = 0.25;   // 4/16
                const float w2 = 0.375;  // 6/16

                float2 uv = i.uv;
                float2 off = _Direction * _MainTex_TexelSize.xy;

                float4 sum = tex2D(_MainTex, uv) * w2;
                sum += tex2D(_MainTex, uv + off) * w1;
                sum += tex2D(_MainTex, uv - off) * w1;
                sum += tex2D(_MainTex, uv + off * 2) * w0;
                sum += tex2D(_MainTex, uv - off * 2) * w0;

                return sum;
            }

            ENDCG
        }
    }
}
