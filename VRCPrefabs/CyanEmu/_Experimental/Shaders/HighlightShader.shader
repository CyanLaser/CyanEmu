Shader "CyanEmu/HighlightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
			#pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            #include "UnityCG.cginc"

            sampler2D _MainTex;
			sampler2D _GlowMap;
            float4 _GlowMap_TexelSize;

			// Taken from https://github.com/lindenreid/pipelinestuff/blob/master/Assets/Shaders/bloom.shader
			float4 gaussianBlur(sampler2D tex, float2 dir, float2 uv, float res)
            {
                //this will be our RGBA sum
                float4 sum = float4(0, 0, 0, 0);
                
                //the amount to blur, i.e. how far off center to sample from 
                //1.0 -> blur by one pixel
                //2.0 -> blur by two pixels, etc.
                float blur = 3 / res; 
                
                //the direction of our blur
                //(1.0, 0.0) -> x-axis blur
                //(0.0, 1.0) -> y-axis blur
                float hstep = dir.x;
                float vstep = dir.y;
                
                //apply blurring, using a 9-tap filter with predefined gaussian weights
                
                sum += tex2Dlod(tex, float4(uv.x - 4*blur*hstep, uv.y - 4.0*blur*vstep, 0, 0)) * 0.0162162162;
                sum += tex2Dlod(tex, float4(uv.x - 3.0*blur*hstep, uv.y - 3.0*blur*vstep, 0, 0)) * 0.0540540541;
                sum += tex2Dlod(tex, float4(uv.x - 2.0*blur*hstep, uv.y - 2.0*blur*vstep, 0, 0)) * 0.1216216216;
                sum += tex2Dlod(tex, float4(uv.x - 1.0*blur*hstep, uv.y - 1.0*blur*vstep, 0, 0)) * 0.1945945946;
                
                sum += tex2Dlod(tex, float4(uv.x, uv.y, 0, 0)) * 0.2270270270;
                
                sum += tex2Dlod(tex, float4(uv.x + 1.0*blur*hstep, uv.y + 1.0*blur*vstep, 0, 0)) * 0.1945945946;
                sum += tex2Dlod(tex, float4(uv.x + 2.0*blur*hstep, uv.y + 2.0*blur*vstep, 0, 0)) * 0.1216216216;
                sum += tex2Dlod(tex, float4(uv.x + 3.0*blur*hstep, uv.y + 3.0*blur*vstep, 0, 0)) * 0.0540540541;
                sum += tex2Dlod(tex, float4(uv.x + 4.0*blur*hstep, uv.y + 4.0*blur*vstep, 0, 0)) * 0.0162162162;

                return float4(sum.rgb, 1.0);
            }


            float4 frag(v2f_img input) : COLOR
			{
				/*
				float resX = _GlowMap_TexelSize.z;
				float resY = _GlowMap_TexelSize.w;
				float4 glow = 0;

				glow += gaussianBlur(_GlowMap, float2(1,0), input.uv, resX);
				glow += gaussianBlur(_GlowMap, float2(0,1), input.uv, resY);
				
				glow /= 4;
				*/

				float4 mainTex = tex2D(_MainTex, input.uv);
				float4 glow = tex2D(_GlowMap, input.uv);
				return mainTex + glow;
			}
            ENDCG
        }
    }
}
