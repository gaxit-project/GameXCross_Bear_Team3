Shader "Custom/GhostDoubleShell"
{
    Properties
    {
        _Color ("Main Color", Color) = (0, 1, 0, 1) // ベース色
        _AlphaFront ("Front Alpha", Range(0, 1)) = 0.5 // 手前の濃さ
        _AlphaBack ("Back Alpha", Range(0, 1)) = 0.2 // 奥の濃さ
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        
        // 共通設定
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        // =======================================================
        // 【1パス目】奥の面（裏殻）を描く
        // =======================================================
        Pass
        {
            Cull Front // 裏側だけ描く
            
            // ステンシル設定：
            // 「まだ裏側を描いていない場所（ID:1以外）」だけに描く。
            // つまり、裏側同士が重なっても2回描かれない（色が濃くならない）。
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            fixed4 _Color;
            float _AlphaBack;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                col.a = _AlphaBack;
                return col;
            }
            ENDCG
        }

        // =======================================================
        // 【2パス目】手前の面（表殻）を描く
        // =======================================================
        Pass
        {
            Cull Back // 表側だけ描く

            // ステンシル設定：
            // 今度は別のID（ID:2）を使って管理する。
            // 「まだ表側を描いていない場所（ID:2以外）」だけに描く。
            Stencil
            {
                Ref 2
                Comp NotEqual
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            fixed4 _Color;
            float _AlphaFront;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                col.a = _AlphaFront;
                return col;
            }
            ENDCG
        }
    }
}