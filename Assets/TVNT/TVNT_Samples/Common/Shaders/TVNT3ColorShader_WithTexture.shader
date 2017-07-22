// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "TVNT/TVNT3ColorShader_WithTexture" {
Properties
{
 _MainTex ("Base (RGB)", 2D) = "white" {}
 _TexMulti ("Texture Multiplier", Range(0.0,2.0)) = 1.125
_UpColor ("UpColor", Color) = (1,0,0,1)
_LeftColor ("LeftColor", Color) = (0,0,0,1)
_FrontColor ("FrontColor", Color) = (0,0,0,1)
}
SubShader
{
Tags { "RenderType"="Opaque" }
LOD 200
 
Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
 
#include "UnityCG.cginc"
 
struct appdata
{
float4 vertex : POSITION;
float4 normal : NORMAL;
fixed4 color: COLOR;
float2 uv:TEXCOORD0;
};
 
struct v2f
{
float4 color : COLOR;
float4 vertex : SV_POSITION;
float2 uv:TEXCOORD0;
};

sampler2D _MainTex;
fixed4 _MainTex_ST;
float _TexMulti;
fixed4 _UpColor;
fixed4 _LeftColor;
fixed4 _FrontColor;
 
v2f vert (appdata v)
 {
 v2f o;
 o.vertex = UnityObjectToClipPos(v.vertex);
 float3 normal = UnityObjectToWorldNormal(v.normal);
 o.color = (lerp(_FrontColor, _LeftColor, (0.5-(saturate(dot(normal, float3(0,0,-1)))*0.5))+(0.5*saturate(dot(normal, float3(1,0,0))))))*(1-saturate(dot(normal, float3(0,1,0))));
 o.color += (_UpColor * saturate(dot(normal, float3(0,1,0))));
 o.color *= v.color;
 o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
 return o;
 }
           
 
fixed4 frag (v2f i) : SV_Target
{
return i.color*(tex2D(_MainTex,i.uv)*_TexMulti);
}
ENDCG
}
}
}