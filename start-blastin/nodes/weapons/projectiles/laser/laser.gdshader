shader_type canvas_item;

uniform float intensity = 1.0;
uniform float alpha_mod = 1.0;

uniform float speed = 2.0;
uniform float frequency_y = 5.0;
uniform float frequency_x = 5.0;
uniform float amplitude_y = 10.0; // مقدار را کمتر کنید تا از کادر خارج نشود
uniform float amplitude_x = 10.0;
uniform float rotation_angle = 0.0;

void vertex() {
    // ۱. تبدیل زاویه به رادیان و ساخت ماتریس دوران
    float rad = radians(rotation_angle);
    mat2 rot = mat2(vec2(cos(rad), -sin(rad)), vec2(sin(rad), cos(rad)));

    // ۲. اعمال دوران اولیه به راس‌ها
    vec2 pos = VERTEX.xy;

    // ۳. محاسبه جابجایی متوازن (Wave)
    // حذف UV.x از انتهای فرمول باعث می‌شود کل بدنه‌ی اسپرایت به یک اندازه تکان بخورد
    float wave_y = sin((UV.x * frequency_y) + (TIME * speed)) * amplitude_y;
    float wave_x = cos((UV.y * frequency_x) + (TIME * speed)) * amplitude_x;

    // ۴. اضافه کردن نوسان به پوزیشن
    pos.x += wave_x;
    pos.y += wave_y;

    // ۵. بازگرداندن پوزیشن نهایی با احتساب دوران
    VERTEX.xy = rot * pos;
}

void fragment() {
	float red = texture(TEXTURE, UV).r;
	float green = texture(TEXTURE, UV).g;
	float blue = texture(TEXTURE, UV).b;
	
	COLOR = vec4(red - (sin(TIME * speed)) * intensity, green - (sin(TIME * speed)) * intensity, blue, alpha_mod);
}