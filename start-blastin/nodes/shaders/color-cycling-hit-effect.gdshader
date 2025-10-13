// Color cycling hit effect by rasmus
// Modified with different colors
// https://godotshaders.com/shader/color-cycling-hit-effect/

shader_type canvas_item;

uniform int start_frame = 0;
uniform int current_frame = 0;
uniform float mix_ratio = 1.0;

uniform vec3 color_array[6]: source_color;

// const vec3 colors[6] = vec3[] (
// 	vec3(255, 251, 0), // Coral
// 	vec3(0.38, 0.0, 0.06), // Dark red
// 	vec3(0.0, 0.0, 0.0), // Black
// 	vec3(0.37, 0.76, 1.0), // Cyan
// 	vec3(0.0, 0.49, 0.8), // Dark blue
// 	vec3(0.0, 0.0, 0.0)  // Black
// );

void fragment() {
	// https://en.wikipedia.org/wiki/Rec._709#Luma_coefficients
	float brightness = dot(COLOR.rgb, vec3(0.2126, 0.7152, 0.0722));

	// Calculate the starting frame based on brightness
	int offset = 0;
	if (brightness > 0.75) {
		offset = 2;
	} else if (brightness > 0.25) {
		offset = 1;
	}

	// Get the color, wrapping around at the end of the array
	int color_index = (start_frame + current_frame + offset) % color_array.length();
	vec3 color = color_array[color_index];

	// Apply the color to the sprite
	COLOR = vec4(mix(COLOR.rgb, color, mix_ratio), COLOR.a);
}