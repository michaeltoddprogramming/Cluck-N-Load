Shader "Custom/DynamicGridShader"
{
    Properties
    {
        _Color ("Grid Color", Color) = (0,1,0,0.5)
        _HoverCell ("Hovered Cell", Vector) = (0,0,0,0)
        _GridDivisions ("Grid Divisions", Vector) = (10,10,0,0)
        _GridOrigin ("Grid Origin", Vector) = (0,0,0,0)
        _GridWorldSize ("Grid World Size", Vector) = (10,10,0,0)
        _MainTex ("Grid Texture", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (1,1,0,1)
        _GridLineOpacity ("Grid Line Opacity", Range(0,1)) = 0.5
        _LineWidth ("Line Width", Range(0.01,0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float4 _HoverCell;
            float4 _GridDivisions;
            float4 _GridOrigin;
            float4 _GridWorldSize;
            float4 _HighlightColor;
            float _GridLineOpacity;
            float _LineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 relativePos = (float2(i.worldPos.x, i.worldPos.z) - _GridOrigin.xy) / _GridWorldSize.xy;
                float2 gridIndex = relativePos * _GridDivisions.xy;
                float2 cellIndices = floor(gridIndex);
                float2 fracInCell = frac(gridIndex);

                // Base grid color
                fixed4 gridColor = _Color;

                // Hover highlight
                float isX = abs(cellIndices.x - _HoverCell.x) < 0.5 ? 1.0 : 0.0;
                float isY = abs(cellIndices.y - _HoverCell.y) < 0.5 ? 1.0 : 0.0;
                float hoverMatch = isX * isY;
                if (hoverMatch > 0.5)
                {
                    gridColor = _HighlightColor; // Use highlight color
                }

                // Draw grid lines
                float lineX = smoothstep(0.0, _LineWidth, fracInCell.x) * smoothstep(0.0, _LineWidth, 1.0 - fracInCell.x);
                float lineY = smoothstep(0.0, _LineWidth, fracInCell.y) * smoothstep(0.0, _LineWidth, 1.0 - fracInCell.y);
                float gridLine = max(1.0 - lineX, 1.0 - lineY);

                // Apply grid line opacity
                gridColor.a *= _GridLineOpacity;

                // Sample texture
                float2 texUV = float2((cellIndices.x + 0.5) / _GridDivisions.x, (cellIndices.y + 0.5) / _GridDivisions.y);
                fixed4 texColor = tex2D(_MainTex, texUV);

                // Blend texture colors (R, G, B) with grid lines
                fixed4 blendedColor = texColor * (1.0 - gridLine) + gridColor * gridLine;

                // Ensure alpha is preserved for transparency
                blendedColor.a = max(gridLine, gridColor.a);

                return saturate(blendedColor);
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}