float Foam(float shore, float2 worldxz, sampler2D noiseTex) 
{
    float2 noiseuv = worldxz + _Time.y * 0.25;
    float4 noise = tex2D(noiseTex, noiseuv * 0.015);
    float distortion = noise.x * (1 - shore);
    float foam = sin((shore+distortion) * 10 - _Time.y);
    foam *= foam * shore;
    
    float distortion2 = noise.y * (1 - shore);
    float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2);
    foam2 *= foam2 * 0.7;
    return max(foam, foam2) * shore;
}

float Waves(float2 worldxz, sampler2D noiseTex)
{
    float2 uv1 = worldxz;
    uv1.y += _Time.y;
    float4 noise1 = tex2D(noiseTex, uv1 * 0.025);
    float2 uv2 = worldxz;
    uv2.x += _Time.y;
    float4 noise2 = tex2D(noiseTex, uv2 * 0.025);
    float blendWave = sin((worldxz.x + worldxz.y) * 0.1 + (noise1.y + noise2.z) + _Time.y);
    blendWave *= blendWave;
    float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);
    return smoothstep(0.75, 2, waves);
}