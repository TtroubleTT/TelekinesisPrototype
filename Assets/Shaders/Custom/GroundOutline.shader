Shader "Custom/GroundOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _Size ("Size", Vector) = (1, 1, 0, 0) // For boxes/rectangles
        _Radius ("Radius", Float) = 0.5 // For circles
        _Shape ("Shape", Int) = 0 // 0: Circle, 1: Rectangle
        _Thickness ("Thickness", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _OutlineColor;
            float2 _Size;
            float _Radius;
            int _Shape;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xy, 0.001f, 1.0f));
                o.uv = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_Shape == 0) // Circle
                {
                    float dist = distance(i.uv, float2(0, 0));
                    float outerRadius = _Radius;
                    float innerRadius = _Radius - _Thickness;

                    if (dist > innerRadius && dist < outerRadius)
                    {
                        return _OutlineColor;
                    }
                }
                else // Rectangle
                {
                    float2 halfSize = _Size / 2;
                    float2 outer = halfSize;
                    float2 inner = halfSize - _Thickness;

                    if (i.uv.x > -outer.x && i.uv.x < outer.x && (i.uv.y > outer.y || i.uv.y < -outer.y) ||
                        i.uv.y > -outer.y && i.uv.y < outer.y && (i.uv.x > outer.x || i.uv.x < -outer.x))
                    {
                        return _OutlineColor;
                    }
                }

                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}