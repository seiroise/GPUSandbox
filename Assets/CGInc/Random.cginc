#ifndef __INCLUDE_RANDOM_
#define __INCLUDE_RANDOM_

#ifndef PI
#define PI 3.1415926535f
#endif

#if 0
// これらのハッシュ関数については下のリンク先を参考のこと
// https://www.shadertoy.com/view/4djSRW

// スカラーから対応するハッシュを生成
float hash11(float p)
{
	p = frac(p * .1031);
	p *= p + 19.19;
	p *= p + p;
	return frac(p);
}

// 二次元ベクトルから対応するハッシュの生成
float hash12(float2 p)
{
	float3 p3 = frac(float3(p.xyx) * HASHSCALE1);
	p3 += dot(p3, p3.yzx + 19.19);
	return frac((p3.x + p3.y) * p3.z);
}
#endif

// 二次元ベクトルから対応するハッシュの生成
// 一度の呼び出しでは規則性の少ない乱数を生成してくれる。
inline float hash12_sin(float2 p)
{
	return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// 単位円上のランダムな点を生成する。
float2 random_point_on_unit_circle(float2 p)
{
	float theta = hash12_sin(p) * PI * 2;
	return float2(cos(theta), sin(theta));
}

#endif