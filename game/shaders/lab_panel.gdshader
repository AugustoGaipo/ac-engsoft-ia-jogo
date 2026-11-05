// Test-facility panels (2026-11-04): a world-space grid of panels with dark seams, each panel a slightly
// different shade. World space, so every box of the scenery lines up with its neighbours whatever its size.
shader_type spatial;

uniform vec3 base_color : source_color = vec3(0.86, 0.87, 0.88);
uniform vec3 seam_color : source_color = vec3(0.32, 0.35, 0.40);
uniform float panel_size = 1.5;
uniform float seam_width = 0.03;
uniform float variation = 0.05;
uniform float roughness_value = 0.55;
uniform vec3 emission_color : source_color = vec3(0.0);

varying vec3 world_pos;
varying vec3 world_normal;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = abs(normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz));
}

void fragment() {
	// The two axes of the face: the plane across the dominant normal.
	vec2 uv = world_normal.y > 0.5 ? world_pos.xz : (world_normal.x > 0.5 ? world_pos.zy : world_pos.xy);
	vec2 cell = uv / panel_size;
	vec2 inside = abs(fract(cell) - 0.5) * panel_size;          // distance from the panel centre (m)
	float edge = max(inside.x, inside.y);
	float seam = smoothstep(panel_size * 0.5 - seam_width, panel_size * 0.5 - seam_width * 0.4, edge);
	float shade = 1.0 + (hash(floor(cell)) - 0.5) * 2.0 * variation;
	ALBEDO = mix(base_color * shade, seam_color, seam);
	ROUGHNESS = mix(roughness_value, 0.9, seam);
	EMISSION = emission_color;
}
