Shader "Custom/Barrel" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Factor ("Factor", float) = 1.8
		_xCenter ("xCenter", float) = 0.5
		_yCenter ("yCenter", float) = 0.5 
		_blueOffset ("Blue Offset", float) = 0
		_redOffset ("Red Offset", float) = 0
		_gammaMod ("Gamma Adjustment", float) = 1.8
	}
		
		
	SubShader {
	 
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 
			//we include "UnityCG.cginc" to use the appdata_img struct

			sampler2D _MainTex;
			float _Factor;
			float _xCenter;
			float _yCenter;
			float _blueOffset;
			float _redOffset;
			float _gammaMod;

			struct v2f {
				float4 pos : POSITION;
				half2 uv : TEXCOORD0;
			};

			//functions
			v2f vert (appdata_img v){
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
//				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				o.uv = v.texcoord.xy;
				//_Theta = (v.texcoord.xy - _LensCentre) * _Scale;
				return o; 
			}

			float4 frag(v2f i) : COLOR
			{
				const float2 warpCenter = float2(_xCenter, _yCenter);
				float2 centeredTexCoord = i.uv - warpCenter;
				float2 warped = normalize(centeredTexCoord); //0-pi/2 range
				// If radial length was 0.5, we want rescaled to also come out
				// as 0.5, so the edges of the rendered image are at the edges
				// of the warped image.
				float rescaled = tan (length(centeredTexCoord)*_Factor)/tan(0.5*_Factor);


				warped *= 0.5 * rescaled;
				warped += warpCenter;

				float2 tcBlue = (warped - warpCenter) * _blueOffset * 0.001 + warped;

				if (!all(clamp(tcBlue, float2(0,0), float2(1,1))==tcBlue))
				{
					return float4(0,0,0,1);
				}
				else
				{
					//return float4 (clamp(tcBlue, float2(0,0), float2(1,1)).x,clamp(tcBlue, float2(0,0), float2(1,1)).y,1,1);
					float2 tcRed = (warped - warpCenter) * _redOffset * 0.001 + warped;
//					float2 tcBlue2 = (warped - warpCenter) * _blueOffset * 0.001 + warped;
					float4 red = tex2D(_MainTex, tcRed);
					float4 green = tex2D(_MainTex, warped);
					//red = float4(0,0,0,1);
					//green = float4(0,0,0,1);
					float4 blue = tex2D(_MainTex, tcBlue);

					//	  	half4 red2 = half4 (red.r, 0,0,1);
					//	  	half4 green2 = half4 (0,green.g,0,1);
					//	  	half4 blue2 = half4 (0,0,blue.b,1);
					//	  	return red2 + green2 + blue2;
					float4 gamma = float4(1/_gammaMod,1/_gammaMod,1/_gammaMod,1.0);
					return pow(float4(red.r,green.g,blue.b,1),gamma);
//					return float4(red.r, green.g, blue.b, 1);
				}
			}
		ENDCG
		}
	}
}
