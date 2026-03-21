// CC0, shadecore_dev
// Stylized gradient pixel glint shader, from https://godotshaders.com/shader/gradient-pixel-glint/

shader_type canvas_item;

/* Glint texture. Any 1xN size texture can be used. */
uniform sampler2D gradient_texture;
/* Glint texture width in pixels. */
uniform float gradient_width : hint_range(0.0, 512.0, 1.0) = 32.0;

uniform float shine_progress : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform float shine_size : hint_range(0.01, 1.0, 0.01) = 0.1;
uniform float shine_angle : hint_range(0.0, 89.9, 0.1) = 45.0;

/* Any pixels darker than this threshold will not receive glint. */
uniform float threshold : hint_range(0.0, 1.0) = 0.01;

float scale(float value, float inMin, float inMax, float outMin, float outMax) {
    return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
}

void fragment() {
	vec2 pixelation = 1.0 / TEXTURE_PIXEL_SIZE;
	vec2 pixelated_uv = floor(UV * pixelation) / pixelation;
	COLOR = texture(TEXTURE, UV);
    
	float slope = tan(radians(shine_angle));

	float progress = scale(shine_progress, 0.0, 1.0, -1.0 - shine_size - shine_size * slope, 1.0 * slope + shine_size + shine_size * slope);

	float shine_distance = (slope * pixelated_uv.x - pixelated_uv.y - progress) / (sqrt(slope * slope + 1.0));

	float shine_color_coord = clamp((shine_distance / (2.0 * shine_size)) + 0.5, 0.0, 1.0);

	vec4 raw_color = texelFetch(gradient_texture, ivec2(int(shine_color_coord * gradient_width), 0), 0);

    vec3 cycle = vec3(
    sin(TIME * 0.4) * 0.5 + 0.5,
    sin(TIME * 0.4 + 2.094) * 0.5 + 0.5,
    sin(TIME * 0.4 + 4.189) * 0.5 + 0.5
    );


    vec4 mod_color = clamp(sin(vec4(1, 1, 1, raw_color.a) * TIME), vec4(0.5, 0.5, 0.5, 0.5), vec4(1, 1, 1, 1));
    vec4 new_color = vec4(mix(raw_color.rgb, cycle, 0.4), raw_color.a);

    

	float shine = step(shine_distance, shine_size) - step(shine_distance, -shine_size);
	
	float max_component = max(max(COLOR.r, COLOR.g), COLOR.b);

	/* COLOR.rgb = mix(COLOR.rgb, raw_color.rgb, shine * raw_color.a * float(max_component >= threshold)); */

    COLOR.rgb = mix(COLOR.rgb, new_color.rgb, shine * new_color.a * float(max_component >= threshold));
}