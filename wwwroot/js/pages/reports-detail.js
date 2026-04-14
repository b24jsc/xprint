$(function () {
    // 1. SỰ KIỆN ĐỊNH DẠNG JSON (EVENT DELEGATION)
    $(document).on('click', '#btnFormatJson', function () {
        try {
            const content = $('#jsonEditor').val();
            const obj = JSON.parse(content);
            $('#jsonEditor').val(JSON.stringify(obj, null, 4));
            DevExpress.ui.notify("Đã căn chỉnh JSON", "info", 1500);
        } catch (e) {
            alert("JSON không hợp lệ để định dạng!");
        }
    });

    // 2. SỰ KIỆN LƯU JSON (EVENT DELEGATION)
    $(document).on('click', '#btnSaveJsonDetail', function () {
        const btn = $(this);

        // Lấy form đang nằm trong Offcanvas
        const formElement = $('#frmUpdateJson')[0];
        if (!formElement) {
            alert("Lỗi: Không tìm thấy Form dữ liệu!");
            return;
        }

        const formData = new FormData(formElement);
        formData.append('__RequestVerificationToken', $('meta[name="csrf-token"]').attr('content'));

        btn.prop('disabled', true).text('Đang lưu...');

        $.ajax({
            url: '/Reports/UpdateJsonSchema',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                DevExpress.ui.notify(res, "success", 2000);
                btn.prop('disabled', false).html('<i class="dx-icon-save me-1"></i> LƯU CẤU HÌNH');
            },
            error: function (xhr) {
                alert(xhr.responseText);
                btn.prop('disabled', false).text('Lưu lại');
            }
        });
    });
});