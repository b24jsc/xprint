document.addEventListener('DOMContentLoaded', () => {
    const interactiveBlob = document.querySelector('.blob-light');

    // Lưu trữ tọa độ đích (vị trí chuột)
    let mouseX = window.innerWidth / 2;
    let mouseY = window.innerHeight / 2;

    // Lưu trữ tọa độ hiện tại của khối màu
    let blobX = mouseX;
    let blobY = mouseY;

    window.addEventListener('mousemove', (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
    });

    function animateBlob() {
        // Thuật toán Lerp (Linear Interpolation) để khối màu trượt đi mượt mà
        // theo sau chuột chứ không bị giật cứng vào vị trí chuột
        blobX += (mouseX - blobX) * 0.05;
        blobY += (mouseY - blobY) * 0.05;

        // Cập nhật vị trí thông qua biến CSS transform
        // Chú ý trừ đi 50% để tâm của khối màu trùng với con trỏ
        if (interactiveBlob) {
            interactiveBlob.style.transform = `translate(calc(-50% + ${blobX - window.innerWidth / 2}px), calc(-50% + ${blobY - window.innerHeight / 2}px))`;
        }

        requestAnimationFrame(animateBlob);
    }

    animateBlob();
});