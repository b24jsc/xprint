document.addEventListener('DOMContentLoaded', () => {
    const canvas = document.getElementById('glCanvas');
    const gl = canvas.getContext('webgl');

    if (!gl) return;

    const vsSource = `
        attribute vec4 a_position;
        void main() { gl_Position = a_position; }
    `;

    const fsSource = `
        precision highp float;
        uniform vec2 u_resolution;
        
        // Tăng giới hạn lên 100 điểm để lưu được đuôi dài hơn khi di chuột nhanh
        uniform vec2 u_trail[100];       
        uniform float u_trail_time[100]; 
        uniform float u_time;

        void main() {
            vec2 uv = gl_FragCoord.xy / u_resolution.xy;
            vec2 force = vec2(0.0);
            float aspect = u_resolution.x / u_resolution.y;

            for(int i = 0; i < 100; i++) {
                float t = u_trail_time[i];
                if (t > 0.0) {
                    vec2 pos = u_trail[i] / u_resolution.xy;
                    vec2 p = uv - pos;
                    p.x *= aspect;

                    float dist = length(p);
                    float age = u_time - t;

                    // Tăng tuổi thọ sóng lên 3.0 giây (trước là 1.5)
                    if (age < 3.0 && age > 0.0) {
                        // Làm mờ từ từ trong 3 giây
                        float decay = max(0.0, 1.0 - age / 3.0);
                        
                        // age * 0.1: Tốc độ lan toả của vòng tròn (to hơn bản cũ)
                        float diff = dist - age * 0.08; 
                        
                        // 1500.0: Làm vòng tròn dày hơn một chút (để dễ nhìn khi toả lớn)
                        float wave = exp(-diff * diff * 1500.0); 
                        
                        force += normalize(p + 0.0001) * wave * decay * 0.03; 
                    }
                }
            }

            vec2 bg_uv = uv - force;
            
            float blue_dist = length(bg_uv - vec2(0.6, 0.5));
            vec3 bg_color = mix(vec3(0.65, 0.85, 1.0), vec3(0.97, 0.98, 0.99), smoothstep(0.1, 0.8, blue_dist));
            
            bg_color += length(force) * 0.6; // Tăng độ bắt sáng lên một chút

            gl_FragColor = vec4(bg_color, 1.0);
        }
    `;

    function createShader(gl, type, source) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        return shader;
    }

    const program = gl.createProgram();
    gl.attachShader(program, createShader(gl, gl.VERTEX_SHADER, vsSource));
    gl.attachShader(program, createShader(gl, gl.FRAGMENT_SHADER, fsSource));
    gl.linkProgram(program);
    gl.useProgram(program);

    const positionBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);

    const positionLocation = gl.getAttribLocation(program, "a_position");
    gl.enableVertexAttribArray(positionLocation);
    gl.vertexAttribPointer(positionLocation, 2, gl.FLOAT, false, 0, 0);

    const u_resolution = gl.getUniformLocation(program, "u_resolution");
    const u_time = gl.getUniformLocation(program, "u_time");
    const u_trail = gl.getUniformLocation(program, "u_trail");
    const u_trail_time = gl.getUniformLocation(program, "u_trail_time");

    // Nâng lên 100 điểm
    const TRAIL_LENGTH = 100;
    const trail = new Float32Array(TRAIL_LENGTH * 2);
    const trail_time = new Float32Array(TRAIL_LENGTH);
    let trail_index = 0;

    // Biến lưu trữ tọa độ chuột và quán tính
    let targetX = window.innerWidth / 2;
    let targetY = window.innerHeight / 2;
    let currentX = targetX;
    let currentY = targetY;
    let lastRecordTime = 0;

    window.addEventListener('mousemove', (e) => {
        targetX = e.clientX;
        targetY = canvas.height - e.clientY;
    });

    function resize() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        gl.viewport(0, 0, canvas.width, canvas.height);
        gl.uniform2f(u_resolution, canvas.width, canvas.height);
    }
    window.addEventListener('resize', resize);
    resize();

    function render(now) {
        // Toán học Lerp: Tạo độ trễ mượt mà cho vệt nước đi theo chuột
        // Số 0.08 quyết định độ trễ (càng nhỏ càng chậm, trượt càng dài)
        currentX += (targetX - currentX) * 0.08;
        currentY += (targetY - currentY) * 0.08;

        // Chỉ ghi lại điểm mới mỗi 30ms để tiết kiệm bộ nhớ 100 điểm
        if (now - lastRecordTime > 30) {
            trail[trail_index * 2] = currentX;
            trail[trail_index * 2 + 1] = currentY;
            trail_time[trail_index] = now / 1000.0;
            trail_index = (trail_index + 1) % TRAIL_LENGTH;
            lastRecordTime = now;
        }

        gl.uniform1f(u_time, now / 1000.0);
        gl.uniform2fv(u_trail, trail);
        gl.uniform1fv(u_trail_time, trail_time);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
        requestAnimationFrame(render);
    }
    requestAnimationFrame(render);
});